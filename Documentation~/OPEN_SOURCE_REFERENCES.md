# OSS reference inventory (research only; no vendored code)

The current prototype is an original, simplified implementation, influenced by broad techniques used in painting systems. No source code, example brushes, art assets, or licensed dependencies from these projects are bundled.

| Project | URL | Observed license | What to learn from |
| --- | --- | --- | --- |
| libmypaint | https://github.com/mypaint/libmypaint | ISC | Brush-state semantics, pressure and speed response, stroke/dab decoupling |
| Hokusai | https://github.com/reearth/hokusai | MIT or Apache-2.0 | Portable brush core, tiled surface design, replay / compatibility testing |
| unity-moxi-ink | https://github.com/komietty/unity-moxi-ink | MIT | Flow/diffusion rendering in Unity; longer-term GPU backend |
| InkPainter | https://github.com/EsProgram/InkPainter | MIT | Unity painting surface integrations |

Licenses verified from public repository pages on 2026-10-18. For any future copying, distribution or vendoring, check the exact component's LICENSE, dependencies, brush assets and notice requirements again. GPL-only projects were not included.

NOTE: Core diffusion in this package is a simple finite-difference visual approximation; do not describe it as MoXi/LBM or equivalent to full watercolour research implementations.
