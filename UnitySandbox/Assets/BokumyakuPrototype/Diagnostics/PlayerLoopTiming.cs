using System;
using UnityEngine.LowLevel;
using Bokumyaku.Presentation;
namespace Bokumyaku.Diagnostics
{
    // Explicit QA only. Measures main-thread phase duration, not GPU execution time.
    public static class PlayerLoopTiming
    {
        static PlayerLoopSystem original;
        static bool installed;
        sealed class StartMarker { }
        sealed class EndMarker { }
        sealed class Phase
        {
            readonly string name;
            BattleTiming.Scope scope;
            public Phase(string name) { this.name = name; }
            public void Begin() { scope = BattleTiming.Measure(name); }
            public void End() { scope.Dispose(); }
        }
        public static void Install()
        {
            if (installed) return;
            original = PlayerLoop.GetCurrentPlayerLoop();
            var loop = original;
            loop.subSystemList = (PlayerLoopSystem[])original.subSystemList.Clone();
            for (int i = 0; i < loop.subSystemList.Length; i++)
            {
                var system = loop.subSystemList[i];
                var children = system.subSystemList;
                if (children == null || children.Length == 0) continue;
                if (system.type.Name == "PostLateUpdate")
                {
                    var detailed = new PlayerLoopSystem[children.Length * 3];
                    for (int j = 0; j < children.Length; j++)
                    {
                        var childPhase = new Phase("PostLate." + children[j].type.Name);
                        detailed[j * 3] = new PlayerLoopSystem { type = typeof(StartMarker), updateDelegate = childPhase.Begin };
                        detailed[j * 3 + 1] = children[j];
                        detailed[j * 3 + 2] = new PlayerLoopSystem { type = typeof(EndMarker), updateDelegate = childPhase.End };
                    }
                    children = detailed;
                }
                var phase = new Phase("Loop." + system.type.Name);
                var wrapped = new PlayerLoopSystem[children.Length + 2];
                wrapped[0] = new PlayerLoopSystem { type = typeof(StartMarker), updateDelegate = phase.Begin };
                Array.Copy(children, 0, wrapped, 1, children.Length);
                wrapped[wrapped.Length - 1] = new PlayerLoopSystem { type = typeof(EndMarker), updateDelegate = phase.End };
                system.subSystemList = wrapped;
                loop.subSystemList[i] = system;
            }
            PlayerLoop.SetPlayerLoop(loop);
            installed = true;
        }
        public static void Restore()
        {
            if (!installed) return;
            PlayerLoop.SetPlayerLoop(original);
            installed = false;
        }
    }
}
