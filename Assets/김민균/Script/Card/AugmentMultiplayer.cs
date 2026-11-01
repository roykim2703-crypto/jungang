using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Collections;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Multiplayer;

/// <summary>Persistent two-player Relay room and host-authoritative match transport.</summary>
public sealed class AugmentMultiplayer : MonoBehaviour
{
    public enum RoomStage { Offline, Connecting, Lobby, Loading, Playing, Disconnected }
    public const string GameScene="Assets/Scenes/Game.unity",HomeScene="Assets/Scenes/Home.unity";
    private static AugmentMultiplayer instance;
    public static AugmentMultiplayer Instance
    {
        get
        {
            if(instance==null)instance=new GameObject("AugmentRoom").AddComponent<AugmentMultiplayer>();
            return instance;
        }
    }
    private AugmentMatch match;
    private NetworkManager network;
    private ISession session;
    private bool busy,active,registered,guestReady,lobbyReady,leaving;
    private int operation;
    private ulong? guest;
    private string code="",message="대국 방식을 선택하세요.";
    private static readonly string[] Messages={"augment-lobby","augment-start","augment-ready","augment-command","augment-state"};
    public RoomStage Stage { get; private set; }
    public bool Active=>active;
    public bool IsHost=>network!=null&&network.IsHost;
    public string InvitationCode=>code;
    public string ConnectionStatus=>message;
    public bool IsConnecting=>busy;
    public bool CanStart=>active&&IsHost&&guest.HasValue&&lobbyReady&&Stage==RoomStage.Lobby;
    private void Awake()
    {
        if(instance!=null&&instance!=this){Destroy(gameObject);return;}
        instance=this;DontDestroyOnLoad(gameObject);Application.runInBackground=true;
    }
    public void Attach(AugmentMatch value){match=value;}
    public void MatchLoaded(AugmentMatch value)
    {
        match=value;
        if(!active)return;
        match.WaitForNetwork();
        if(IsHost)TryBeginMatch();
        else Send("augment-ready","ready",NetworkManager.ServerClientId);
    }
    public Task JoinByCodeAsync(string invitationCode)
    {
        if(busy||active)throw new InvalidOperationException("이미 연결 중이거나 방에 참가했습니다.");
        string normalized=(invitationCode??"").Trim().ToUpperInvariant();
        if(normalized.Length<4||normalized.Length>12)throw new ArgumentException("초대코드를 확인하세요.");
        code=normalized;return Connect(false);
    }
    public async Task Connect(bool host)
    {
        if(busy||active)return;
        int request=++operation;busy=true;Stage=RoomStage.Connecting;message="방을 연결하는 중입니다…";
        try
        {
            if(UnityServices.State!=ServicesInitializationState.Initialized)
                await UnityServices.InitializeAsync(new InitializationOptions().SetProfile("aug-"+Guid.NewGuid().ToString("N").Substring(0,12)));
            if(request!=operation)return;
            if(!AuthenticationService.Instance.IsSignedIn)await AuthenticationService.Instance.SignInAnonymouslyAsync();
            if(request!=operation)return;
            network=NetworkManager.Singleton;
            if(network==null)
            {
                var root=new GameObject("AugmentNetwork");DontDestroyOnLoad(root);
                network=root.AddComponent<NetworkManager>();
            }
            if(network.NetworkConfig==null)network.NetworkConfig=new NetworkConfig();
            if(network.NetworkConfig.NetworkTransport==null)
                network.NetworkConfig.NetworkTransport=network.GetComponent<UnityTransport>()??network.gameObject.AddComponent<UnityTransport>();
            if(network.IsListening)throw new InvalidOperationException("이전 네트워크 연결이 아직 종료되지 않았습니다.");
            network.NetworkConfig.EnableSceneManagement=false;
            network.OnClientConnectedCallback+=Connected;network.OnClientDisconnectCallback+=Disconnected;
            ISession created=host
                ?await MultiplayerService.Instance.CreateSessionAsync(new SessionOptions{MaxPlayers=2,IsPrivate=true}.WithRelayNetwork())
                :await MultiplayerService.Instance.JoinSessionByCodeAsync(code);
            if(request!=operation){await created.LeaveAsync();return;}
            session=created;code=session.Code;active=true;Stage=RoomStage.Lobby;Register();
            message=host?"방 생성 완료 · 친구에게 초대코드를 보내세요.":"참가 완료 · 방장이 대국을 시작할 때까지 기다리세요.";
            if(!host)Send("augment-lobby","ready",NetworkManager.ServerClientId);
        }
        catch(Exception e)
        {
            if(request!=operation)return;
            Debug.LogException(e);
            string error="연결 실패: "+e.Message;
            await Leave(false);message=error;
        }
        finally{if(request==operation)busy=false;}
    }
    private void Register()
    {
        if(registered||network==null||network.CustomMessagingManager==null)return;
        network.CustomMessagingManager.RegisterNamedMessageHandler(Messages[0],LobbyReady);
        network.CustomMessagingManager.RegisterNamedMessageHandler(Messages[1],StartReceived);
        network.CustomMessagingManager.RegisterNamedMessageHandler(Messages[2],Ready);
        network.CustomMessagingManager.RegisterNamedMessageHandler(Messages[3],ReceiveCommand);
        network.CustomMessagingManager.RegisterNamedMessageHandler(Messages[4],ReceiveState);registered=true;
    }
    private void Connected(ulong client)
    {
        Register();if(network.IsHost&&client!=NetworkManager.ServerClientId)guest=client;
    }
    private void LobbyReady(ulong sender,FastBufferReader reader)
    {
        if(!IsHost||sender==NetworkManager.ServerClientId||guest.HasValue&&guest.Value!=sender)return;
        guest=sender;lobbyReady=true;message="참가자 연결 완료 · 대국을 시작할 수 있습니다.";
    }
    public void StartMatch()
    {
        if(!CanStart)return;
        guestReady=false;Send("augment-start","start",guest.Value);LoadMatch();
    }
    private void StartReceived(ulong sender,FastBufferReader reader)
    {if(active&&!IsHost&&sender==NetworkManager.ServerClientId&&Stage==RoomStage.Lobby)LoadMatch();}
    private void LoadMatch()
    {
        Stage=RoomStage.Loading;message="양쪽 대국 화면을 준비하는 중입니다…";match=null;
        SceneManager.LoadSceneAsync(GameScene);
    }
    private void Ready(ulong sender,FastBufferReader reader)
    {
        if(!IsHost||guest!=sender||Stage!=RoomStage.Loading)return;
        guestReady=true;TryBeginMatch();
    }
    private void TryBeginMatch()
    {
        if(!IsHost||!guestReady||match==null||Stage!=RoomStage.Loading)return;
        match.ResetMatch(Guid.NewGuid().GetHashCode());Stage=RoomStage.Playing;message="흑 · 내 차례에만 조작할 수 있습니다.";Broadcast();
    }
    private void Disconnected(ulong client)
    {
        if(!active||leaving)return;
        if(!IsHost||guest==client)
        {
            Stage=RoomStage.Disconnected;lobbyReady=false;message="상대와 연결이 끊겼습니다. 나간 뒤 새 방을 만들어 주세요.";
            if(match!=null)match.WaitForNetwork();
        }
    }
    private void ReceiveCommand(ulong sender,FastBufferReader reader)
    {
        if(!IsHost||guest!=sender||match==null||Stage!=RoomStage.Playing)return;
        try
        {
            reader.ReadValueSafe(out string json);var command=JsonUtility.FromJson<AugmentMatch.Command>(json);
            if(command==null||command.side!=2||command.kind=="meteor-impact")return;
            match.Apply(command);Broadcast();
        }
        catch(Exception e){Debug.LogWarning("대국 명령 거부: "+e.Message);}
    }
    private void ReceiveState(ulong sender,FastBufferReader reader)
    {
        if(IsHost||sender!=NetworkManager.ServerClientId||match==null||!active)return;
        try
        {
            reader.ReadValueSafe(out string json);match.Restore(json);Stage=RoomStage.Playing;message="백 · 내 차례에만 조작할 수 있습니다.";
        }
        catch(Exception e){message="대국 동기화 실패: "+e.Message;match.WaitForNetwork();}
    }
    private void Send(string name,string json,ulong recipient)
    {
        if(network==null||!network.IsListening||network.CustomMessagingManager==null)return;
        using(var writer=new FastBufferWriter(System.Text.Encoding.UTF8.GetByteCount(json)*4+32,Allocator.Temp))
        {writer.WriteValueSafe(json);network.CustomMessagingManager.SendNamedMessage(name,recipient,writer,NetworkDelivery.ReliableFragmentedSequenced);}
    }
    public void SendCommand(AugmentMatch.Command command)
    {if(active&&Stage==RoomStage.Playing)Send("augment-command",JsonUtility.ToJson(command),NetworkManager.ServerClientId);}
    public void Broadcast(){if(guest.HasValue&&match!=null)Send("augment-state",match.Capture(),guest.Value);}
    public async Task Leave(bool returnHome=true)
    {
        if(leaving)return;leaving=true;++operation;busy=false;active=false;
        if(match!=null)match.WaitForNetwork();
        if(network!=null)
        {
            network.OnClientConnectedCallback-=Connected;network.OnClientDisconnectCallback-=Disconnected;
            if(registered&&network.CustomMessagingManager!=null)
                foreach(string name in Messages)network.CustomMessagingManager.UnregisterNamedMessageHandler(name);
            if(network.IsListening)network.Shutdown();
        }
        registered=false;guest=null;guestReady=lobbyReady=false;
        if(session!=null){var old=session;session=null;try{await old.LeaveAsync();}catch(Exception e){Debug.LogWarning(e.Message);}}
        code="";Stage=RoomStage.Offline;message="대국 방식을 선택하세요.";leaving=false;
        if(returnHome&&SceneManager.GetActiveScene().path!=HomeScene)SceneManager.LoadSceneAsync(HomeScene);
    }
    public void StartLocal()
    {
        if(active||busy)return;SceneManager.LoadSceneAsync(GameScene);
    }
    private void OnDestroy()
    {
        if(instance!=this)return;instance=null;
        if(network!=null){network.OnClientConnectedCallback-=Connected;network.OnClientDisconnectCallback-=Disconnected;}
    }
}
