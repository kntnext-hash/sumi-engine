using UnityEditor.TestTools.TestRunner.Api;using System.Reflection;
public static class TestRunStateCheck{public static object Run(){return new{active=(bool)typeof(TestRunnerApi).GetMethod("IsRunActive",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null),compiling=UnityEditor.EditorApplication.isCompiling,playing=UnityEditor.EditorApplication.isPlaying};}}
