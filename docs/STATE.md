# 現状 — 墨脈
更新: 2026-10-23

## 現状
- Unity6000.3.26f1 / URP17.3.0 2D。Brush Lab表示/PNG互換確認済み。
- 戦場SpriteRenderer、戦闘/ホーム/研究/記録uGUI+TMP。同梱日本語フォント共有、ゲームIMGUI撤去。
- 今回シミュレーション・経済・保存ロジック・Runtime未変更。価格500案はstash保護。

## 直近の結果
- 検証区分: UI修正統合のWindowsビルド/表示Player限定試験。全テスト・実機検証ではない。
- v8（7cabb5e）error0/warning0、72.96秒。6武器/自動周回/別プロセス復帰合格。2プロセスexit0（118.11秒/15.62秒）。540×960ホーム/戦闘画像を目視。証拠evidence/*ui-v8*、ui-build-v8.json。
- 手動出撃で選択速度を保持。結果は通常+初達成=確定合計、未確定は保存待ち。7強化の説明パネル追加。隔離Editor Playで速度/報酬一致/説明開閉/文字収まりを検証。start-speed、result-reward、upgrade-helpのJSON/PNG。
- メニュー1080×2400操作/文字確認と関連EditMode213/213の既存結果はmenu-migration-check、menu-editmode参照。今回の全テスト結果ではない。
- 初期体験27周: Farming初回367〜421秒、祖墨は全構成1周購入で目標未達。OPENING_BASELINE.md。
- 過去の詳細はarchive/STATE_ui_fixes_2026-10-23.md。

## 次の一手
1. 素材室delivery-v4の墨縁/金縁を5強化カードへ統合。中央非描画、360×640の文字収まり/無効状態/実画像を軽量Play確認（card-frame.json/png）。新枠のPlayerビルド/実機は未検証。横長筆枠v3はメニュー採用済み。
2. 墨核を細い金輪/金脈/白い中心へ変更。Wave1/360×640/演出軽減の限定Play画像確認（core-light.json/png）。点墨/貫筆の墨先端移動追加。注入描画試験で移動/演出軽減/戦闘状態不変を確認（attack-travel.json/png）。3分実戦Wave17→32/304撃破、平均16.81ms/最大123.86ms、全面更新0（render-polish-15a4f40-timing.json）。最大フレーム原因未特定。独立QA指摘のボス赤輪ヒットストップずれを修正、停止/再開位置一致を限定検証（boss-freeze.json）。同tick生成/撃破の分裂子演出欠落は残る。
3. 音8素材は統合済み。mono/PCMループ検査済み、全イベント発火/聴感は未達。初週バランス・スマホ実機も未達（PRODUCT_COMPLETION.md）。リポジトリ分割は計画のみ。
