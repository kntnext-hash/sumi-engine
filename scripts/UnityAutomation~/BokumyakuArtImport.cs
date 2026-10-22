using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Asset import only. Original generated PNGs stay intact outside Unity's import tree.
public static class BokumyakuArtImport
{
    public static object Run()
    {
        if (EditorApplication.isPlaying || UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().isDirty)
            throw new InvalidOperationException("Stop Play and protect unsaved scene first.");
        const string parent = "Assets/BokumyakuPrototype";
        if (!AssetDatabase.IsValidFolder(parent + "/Resources")) AssetDatabase.CreateFolder(parent, "Resources");
        const string root = parent + "/Resources";
        if (!AssetDatabase.IsValidFolder(root + "/BokumyakuArt")) AssetDatabase.CreateFolder(root, "BokumyakuArt");
        string[] names = { "paper_ivory_01", "landscape_mist_01", "enso_frame_01", "weapon_point_01", "weapon_pierce_01", "weapon_bleed_01" };
        var rows = new List<object>();
        long textureBytes = 0;
        for (int i = 0; i < names.Length; i++)
        {
            string source = Path.GetFullPath("../ArtSource~/Bokumyaku/batch-001/" + names[i] + ".png");
            string path = root + "/BokumyakuArt/" + names[i] + ".png";
            byte[] input = File.ReadAllBytes(source);
            if (File.Exists(path))
            {
                byte[] existing = File.ReadAllBytes(path);
                if (!System.Linq.Enumerable.SequenceEqual(input, existing)) throw new IOException("Protect different existing asset: " + path);
            }
            else File.Copy(source, path);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = true;
            importer.alphaSource = i < 2 ? TextureImporterAlphaSource.None : TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = i >= 2;
            importer.mipmapEnabled = false;
            importer.isReadable = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = i == 1 ? 1024 : i < 3 ? 512 : 256;
            importer.SaveAndReimport();
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture == null) throw new IOException("Texture import failed: " + path);
            long bytes = UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(texture);
            textureBytes += bytes;
            rows.Add(new { name = names[i], path, sourceBytes = input.Length, width = texture.width, height = texture.height,
                format = texture.format.ToString(), alpha = i >= 2, textureBytes = bytes, sourcePixelsUnchanged = true });
        }
        AssetDatabase.SaveAssets();
        var report = new { unity = Application.unityVersion, assets = rows, textureBytes,
            classification = "Editor import and current texture allocation; not device performance or total game memory" };
        File.WriteAllText(Path.GetFullPath("../docs/evidence/bokumyaku-art-import-v4.json"), Newtonsoft.Json.JsonConvert.SerializeObject(report, Newtonsoft.Json.Formatting.Indented));
        return report;
    }
}
