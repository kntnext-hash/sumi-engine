using System;using System.Linq;using System.IO;using UnityEditor;using UnityEditor.Build.Reporting;using UnityEngine;
public static class BuildPortraitCandidate{
 public static object Run(){if(EditorApplication.isPlaying||EditorApplication.isCompiling||EditorApplication.isUpdating||BuildPipeline.isBuildingPlayer)throw new Exception("Editor must be idle");for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)throw new Exception("Unsaved scene");var output=Path.GetFullPath("../Builds/Bokumyaku-portrait-v7/Bokumyaku.exe");if(File.Exists(output))throw new Exception("Preserve existing build");Directory.CreateDirectory(Path.GetDirectoryName(output));Build(output);return new{finished=true,output};}
 static void Build(string output){var path=Path.GetFullPath("../docs/evidence/portrait-build-v7.json");try{var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/BokumyakuPrototype/Scenes/Bokumyaku.unity"},locationPathName=output,target=BuildTarget.StandaloneWindows64,options=BuildOptions.CleanBuildCache});var s=report.summary;File.WriteAllText(path,Newtonsoft.Json.JsonConvert.SerializeObject(new{result=s.result.ToString(),bytes=s.totalSize,seconds=s.totalTime.TotalSeconds,errors=s.totalErrors,warnings=s.totalWarnings,output,unity=Application.unityVersion,source="62ca6e4",messages=report.steps.SelectMany(step=>step.messages).Where(m=>m.type==LogType.Warning||m.type==LogType.Error).Select(m=>new{type=m.type.ToString(),m.content}).ToArray()},Newtonsoft.Json.Formatting.Indented));}catch(Exception e){File.WriteAllText(path,Newtonsoft.Json.JsonConvert.SerializeObject(new{result="Exception",error=e.ToString()}));throw;}}
}





