using UnityEditor;
using UnityEngine;

public static class ImportPaperCard
{
    public static object Run()
    {
        AssetDatabase.Refresh();
        const string path = "Assets/BokumyakuPrototype/Resources/BokumyakuArt/washi_card_07.png";
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) throw new System.InvalidOperationException("Paper card texture is missing: " + path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 100;
        importer.spriteBorder = new Vector4(180, 220, 180, 220);
        importer.maxTextureSize = 2048;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
        return "Imported filled paper card: 1024x1536, border L180/B220/R180/T220, PPU 100";
    }
}
