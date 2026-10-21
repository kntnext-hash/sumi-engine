using ConnectPiece.SumiEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ConnectPiece.SumiEngine.Editor
{
    public static class SumiLabMenu
    {
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
