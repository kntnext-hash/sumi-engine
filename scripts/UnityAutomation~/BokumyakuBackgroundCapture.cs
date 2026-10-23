using UnityEditor;
using UnityEngine;
using Bokumyaku.Presentation;
public static class BokumyakuBackgroundCapture
{
 public static object Run(){var g=Object.FindFirstObjectByType<BokumyakuGame>();if(!g.SavePath.Contains("ink-qa-"))throw new System.Exception("isolated only");g.SetPaused(true);ScreenCapture.CaptureScreenshot(System.IO.Path.GetFullPath("../docs/evidence/portrait-background-v2.png"));return new{g.Run.Wave,frame=Time.frameCount};}
}
