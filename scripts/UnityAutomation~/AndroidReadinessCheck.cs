using System;using System.IO;using UnityEditor;using UnityEngine;
public static class AndroidReadinessCheck
{
 public static object Run()
 {
  string root=Path.Combine(EditorApplication.applicationContentsPath,"PlaybackEngines/AndroidPlayer");
  var report=new {
   unity=Application.unityVersion,
   supported=BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android,BuildTarget.Android),
   androidModule=Directory.Exists(root),sdk=Directory.Exists(Path.Combine(root,"SDK")),
   ndk=Directory.Exists(Path.Combine(root,"NDK")),jdk=Directory.Exists(Path.Combine(root,"OpenJDK")),
   activeTarget=EditorUserBuildSettings.activeBuildTarget.ToString(),
   orientation=PlayerSettings.defaultInterfaceOrientation.ToString(),
   landscapeLeft=PlayerSettings.allowedAutorotateToLandscapeLeft,
   landscapeRight=PlayerSettings.allowedAutorotateToLandscapeRight,
   identifier=PlayerSettings.GetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android),
   minimumSdk=PlayerSettings.Android.minSdkVersion.ToString(),
   architectures=PlayerSettings.Android.targetArchitectures.ToString(),
   backend=PlayerSettings.GetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android).ToString(),
   note="Read-only readiness inspection. Bundled dependency directories only; no APK or device test."
  };
  File.WriteAllText(Path.GetFullPath("../docs/evidence/android-readiness.json"),Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));return report;
 }
}
