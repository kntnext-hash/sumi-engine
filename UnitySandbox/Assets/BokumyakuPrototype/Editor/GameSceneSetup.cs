using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Bokumyaku.Presentation;
namespace Bokumyaku.Editor {
public static class GameSceneSetup {
 public const string ScenePath="Assets/BokumyakuPrototype/Scenes/Bokumyaku.unity";
 [MenuItem("Tools/Bokumyaku/Create Game Scene")]
 public static void Create() {
  if(EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play first.");
  if(File.Exists(ScenePath)) return;
  Directory.CreateDirectory("Assets/BokumyakuPrototype/Scenes");
  var old=SceneManager.GetActiveScene();
  var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
  try {
   SceneManager.SetActiveScene(scene);
   var c=new GameObject("Main Camera",typeof(Camera)).GetComponent<Camera>();
   c.tag="MainCamera"; c.clearFlags=CameraClearFlags.SolidColor;c.backgroundColor=new Color(.93f,.91f,.85f);c.orthographic=true;c.transform.position=new Vector3(0,0,-10);
   new GameObject("Bokumyaku",typeof(BokumyakuGame));
   if(!EditorSceneManager.SaveScene(scene,ScenePath))throw new IOException("Scene save failed.");
  } finally { if(old.IsValid()&&old.isLoaded)SceneManager.SetActiveScene(old); EditorSceneManager.CloseScene(scene,true); }
 }
}
}
