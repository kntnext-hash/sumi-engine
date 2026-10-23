using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Bokumyaku.Presentation;
public static class BokumyakuAudioStressCheck
{
 static BokumyakuGame game;static AudioSource music;static MethodInfo attack;static double start;static int events,maximum,previous,wraps,calls;static float peak;
 public static object Run(){
 game=UnityEngine.Object.FindFirstObjectByType<BokumyakuGame>();if(!game.SavePath.Contains("ink-qa-"))throw new Exception("isolated only");
 Application.runInBackground=true;EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView")).Focus();
 game.SetAudioSettings(.6f,.4f,.5f,false);music=game.GetComponents<AudioSource>().First(s=>s.loop);
 music.timeSamples=music.clip.samples-music.clip.frequency;previous=music.timeSamples;
 attack=typeof(BokumyakuGame).GetMethod("AudioAttack",BindingFlags.Instance|BindingFlags.NonPublic);
 events=game.PlayedAudioEvents;maximum=wraps=calls=0;peak=0;start=EditorApplication.timeSinceStartup;EditorApplication.update+=Poll;return "3 second loop-boundary and attack burst check";
 }
 static void Poll(){
 for(int i=0;i<100;i++){attack.Invoke(game,null);calls++;}
 maximum=Math.Max(maximum,game.ActiveCombatVoices);
 int now=music.timeSamples;if(now<previous)wraps++;previous=now;
 var data=new float[256];music.GetOutputData(data,0);foreach(float sample in data)peak=Math.Max(peak,Math.Abs(sample));
 if(EditorApplication.timeSinceStartup-start<3)return;EditorApplication.update-=Poll;
 int emitted=game.PlayedAudioEvents-events;
 var result=new{calls,emitted,maximum,wraps,peak,playing=music.isPlaying,passed=maximum<=4&&emitted>0&&emitted<50&&wraps==1&&music.isPlaying&&peak>0,method="Seek to last second then sample loop wrap and mixer output; 100 attack requests per editor update. Not full 80s listening or real combat-event coverage."};
 File.WriteAllText(Path.GetFullPath("../docs/evidence/audio-stress.json"),Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
 }
}
