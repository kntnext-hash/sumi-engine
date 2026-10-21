using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Sumi.Automation.Editor
{
    public static class AutomationScene
    {
        public const string ScenePath = "Assets/CodexAutomation/Scenes/ConnectionTest.unity";

        [MenuItem("Tools/SUMI Engine/Automation/Generate Connection Test")]
        public static void Generate()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play Mode first.");
            if (File.Exists(ScenePath))
                throw new IOException("Test scene already exists; inspect it instead of overwriting it.");
            Directory.CreateDirectory("Assets/CodexAutomation/Scenes");
            AssetDatabase.Refresh();
            var previous = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);
                var camera = new GameObject("Camera", typeof(Camera)).GetComponent<Camera>();
                camera.transform.position = new Vector3(0, 1, -5);
                camera.transform.LookAt(Vector3.zero);
                var light = new GameObject("Directional Light", typeof(Light)).GetComponent<Light>();
                light.type = LightType.Directional;
                light.transform.rotation = Quaternion.Euler(50, -30, 0);
                var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.name = "Cube";
                cube.AddComponent<AutomationRotator>();
                new GameObject("Empty");
                if (scene.rootCount != 4 || !EditorSceneManager.SaveScene(scene, ScenePath))
                    throw new InvalidOperationException("Scene generation verification failed.");
                Debug.Log("SUMI_AUTOMATION_SCENE_CREATED: " + ScenePath + " roots=4");
            }
            finally
            {
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
                EditorSceneManager.CloseScene(scene, true);
            }
        }
    }
}
