# 戦闘描画刷新 2026-10-23

## 実装
Unity 6000.3.26f1 / URP 17.3.0 の2D RendererをGraphicsSettingsと全Qualityへ設定。戦場はSpriteRenderer、戦闘表示はuGUI/TMPへ移行。既存のホーム・研究・記録はIMGUI。uGUIを選んだ理由は、スプライトの筆枠と再利用するボタン・数値・パネルを同じCanvasに組みやすいため。

戦場はsafe areaの幅100%・高さ68%。従来は30Hzの現在位置を直接描いていた。現在は前/current tickの位置を描画側で補間する。敵5種の墨シルエット、被弾の滲み/白、消失、6武器の軌跡・範囲、45msの表示ヒットストップ（連打制限あり）を追加。演出軽減は揺れとフラッシュを停止する。シミュレーションの時間を止めない。

SUMIで枝・撃破跡を描き、32pxタイル単位でCPU変換とGPU CopyTextureを行う。通常描画で全面Applyを行わない。初期化と新周回のClearは全面初期化。Runtime本体・シミュレーション・保存は未変更。

## 検証
- Brush Lab前後の表示を確認。出力PNGのSHA256は両方 `uL2m0kMyQHHoRhb84P0xY8EfG6nYvEssSL+8T+kMWZ4=`。evidence/brushlab-urp-{before,after}*。
- シミュレーション等211件 + dirty tileのGPU読戻し2件、213/213合格。evidence/render-editmode-tests.json。
- uGUIの速度・停止・自動・演出軽減・保存・ホームのraycast操作とviewport/EventSystem、8/8合格。evidence/render-ugui-controls.json。
- 前後画像はseed42、点墨/Farming、6000tickのWave17。前後の3分計測はevidence/render-{before,after}-timing.json。Editor/360×640/60fps目標であり、スマホ性能の保証ではない。
- 確認手順: Bokumyakuシーンで開始し、速度/停止/保存を操作。撃破跡の蓄積と枝追加、演出軽減を切り替える。Brush Labでは筆描画後PNG出力を確認。

## 参考画像へ近づける次工程
2026-10-23のユーザー添付画像を美術目標とする。現時点では参考の品質に未達。太くうねる広い樹形、赤い敵核と金の味方核、厚みのある紙札・筆枠、敵の判別しやすい輪郭を順に改善する。敵数や資源値を見た目のために改変しない。参考画像自体は素材として同梱しない。

## 残る制約
初回シェーダー準備の待機、実機のCopyTexture対応・日本語フォントは未確認。撃破跡は周回内の表示で保存データに追加しない。同tickで発生・消失した敵は既存シミュレーションにイベントがなく検出できない場合がある。メニューの移行、完成美術、スマホ実機QAは次工程。研究価格変更はstashに退避して保持。

### 最終計測結果
前: 平均16.8316ms / 最大112.8685ms。後: 平均16.7216ms / 最大53.0458ms。後は304撃破/304跡、全面再構築0、サンプル内全面初期化0。初回準備とEditor操作が混在した最大393msの試行は比較不採用。最大53msは残るため完全な無停止とは扱わない。
