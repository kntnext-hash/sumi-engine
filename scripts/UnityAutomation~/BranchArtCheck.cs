using System;using System.IO;using UnityEditor;using UnityEngine;using Bokumyaku.Presentation;using Bokumyaku.Simulation;
public static class BranchArtCheck{
 public static object Run(){var g=UnityEngine.Object.FindFirstObjectByType<BokumyakuGame>();if(!g.SavePath.Contains("ink-qa-"))throw new Exception("Isolated save required");g.SetBuild(BuildPreset.Farming);g.SetWeapon(WeaponKind.Point);g.TryStartRun(42);for(int i=0;i<6000;i++)g.Run.Tick();g.SetPaused(true);return new{g.Run.Wave,g.Run.TickCount};}
 public static object Capture(){var g=UnityEngine.Object.FindFirstObjectByType<BokumyakuGame>();ScreenCapture.CaptureScreenshot(Path.GetFullPath("../docs/evidence/branch-art-wave17.png"));return new{g.Run.Wave,g.DirtyInkUploads,g.BranchOverlayRebuilds};}
}
