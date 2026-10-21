using System;
using System.IO;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

// Run through Pipeline's script run command; no project assembly or scene changes.
public static class BokumyakuEvidence
{
    static TestRunnerApi api;
    static Recorder recorder;

    public static string Run()
    {
        if (UnityEditor.EditorApplication.isPlaying)
            throw new InvalidOperationException("Stop Play mode before EditMode tests.");
        if (api != null) throw new InvalidOperationException("Evidence run already started.");
        api = ScriptableObject.CreateInstance<TestRunnerApi>();
        recorder = new Recorder();
        api.RegisterCallbacks(recorder);
        api.Execute(new ExecutionSettings(new Filter
        {
            testMode = TestMode.EditMode,
            assemblyNames = new[] { "Bokumyaku.Tests" }
        }));
        return "Started BOKUMYAKU EditMode; await docs/evidence/bokumyaku-editmode-v2.xml.";
    }

    sealed class Recorder : ICallbacks
    {
        public void RunStarted(ITestAdaptor tests) { }
        public void TestStarted(ITestAdaptor test) { }
        public void TestFinished(ITestResultAdaptor result) { }
        public void RunFinished(ITestResultAdaptor result)
        {
            var root = Path.GetFullPath(Path.Combine(Application.dataPath, "../.."));
            var path = Path.Combine(root, "docs/evidence/bokumyaku-editmode-v2.xml");
            TestRunnerApi.SaveResultToFile(result, path);
            Debug.Log($"BOKUMYAKU XML: {result.PassCount} passed / {result.FailCount} failed -> {path}");
            api.UnregisterCallbacks(this);
            UnityEngine.Object.DestroyImmediate(api);
            api = null;
            recorder = null;
        }
    }
}
