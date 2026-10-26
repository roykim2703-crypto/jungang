using System;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Collections;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Multiplayer;

/// <summary>Relay invite codes, host authority, reliable ordered snapshots; no incoming state is trusted.</summary>
public sealed class AugmentMultiplayer : MonoBehaviour
{
    private AugmentMatch match;
    private NetworkManager network;
    private ISession session;
    private bool busy, active, registered;
    private string code="",message="로컬 대국";
    private ulong? guest;
    public bool Active=>active;
    public bool IsHost=>network!=null&&network.IsHost;
    public string InvitationCode=>code;
    public string ConnectionStatus=>message;
    public bool IsConnecting=>busy;
    public Task JoinByCodeAsync(string invitationCode)
    {
        if(busy||active)throw new InvalidOperationException("이미 연결 중이거나 방에 참가한 상태입니다.");
        code=invitationCode;
        return Connect(false);
    }
    private void Awake(){match=GetComponent<AugmentMatch>();}
    public async Task Connect(bool host)
    {
        if(busy||active)return;busy=true;message="연결 중…";match.Waiting=true;
        try
        {
            await UnityServices.InitializeAsync();
            if(!AuthenticationService.Instance.IsSignedIn)await AuthenticationService.Instance.SignInAnonymouslyAsync();
            network=NetworkManager.Singleton;
            if(network==null)
            {
                var root=new GameObject("AugmentNetwork");
                network=root.AddComponent<NetworkManager>();
                var transport=root.AddComponent<UnityTransport>();network.NetworkConfig.NetworkTransport=transport;
            }
            network.NetworkConfig.EnableSceneManagement=false;
            network.OnClientConnectedCallback+=Connected;
            network.OnClientDisconnectCallback+=Disconnected;
            if(host)
            {
                session=await MultiplayerService.Instance.CreateSessionAsync(new SessionOptions{MaxPlayers=2,IsPrivate=true}.WithRelayNetwork());
                code=session.Code;message="초대코드: "+code+" · 참가자 대기";
            }
            else
            {
                session=await MultiplayerService.Instance.JoinSessionByCodeAsync(code.Trim().ToUpperInvariant());
                message="백 · 방장과 동기화 중";
            }
            active=true;Register();
            if(!host&&network.IsConnectedClient)Send("augment-ready","ready",NetworkManager.ServerClientId);
        }
        catch(Exception e){message="연결 실패: "+e.Message;await Leave();}
        finally{busy=false;}
    }
    private void Register()
    {
        if(registered||network==null||network.CustomMessagingManager==null)return;
        network.CustomMessagingManager.RegisterNamedMessageHandler("augment-ready",Ready);
        network.CustomMessagingManager.RegisterNamedMessageHandler("augment-command",ReceiveCommand);
        network.CustomMessagingManager.RegisterNamedMessageHandler("augment-state",ReceiveState);
        registered=true;
    }
    private void Connected(ulong client)
    {
        Register();
        if(network.IsHost&&client!=NetworkManager.ServerClientId){guest=client;}
        if(!network.IsHost&&client==network.LocalClientId)Send("augment-ready","ready",NetworkManager.ServerClientId);
    }
    private void Ready(ulong sender,FastBufferReader reader)
    {
        if(!IsHost||sender==NetworkManager.ServerClientId)return;
        if(guest.HasValue&&guest.Value!=sender)return;
        bool first=match.Waiting;guest=sender;
        if(first)match.ResetMatch(Guid.NewGuid().GetHashCode());
        message="흑 · 참가자 연결됨 · "+code;Broadcast();
    }
    private void Disconnected(ulong client)
    {
        if(!active)return;
        if(!IsHost||guest==client){match.Waiting=true;message="연결이 끊겼습니다. 로컬로 나간 뒤 새 초대코드로 시작하세요.";}
    }
    private void ReceiveCommand(ulong sender,FastBufferReader reader)
    {
        if(!IsHost||guest!=sender)return;
        reader.ReadValueSafe(out string json);
        try
        {
            var command=JsonUtility.FromJson<AugmentMatch.Command>(json);
            if(command==null||command.side!=2)return;
            match.Apply(command);Broadcast();
        }
        catch(Exception e){Debug.LogWarning("증강 명령 거부: "+e.Message);}
    }
    private void ReceiveState(ulong sender,FastBufferReader reader)
    {
        if(IsHost||sender!=NetworkManager.ServerClientId)return;
        reader.ReadValueSafe(out string json);match.Restore(json);message="백 · 연결됨 · "+code;
    }
    private void Send(string name,string json,ulong recipient)
    {
        using(var writer=new FastBufferWriter(System.Text.Encoding.UTF8.GetByteCount(json)*4+32,Allocator.Temp))
        {writer.WriteValueSafe(json);network.CustomMessagingManager.SendNamedMessage(name,recipient,writer,NetworkDelivery.ReliableFragmentedSequenced);}
    }
    public void SendCommand(AugmentMatch.Command command)=>Send("augment-command",JsonUtility.ToJson(command),NetworkManager.ServerClientId);
    public void Broadcast(){if(guest.HasValue)Send("augment-state",match.Capture(),guest.Value);}
    public async Task Leave()
    {
        active=false;
        if(network!=null)
        {
            network.OnClientConnectedCallback-=Connected;network.OnClientDisconnectCallback-=Disconnected;
            if(registered&&network.CustomMessagingManager!=null)
                foreach(string name in new[]{"augment-ready","augment-command","augment-state"})network.CustomMessagingManager.UnregisterNamedMessageHandler(name);
            network.Shutdown();registered=false;guest=null;
        }
        if(session!=null){try{await session.LeaveAsync();}catch(Exception e){Debug.LogWarning(e.Message);}session=null;}
        if(match!=null){match.Waiting=false;match.ResetMatch(Guid.NewGuid().GetHashCode());}
    }
    private async void OnDestroy(){if(session!=null||active)await Leave();}
}
