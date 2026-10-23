using UnityEditor;
using UnityEngine;
public static class ImportUpgradeIcons
{
    public static object Run()
    {
        AssetDatabase.Refresh();
        foreach (var name in new[] { "attack", "speed", "flow", "branch", "width" })
        {
            var path="Assets/BokumyakuPrototype/Resources/BokumyakuArt/upgrade_"+name+"_08.png";
            var importer=AssetImporter.GetAtPath(path) as TextureImporter;
            if(importer==null) throw new System.InvalidOperationException(path);
            importer.textureType=TextureImporterType.Sprite;
            importer.spriteImportMode=SpriteImportMode.Single;
            importer.spritePixelsPerUnit=100;
            importer.spriteBorder=Vector4.zero;
            importer.maxTextureSize=512;
            importer.npotScale=TextureImporterNPOTScale.None;
            importer.mipmapEnabled=false;
            importer.isReadable=false;
            importer.alphaIsTransparency=true;
            importer.wrapMode=TextureWrapMode.Clamp;
            importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }
        return "Imported five upgrade sprites, max512, no mipmaps or CPU copy";
    }
}
