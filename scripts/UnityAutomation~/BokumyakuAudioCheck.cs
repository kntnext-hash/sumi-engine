using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Bokumyaku.Presentation;
public static class BokumyakuAudioCheck
{
 static BokumyakuGame game;static AudioSource music;static int sample;static double start;
 public static object Run()
 {
 game=UnityEngine.Object.FindFirstObjectByType<BokumyakuGame>();if(!game.SavePath.Contains("ink-qa-"))throw new Exception("isolated only");
 game.SetPage(BokumyakuGame.GamePage.Home);game.SetAudioSettings(.6f,.4f,.5f,false);
 music=game.GetComponents<AudioSource>().First(s=>s.loop);sample=music.timeSamples;start=EditorApplication.timeSinceStartup;
 EditorApplication.update+=Finish;return "sample music progress and mute/persistence";
 }
 static void Finish()
 {
 if(EditorApplication.timeSinceStartup-start<2)return;EditorApplication.update-=Finish;
 bool advancing=music.timeSamples>sample;game.SetAudioSettings(.6f,.4f,.5f,true);
 bool silent=game.GetComponents<AudioSource>().All(s=>s.mute||s.volume==0);
 game.ReloadAudioSettings();var a=game.AudioSettings;bool stored=a.Muted&&Mathf.Approximately(a.Master,.6f)&&Mathf.Approximately(a.Music,.4f)&&Mathf.Approximately(a.Effects,.5f);
 game.SetAudioSettings(.6f,.4f,.5f,false);
 int listeners=UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Count(l=>l.enabled);
 var result=new{loaded=game.LoadedAudioCount,advancing,silent,stored,listeners,game.MusicPlaying,passed=game.LoadedAudioCount==8&&advancing&&silent&&stored&&listeners==1,method="Unity playback state/sample clock and mute/settings inspection, not audible speaker or subjective listening test"};
 File.WriteAllText(Path.GetFullPath("../docs/evidence/audio-integration.json"),Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
 ScreenCapture.CaptureScreenshot(Path.GetFullPath("../docs/evidence/audio-settings.png"));
 }
}
