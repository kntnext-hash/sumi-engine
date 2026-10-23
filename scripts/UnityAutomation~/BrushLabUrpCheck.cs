using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using ConnectPiece.SumiEngine;
using ConnectPiece.SumiEngine.Core;
public static class BrushLabUrpCheck
{
 public static object Arm(){if(EditorApplication.isPlaying||UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new Exception("Clean stopped scene required");if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().path!="Assets/BrushLab/Scenes/BrushLab.unity")SessionState.SetString("BrushQA.return",UnityEngine.SceneManagement.SceneManager.GetActiveScene().path);EditorSceneManager.OpenScene("Assets/BrushLab/Scenes/BrushLab.unity");EditorApplication.isPlaying=true;return "Brush Lab Play";}
 public static object Before()=>Capture("before");public static object After()=>Capture("after");
 static object Capture(string phase){var lab=UnityEngine.Object.FindFirstObjectByType<SumiBrushLab>();if(lab==null)throw new Exception("Missing Brush Lab");var canvas=lab.GetComponent<SumiCanvas>();canvas.diffusionFps=0;InkStudy.DrawEffects(canvas.Core);canvas.Upload();var bytes=canvas.ExportPng();File.WriteAllBytes(Path.GetFullPath("../docs/evidence/brushlab-urp-"+phase+".png"),bytes);ScreenCapture.CaptureScreenshot(Path.GetFullPath("../docs/evidence/brushlab-urp-"+phase+"-screen.png"));var result=new{phase,canvas.width,canvas.height,bytes=bytes.Length,pipeline=UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline?.GetType().Name??"Built-in",hash=Convert.ToBase64String(System.Security.Cryptography.SHA256.Create().ComputeHash(bytes))};File.WriteAllText(Path.GetFullPath("../docs/evidence/brushlab-urp-"+phase+".json"),Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;}
 public static object Restore(){if(EditorApplication.isPlaying)throw new Exception("Stop first");EditorSceneManager.OpenScene(SessionState.GetString("BrushQA.return","Assets/BokumyakuPrototype/Scenes/Bokumyaku.unity"));return "restored";}
}

