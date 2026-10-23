using System;using System.IO;using UnityEditor;using UnityEditor.Build.Reporting;using UnityEngine;
public static class BuildRenderCandidate{
 public static object Run(){if(EditorApplication.isPlaying||BuildPipeline.isBuildingPlayer)throw new Exception("Editor must be idle");for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)throw new Exception("Unsaved scene");var output=Path.GetFullPath("../Builds/Bokumyaku-render-probe-v3/Bokumyaku.exe");if(File.Exists(output))throw new Exception("Preserve existing build");Directory.CreateDirectory(Path.GetDirectoryName(output));Build(output);return new{finished=true,output};}
 static void Build(string output){var path=Path.GetFullPath("../docs/evidence/render-build.json");try{var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/BokumyakuPrototype/Scenes/Bokumyaku.unity"},locationPathName=output,target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});var s=report.summary;File.WriteAllText(path,Newtonsoft.Json.JsonConvert.SerializeObject(new{result=s.result.ToString(),bytes=s.totalSize,seconds=s.totalTime.TotalSeconds,errors=s.totalErrors,warnings=s.totalWarnings,output,unity=Application.unityVersion,source="899b702 plus GPU probe and seven-asset validation"},Newtonsoft.Json.Formatting.Indented));}catch(Exception e){File.WriteAllText(path,Newtonsoft.Json.JsonConvert.SerializeObject(new{result="Exception",error=e.ToString()}));throw;}}
}



