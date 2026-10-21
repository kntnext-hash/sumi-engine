using ConnectPiece.SumiEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ConnectPiece.SumiEngine.Editor
{
    public static class SumiLabMenu
    {
        // Also invoked by Setup-UnitySandbox.ps1 after the package has compiled.
        [MenuItem("Tools/SUMI Engine/Generate Missing Sample Scenes")]
        public static void GenerateSampleScenes()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new System.InvalidOperationException("Stop Play mode before generating scenes.");
            // Additive creation leaves all existing scenes, including unsaved work, open.
            EnsureFolder("Assets", "BrushLab");
            EnsureFolder("Assets/BrushLab", "Scenes");
            GenerateScene("Assets/BrushLab/Scenes/BrushLab.unity", false);
            GenerateScene("Assets/BrushLab/Scenes/BranchGrowth.unity", true);
            AssetDatabase.SaveAssets();
            Debug.Log("SUMI ENGINE: sample scenes ready in Assets/BrushLab/Scenes; existing scenes were preserved.");
        }

        static void EnsureFolder(string parent, string name)
        {
            if (!AssetDatabase.IsValidFolder(parent + "/" + name))
                AssetDatabase.CreateFolder(parent, name);
        }

        static void GenerateScene(string path, bool growth)
        {
            if (System.IO.File.Exists(path)) return;
            var previous = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            try
            {
                UnityEngine.SceneManagement.SceneManager.SetActiveScene(scene);
                var obj = new GameObject(growth ? "SUMI - Branch Growth Demo" : "SUMI - Brush Lab");
                obj.AddComponent<SumiCanvas>();
                if (growth) obj.AddComponent<SumiVeinDemo>();
                else obj.AddComponent<SumiBrushLab>();
                if (!EditorSceneManager.SaveScene(scene, path))
                    throw new System.IO.IOException("Could not save " + path);
            }
            finally
            {
                if (previous.IsValid() && previous.isLoaded)
                    UnityEngine.SceneManagement.SceneManager.SetActiveScene(previous);
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        [MenuItem("Tools/SUMI Engine/Create Branch Growth Demo in Scene")]
        public static void CreateGrowthDemo()
        {
            var obj = new GameObject("SUMI - Branch Growth Demo");
            Undo.RegisterCreatedObjectUndo(obj, "Create SUMI Growth Demo");
            obj.AddComponent<SumiCanvas>();
            obj.AddComponent<SumiVeinDemo>();
            Selection.activeGameObject = obj;
            EditorSceneManager.MarkSceneDirty(obj.scene);
            Debug.Log("SUMI ENGINE: Press Play to watch procedural ink branches grow.");
        }

        [MenuItem("Tools/SUMI Engine/Create Brush Lab in Scene")]
        public static void Create()
        {
            var obj = new GameObject("SUMI - Brush Lab");
            Undo.RegisterCreatedObjectUndo(obj, "Create SUMI Brush Lab");
            obj.AddComponent<SumiCanvas>();
            obj.AddComponent<SumiBrushLab>();
            Selection.activeGameObject = obj;
            EditorSceneManager.MarkSceneDirty(obj.scene);
            Debug.Log("SUMI ENGINE: Press Play, then drag in the Game view. No camera or UI setup needed.");
        }
    }
}
