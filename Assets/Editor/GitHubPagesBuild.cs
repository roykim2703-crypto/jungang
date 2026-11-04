using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Build a self-contained Web player that works on static GitHub Pages hosting.</summary>
[InitializeOnLoad]
public static class GitHubPagesBuild
{
    private const string Pending="Jungang.PagesBuild.Pending";
    private const string ReportPath="Builds/pages-build-report.json";
    [Serializable] private class Report { public string status,error; public long bytes; public double seconds; }
    static GitHubPagesBuild(){EditorApplication.update+=Tick;}
    public static void BuildForBatch()=>Build();
    [MenuItem("Tools/Build/GitHub Pages Web Player")]
    public static void Queue()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play Mode first.");
        if(!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.WebGL,BuildTarget.WebGL))throw new InvalidOperationException("WebGL module is not installed.");
        EditorSceneManager.SaveOpenScenes();AssetDatabase.SaveAssets();
        Write(new Report{status="queued"});SessionState.SetBool(Pending,true);
        if(EditorUserBuildSettings.activeBuildTarget!=BuildTarget.WebGL)
        {
            if(!EditorUserBuildSettings.SwitchActiveBuildTargetAsync(BuildTargetGroup.WebGL,BuildTarget.WebGL))
            {SessionState.SetBool(Pending,false);Write(new Report{status="failed",error="Could not switch to WebGL."});}
        }
    }
    private static void Tick()
    {
        if(!SessionState.GetBool(Pending,false)||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorUserBuildSettings.activeBuildTarget!=BuildTarget.WebGL)return;
        SessionState.SetBool(Pending,false);Build();
    }
    private static void Build()
    {
        try
        {
            EditorSceneManager.SaveOpenScenes();
            Write(new Report{status="building"});
            PlayerSettings.WebGL.compressionFormat=WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback=true;
            PlayerSettings.WebGL.dataCaching=true;
            PlayerSettings.WebGL.template="APPLICATION:Default";
            PlayerSettings.WebGL.exceptionSupport=WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{
                scenes=new[]{"Assets/Scenes/Home.unity","Assets/Scenes/Game.unity","Assets/Scenes/Result.unity"},
                locationPathName="Builds/WebGL",target=BuildTarget.WebGL,options=BuildOptions.None});
            var summary=report.summary;
            if(summary.result!=BuildResult.Succeeded)throw new Exception("Web build failed: "+summary.result+" ("+summary.totalErrors+" errors)");
            File.WriteAllText("Builds/WebGL/.nojekyll","");
            File.Copy("Assets/Fonts/NanumGothic-OFL.txt","Builds/WebGL/NanumGothic-OFL.txt",true);
            WebPlayerLayout.Apply("Builds/WebGL");
            Write(new Report{status="completed",bytes=(long)summary.totalSize,seconds=summary.totalTime.TotalSeconds});
        }
        catch(Exception e){Write(new Report{status="failed",error=e.ToString()});Debug.LogException(e);}
    }
    private static void Write(Report report)
    {Directory.CreateDirectory("Builds");File.WriteAllText(ReportPath,JsonUtility.ToJson(report,true));}
}
