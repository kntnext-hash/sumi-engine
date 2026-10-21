using System;
using System.Collections;
using System.IO;
using ConnectPiece.SumiEngine;
using ConnectPiece.SumiEngine.Core;
using UnityEngine;

// Explicit command-line smoke mode only. Ordinary desktop launches do not create this object.
public sealed class BrushLabPlayerProbe : MonoBehaviour
{
    string output;
    [Serializable] sealed class Result
    {
        public string unityVersion, error;
        public bool passed, pngMatches, replayMatches;
        public int startFrame, endFrame, width, height, studyStrokes;
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        if (Application.isEditor) return;
        var args = Environment.GetCommandLineArgs();
        int i = Array.IndexOf(args, "--sumi-smoke-output");
        if (i < 0 || i + 1 >= args.Length) return;
        var probe = new GameObject("SUMI explicit player probe").AddComponent<BrushLabPlayerProbe>();
        probe.output = Path.GetFullPath(args[i + 1]);
        Application.runInBackground = true;
        Application.targetFrameRate = 60;
    }
    IEnumerator Start()
    {
        var result = new Result { unityVersion = Application.unityVersion, startFrame = Time.frameCount };
        // Allow the launch splash and the window's initial render target to finish.
        float readyAt=Time.realtimeSinceStartup+3f;
        while(Time.realtimeSinceStartup<readyAt)yield return null;
        try
        {
            Directory.CreateDirectory(output);
            var canvas = FindFirstObjectByType<SumiCanvas>();
            if (canvas == null) throw new InvalidOperationException("Brush Lab canvas missing in Player scene.");
            canvas.Initialize(); canvas.diffusionFps = 0;
            var pointer = new InkPointerStroke(canvas.Core);
            pointer.Begin(new InkPoint(20, 40, 0), InkBrushSettings.Dense, 731);
            pointer.Move(new InkPoint(180, 260, .5f)); pointer.End();
            if (canvas.Core.Paper.TotalPigment() <= 0) throw new InvalidOperationException("Player input produced no ink.");
            InkStudy.DrawEffects(canvas.Core);
            result.studyStrokes = canvas.Core.Session.strokes.Count;
            var replay = new SumiEngineCore(canvas.width, canvas.height, canvas.paperSeed);
            replay.Replay(canvas.Core.Session);
            var a = new byte[canvas.width * canvas.height * 4]; var b = new byte[a.Length];
            canvas.Core.Paper.RenderRgba(a); replay.Paper.RenderRgba(b);
            result.replayMatches = Equal(a,b);
            canvas.inkColor = new Color32(12,12,13,255); canvas.displayDensity = 7;
            byte[] png = canvas.ExportPng();
            File.WriteAllBytes(Path.Combine(output,"player-paper.png"),png);
            var decoded = new Texture2D(2,2);
            ImageConversion.LoadImage(decoded,png);
            var actual = decoded.GetPixels32(); var expected = canvas.Texture.GetPixels32();
            result.pngMatches = actual.Length == expected.Length;
            for (int i=0; result.pngMatches && i<actual.Length;i++) result.pngMatches = actual[i].Equals(expected[i]);
            result.width=decoded.width;result.height=decoded.height;Destroy(decoded);
            ScreenCapture.CaptureScreenshot(Path.Combine(output,"player-screen.png"));
        }
        catch(Exception ex) { result.error=ex.ToString(); }
        for(int i=0;i<10;i++) yield return null;
        result.endFrame=Time.frameCount;
        result.passed=result.error==null && result.pngMatches && result.replayMatches && result.studyStrokes==50 && result.endFrame>result.startFrame;
        try { File.WriteAllText(Path.Combine(output,"player-smoke.json"),JsonUtility.ToJson(result,true)); }
        catch(Exception ex) { Debug.LogException(ex); result.passed=false; }
        Application.Quit(result.passed ? 0 : 2);
    }
    static bool Equal(byte[] a,byte[] b)
    { if(a.Length!=b.Length)return false;for(int i=0;i<a.Length;i++)if(a[i]!=b[i])return false;return true; }
}
