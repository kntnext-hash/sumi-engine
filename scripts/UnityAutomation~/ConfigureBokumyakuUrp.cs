using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
public static class ConfigureBokumyakuUrp
{
 public static object Run()
 {
  if(EditorApplication.isPlaying||UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new Exception("Clean stopped scene required");
  const string folder="Assets/BokumyakuPrototype/Rendering";if(!AssetDatabase.IsValidFolder(folder))AssetDatabase.CreateFolder("Assets/BokumyakuPrototype","Rendering");
  var renderer=AssetDatabase.LoadAssetAtPath<Renderer2DData>(folder+"/Bokumyaku2D.asset");if(renderer==null){renderer=ScriptableObject.CreateInstance<Renderer2DData>();AssetDatabase.CreateAsset(renderer,folder+"/Bokumyaku2D.asset");}
  var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(folder+"/BokumyakuURP.asset");if(pipeline==null){pipeline=UniversalRenderPipelineAsset.Create(renderer);AssetDatabase.CreateAsset(pipeline,folder+"/BokumyakuURP.asset");}
  pipeline.msaaSampleCount=1;pipeline.supportsHDR=false;
  GraphicsSettings.defaultRenderPipeline=pipeline;int previous=QualitySettings.GetQualityLevel();for(int i=0;i<QualitySettings.names.Length;i++){QualitySettings.SetQualityLevel(i,false);QualitySettings.renderPipeline=pipeline;}QualitySettings.SetQualityLevel(previous,false);
  EditorUtility.SetDirty(pipeline);AssetDatabase.SaveAssets();
  return new{pipeline=AssetDatabase.GetAssetPath(GraphicsSettings.defaultRenderPipeline),renderer=renderer.GetType().Name,qualityLevels=QualitySettings.names.Length};
 }
}
