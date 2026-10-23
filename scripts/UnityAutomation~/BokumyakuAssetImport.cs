using UnityEditor;
public static class BokumyakuAssetImport
{
    public static object Run()
    {
        AssetDatabase.Refresh();
        var path="Assets/BokumyakuPrototype/Resources/BokumyakuArt/landscape_portrait_quiet_02.png";
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType=TextureImporterType.Default;importer.mipmapEnabled=false;importer.isReadable=false;importer.maxTextureSize=2048;
        importer.wrapMode=UnityEngine.TextureWrapMode.Clamp;importer.SaveAndReimport();return "portrait background imported";
    }
}
