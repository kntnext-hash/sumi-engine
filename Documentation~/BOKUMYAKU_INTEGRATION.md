# BOKUMYAKU integration design

Game combat is **not inside SUMI ENGINE**. Gameplay's network pressure, HP, wave and ink currency remain in an independent assembly.

- `InkNetwork.GrewBranch(curve, pressure)` -> map the curve to `SumiEngineCore.DrawBezier(...)` using `Vein` preset.
- `Combat.Slashed(enemy, power)` -> invoke `Slash` with low water, high pigment; keep collision computed by gameplay.
- `Combat.Impact(position)` -> invoke `Bloom`, then optionally splash small deterministic paths.
- `Result.OnDefeat` -> export PNG from `SumiCanvas.ExportPng()`.

**Do not** use the appearance of wet ink pixels as combat hit detection. Wet/dry ink is an aesthetic renderer; gameplay must remain deterministic and testable even with renderer disabled.

### Subsequent milestones

1. CPU benchmark 256 / 384 / 512 texture and compare desktop + Android + iPhone.
2. Replace full-frame diffusion with dirty tiles / GPU ping-pong. Maintain Core renderer contract.
3. Add a stable RenderCommand protocol so Web or native UI can reuse paths and presets.
4. Add configurable water anisotropy, coffee-ring outlines, richer anisotropic paper grain.
5. Create game-facing facade for pooling 1000+ permanently baked vein segments.
