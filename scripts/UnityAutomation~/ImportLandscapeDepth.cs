using System;using System.IO;using System.Linq;using UnityEditor;using UnityEngine;
public static class ImportLandscapeDepth {
public static object Run(){
if(EditorApplication.isPlaying||UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new Exception("Clean stopped scene required");
const string path="Assets/BokumyakuPrototype/Resources/BokumyakuArt/landscape_depth_05.png";
string source=Path.GetFullPath("../art-source/delivery-v5/landscape_depth_05.png");
if(File.Exists(path)){if(!File.ReadAllBytes(source).SequenceEqual(File.ReadAllBytes(path)))throw new Exception("Protect existing asset");}else File.Copy(source,path);
AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
var i=(TextureImporter)AssetImporter.GetAtPath(path);i.textureType=TextureImporterType.Default;i.sRGBTexture=true;i.alphaSource=TextureImporterAlphaSource.None;i.mipmapEnabled=false;i.isReadable=false;i.wrapMode=TextureWrapMode.Clamp;i.filterMode=FilterMode.Bilinear;i.npotScale=TextureImporterNPOTScale.None;i.maxTextureSize=2048;i.textureCompression=TextureImporterCompression.CompressedHQ;i.SaveAndReimport();
var t=AssetDatabase.LoadAssetAtPath<Texture2D>(path);var report=new{width=t.width,height=t.height,format=t.format.ToString(),readable=t.isReadable,sourceUnchanged=File.ReadAllBytes(source).SequenceEqual(File.ReadAllBytes(path)),runtimeBytes=UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(t)};
File.WriteAllText(Path.GetFullPath("../docs/evidence/landscape-depth-import.json"),Newtonsoft.Json.JsonConvert.SerializeObject(report));return report;
}}
