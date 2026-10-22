# 手続き筆描画 — 敵・核の初版
2026-10-23。縦画面試作v0.12からの描画工程。戦闘数値/保存/Runtime本体は変更なし。

## 実装
- 敵5種を丸い群れ、尖った突進、二重輪の重装、双核の分裂、8棘のボスとして点列生成。核は開いた円相。
- Drawing/InkTextureBakerは点列・筆設定・SeedからSUMIの顔料を透明マスクへ焼く。ゲームの敵/HP/Waveを参照しない。将来ノベル向けにも切り出せる仮配置。
- ProceduralInkMotifsはゲーム形状を担当。96×96の白RGB/透明alpha、表示時に墨色を付ける。初期化で6枚生成し再利用、終了時破棄。毎フレーム生成しない。
- 枝は既存SUMI描画。敵の赤い中心とHP線は判読用。攻撃と撃破飛散の筆描画は未実装。

## 限定確認
Unity6000.3.26f1コンパイル、360×640の実表示、フレームとtick進行、6画像すべてに墨と透明領域があることを確認。Console error0/warning0。全テスト/ビルド/実機性能の検証はしない。
隔離保存先をPlay開始前に設定。seed42のFarmingを7000tickまで加速してから実Updateを進め、Wave21の画面を確認。通常プレイによる到達実績ではない。

証拠: evidence/bokumyaku-ink-motifs.png（左から通常/突進/重装/分裂/ボス/核）、bokumyaku-ink-battle.png、bokumyaku-ink-check.json。

確認手順: Bokumyaku.unityを縦GameViewでPlay→出撃→敵の形と赤い中心、金色の核を確認。停止/再開して表示と敵移動を確認。必要時のみscripts/UnityAutomation~/BokumyakuInkCheck.csのArm→Run→Capture→停止→Cleanupで隔離確認する。

次: 攻撃の払い・命中/撃破の墨飛沫を同ヘルパーで生成。密集時の区別と方向表現、詳細UI・実機タップは残件。現状の敵シルエットは機能見本で完成美術ではない。
