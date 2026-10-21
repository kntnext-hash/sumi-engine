# Suggested Codex continuation task

Inspect the local `com.connectpiece.sumi-engine` Unity package without replacing its architecture.

1. Import into Unity 6.3 LTS, run compile, capture errors and fix them.
2. Run EditMode tests and make them pass.
3. Create the menu Brush Lab in an empty scene, use mouse to paint all three presets, export PNG and inspect output.
4. Profile 384×512, 10Hz wet simulation, 15Hz upload on desktop and at least one Android phone. Record CPU ms, GC alloc, FPS, and memory.
5. Only then improve strokes: start/end taper, anisotropic fibre grain, anti-aliased edges, efficient dirty-region diffusion, and proper pen/touch handling.
6. Keep Core independent of UnityEngine and BOKUMYAKU game logic; don't vendor external OSS automatically.
7. Deliver a concise bug and benchmarks report and screenshot evidence.

Acceptance: no compile errors, >0 deposited pigment, deterministic replay from equal inputs, positive pigment after diffusion, three visibly distinguishable presets, PNG output, no mandatory GPU compute.
