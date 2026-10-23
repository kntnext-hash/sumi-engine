# 現状 — 墨脈
更新: 2026-10-23

## 現状
- Unity6000.3.26f1 / URP17.3.0 2D。Brush Lab表示/PNG互換確認済み。
- 戦場SpriteRenderer、戦闘uGUI/TMP、メニューIMGUI。縦戦場幅100%/高さ62%、下部HUD30%。同梱日本語フォント。
- 補間、敵5種/6武器演出、SUMI枝/撃破跡の部分更新。根の濃墨層/分岐、ダメージ表示追加。美術は参考画像に未達。
- Runtime/シミュレーション/経済/保存未変更。価格500案はstash保護。

## 直近の結果
- 検証区分: Windows描画移行ビルドとPlayer限定試験（フル検証は未完）。render-build.json / render-player-v3.json / render-player-resume-v3.json。
- WindowsビルドSucceeded、135720813bytes、error0/warning0。Builds/Bokumyaku-render-clean-v4。配布公開なし。
- 旧Player検証が旧SumiCanvasを参照し不合格。GPU墨マスクの明示的QA読み戻しへ修正。
- v3 Player: 6武器900tick/攻撃/保存再読込/墨マスク/素材7個/自動再出撃、全体passed=true。別プロセス復帰後600tickの状態一致も合格。
- Playerは自動終了。配布版の目視は未実施。非表示Playerのframe数は性能証拠に使わない。
- UnityがビルドでURP除外情報/Runtime設定/既定Volume/Standalone batchingを生成。変更を保持。
- 360×640で操作列高44.7px/札68.7×104.2px、主要10ボタンraycast到達。hud-size-check.json/png。
- 全EditMode255/255合格（今回の時計修正前）。独立read-only QAで停止中の輪郭揺らぎを検出・修正、Material時計25.04041→同値を限定Play確認。v4ビルドは修正前。実機なし。

## 次の一手
1. v4クリーンビルドで警告解消、別プロセス復帰600tick一致。173ファイル中171はv3同一、boot.config/resources.assets差分。画面目視とリソース確認を次に実施。
2. 同tick生成→撃破の分裂子の演出欠落が独立QA残件。シミュレーション変更禁止の範囲内で対応を検討。時計修正の次回ビルド、配布画面、美術/メニュー移行を継続。
3. 音の聴感、初週バランス、スマホ実機は未達。PRODUCT_COMPLETION.md。リポジトリ分割は計画のみ。
