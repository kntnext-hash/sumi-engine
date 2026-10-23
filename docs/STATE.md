# 現状 — 墨脈
更新: 2026-10-23

## 現状
- Unity6000.3.26f1 / URP17.3.0 2D。Brush Lab表示/PNG互換確認済み。
- 戦場SpriteRenderer。戦闘とホーム/研究/記録はuGUI+TextMeshPro。ゲーム側IMGUI撤去。同梱日本語フォントと筆枠/紙札/数値部品を共有。
- シミュレーション・経済・保存ロジック・Runtimeは未変更。価格500案はstash保護。

## 直近の結果
- 検証区分: 今回は速度設定の軽量Editor Play検証。2倍保存枠→出撃→保存再読込、1倍出撃が合格（evidence/start-speed.json）。選択速度を手動出撃でも保持する修正。Player再ビルド/実機は未実施。
- 1080×2400、ホーム/研究/記録を目視。表示文字の同梱フォント・glyph・領域内収まりを検査。
- 武器/構成/方針、保存枠、消音、出撃、終了取消/確定、研究購入、履歴、保存再読込の操作確認成功。実ボタンへのraycastを確認。証拠: evidence/menu-migration-check.json、menu-*-1080x2400.png。
- 隔離セーブに試験結晶を付与して検証。Play停止、GameView復元済み。旧IMGUI用QAは使わずMenuMigrationCheck/ MenuInteractionCheckを使用。
- 関連EditMode（Bokumyaku）213/213合格。evidence/menu-editmode.json。
- ボスへ朱の筆輪を追加。人工5種80体/20描画でRenderer340固定、画像確認。軽量検証、自然Wave/実機性能ではない。
- 初期体験27周を読み取り専用測定。Farming初回367〜421秒、祖墨は全構成1周購入で目標未達。OPENING_BASELINE.md。係数未変更。
- 共通スクロールバー追加。軽量Playで1080×2400のScroll入力→研究最下段到達を確認。menu-scroll-check.json/png。実機ドラッグは未検証。

## 次の一手
0. 結果HUDを確定報酬の合計/通常/初達成内訳へ修正。未確定は保存待ち。同梱フォント1字追加。今回の軽量PlayでWave35の241+35=276が所持増分と一致、360×640のglyph/収まり/画像を確認（result-reward.json/png）。実保存障害の注入は未実施。次は戦闘強化の効果説明。
1. v7（62ca6e4）ビルドerror0/warning0。表示Playerの実540×960ホーム/戦闘を目視。6武器/自動周回/別プロセス復帰合格。WAVE見出し/強化レベルの余白を修正。360×640限定Playで非重複と画像を確認（hud-spacing.json/png）。次回ビルドへ反映。
2. 素材室の横長筆枠delivery-v3をメニューボタンへ統合。1080×2400の目視/主要操作を軽量再確認。音室はmono低下0.045dB/30分PCM接続を検査、聴感未検証。参考画像への美術改善と密集時判読性。同tick生成・撃破の分裂子演出欠落が残る。
3. 音の聴感、初週バランス、スマホ実機は未達。PRODUCT_COMPLETION.md。リポジトリ分割は計画のみ。
