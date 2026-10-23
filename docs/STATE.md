# 現状 — 墨脈
更新: 2026-10-23

## 現状
- Unity6000.3.26f1 / URP17.3.0 2D。Brush Lab表示/PNG互換確認済み。
- 戦場SpriteRenderer、戦闘uGUI/TMP、メニューIMGUI。縦戦場幅100%/高さ62%、下部HUD30%。同梱日本語フォント。
- 補間、敵5種/6武器演出、SUMI枝/撃破跡の部分更新。根の濃墨層/分岐、ダメージ表示追加。美術は参考画像に未達。
- Runtime/シミュレーション/経済/保存未変更。価格500案はstash保護。

## 直近の結果
- 検証区分: Windows描画移行ビルドとPlayer限定試験（フル検証は未完）。render-build.json / render-player-v2.json。
- WindowsビルドSucceeded、135720813bytes、error/warning0。Builds/Bokumyaku-render-probe-v2。配布公開なし。
- 旧Player検証が旧SumiCanvasを参照し不合格。GPU墨マスクの明示的QA読み戻しへ修正。
- 修正版Player: 6武器900tick/攻撃/保存再読込/墨マスク、自動再出撃は合格。全体passed=false: 素材期待数6に対し実際7（縦背景追加）。期待値7へ修正済み、修正後ビルド/再試験は次。
- Playerは自動終了。別プロセス復帰と配布版の目視は未実施。非表示Playerのframe数は性能証拠に使わない。
- UnityがビルドでURP除外情報/Runtime設定/既定Volume/Standalone batchingを生成。変更を保持。
- 360×640で操作列高44.7px/札68.7×104.2px、主要10ボタンraycast到達。hud-size-check.json/png。
- 過去の描画移行EditMode213/213、前後3分計測は後続美術変更前。実機なし。

## 次の一手
1. BuildRenderCandidate.Run（出力v3）で素材7個判定版をビルド、隔離Player再検証と別プロセス復帰。結果を合格に書き換えない。
2. 完成候補の画面/独立QA/全テスト、配布物の製品名・不要同梱物を確認。美術とメニュー移行を継続。
3. 音の聴感、初週バランス、スマホ実機は未達。PRODUCT_COMPLETION.md。リポジトリ分割は計画のみ。
