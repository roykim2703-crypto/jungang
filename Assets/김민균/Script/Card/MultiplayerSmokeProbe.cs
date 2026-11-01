#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using UnityEngine;

/// <summary>Opt-in development-build integration probe; never runs during normal play.</summary>
public sealed class MultiplayerSmokeProbe : MonoBehaviour
{
    private string code,report;
    [Serializable] private class Result { public bool success; public string error; public int seed,revision,blackCard,whiteCard,whiteStone; public bool abilityUsed; }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        string[] args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"--augment-test-code");
        if(index<0||index+1>=args.Length||FindAnyObjectByType<MultiplayerSmokeProbe>()!=null)return;
        var go=new GameObject("MultiplayerSmokeProbe");DontDestroyOnLoad(go);
        var probe=go.AddComponent<MultiplayerSmokeProbe>();probe.code=args[index+1];
        int output=Array.IndexOf(args,"--augment-test-report");probe.report=output>=0&&output+1<args.Length?args[output+1]:null;
        probe.StartCoroutine(probe.Run());
    }
    private IEnumerator Run()
    {
        var room=AugmentMultiplayer.Instance;var connecting=room.JoinByCodeAsync(code);
        double deadline=Time.realtimeSinceStartupAsDouble+90;
        while(!connecting.IsCompleted&&Time.realtimeSinceStartupAsDouble<deadline)yield return null;
        if(!room.Active){Finish(new Result{error=room.ConnectionStatus});yield break;}
        Debug.Log("MULTIPLAYER_PROBE_LOBBY_READY");
        while(room.Stage!=AugmentMultiplayer.RoomStage.Playing&&Time.realtimeSinceStartupAsDouble<deadline)yield return null;
        var match=AugmentMatch.Instance;
        if(match==null||room.Stage!=AugmentMultiplayer.RoomStage.Playing){Finish(new Result{error="Match start timed out: "+room.ConnectionStatus});yield break;}
        while(match.Cards.BlackInventory.Count==0&&Time.realtimeSinceStartupAsDouble<deadline)yield return null;
        int[] offers=match.Cards.Capture().whiteOffers;int slot=Array.FindIndex(offers,id=>id!=3);
        if(slot<0)slot=0;
        match.RequestSelection(false,slot);
        while((match.Cards.IsChoosing||match.Board.turn!=2)&&Time.realtimeSinceStartupAsDouble<deadline)yield return null;
        if(match.Cards.WhiteInventory.Count==0||match.Board.turn!=2){Finish(new Result{error="Card selection or black move timed out"});yield break;}
        int id=match.Cards.WhiteInventory[0].Name;
        room.SendCommand(new AugmentMatch.Command{kind="ability",side=2,id=id,x=id==17?9:4,y=id==17?9:4,dx=0,dy=1,power=1,revision=match.Revision});
        while(!match.Cards.WhiteInventory[0].Used&&Time.realtimeSinceStartupAsDouble<deadline)yield return null;
        room.SendCommand(new AugmentMatch.Command{kind="place",side=2,x=10,y=9,revision=match.Revision});
        while(match.Board.At(10,9)!=2&&Time.realtimeSinceStartupAsDouble<deadline)yield return null;
        Finish(new Result{success=match.Board.At(10,9)==2&&match.Cards.WhiteInventory[0].Used,seed=match.Cards.CurrentSeed,
            revision=match.Revision,blackCard=match.Cards.BlackInventory[0].Name,whiteCard=id,whiteStone=match.Board.At(10,9),abilityUsed=match.Cards.WhiteInventory[0].Used});
    }
    private void Finish(Result result)
    {
        string json=JsonUtility.ToJson(result,true);Debug.Log("MULTIPLAYER_PROBE_RESULT "+json);
        if(!string.IsNullOrWhiteSpace(report))File.WriteAllText(report,json);
        // Keep the connection alive until the test process is explicitly closed.
    }
}
#endif
