# v0.2.0

- Unity 6000.3.26f1で動作するBrush LabとWindows Player。
- Dense/Dry/Split/Bleed/Pool/Mist/Splatter、12種類の点列見本。
- 墨/紙RGBと表示濃度、PNG出力フォルダー操作。筆変更時も色を保持。
- マウス経路内での自動払い、方向性のある掠れ、濃淡のある墨だまり。
- Legacy筆の圧力0でも描画していた不具合を修正（旧圧力0の記録は修正後の挙動になる）。
- 乾燥後の処理停止、再開、顔料保存とSeed再現性の検証。

# v0.1.0

- Initial independent Unity package prototype.
- 2D brush simulation with per-bristle deposition and fading pigment reservoirs.
- CPU paper model (pigment, moisture, absorption/drying) and texture exporter.
- Brush Lab GUI, menu installer and NUnit edit mode tests.
