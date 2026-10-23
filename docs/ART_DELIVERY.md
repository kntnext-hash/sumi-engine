# 素材納品 — delivery-v2

2026-10-23 JST。素材室の限定1単位: 縦スマホ戦場用の余白を広げた背景候補1点。ゲーム組込み・採用は統合担当。

## 成果物

- [landscape_portrait_quiet_02.png](../art-source/delivery-v2/landscape_portrait_quiet_02.png): **1024×1536 / PNG RGB 8bit各色 / alphaなし / 2,179,920 bytes**。
- SHA256: `9FFDBAEA2C2E4202B38C9FE68C5456B211892CC2D902043C71FBB1261AE75151`
- [実測JSON](../art-source/delivery-v2/inspection.json)、[全文プロンプト](../art-source/delivery-v2/PROMPT.txt)、[360px幅の検査表示](../art-source/delivery-v2/preview-360.png)。previewは配布用背景ではない。

## 不足と制作判断

既存batch-001の山水原本と縦GameView証拠bokumyaku-portrait-v12.pngを目視。ART_INTEGRATIONによれば和紙・円相・3武器アイコンは導入済み。今回は既存の山が中央近くへ入る構図に対し、中央を広く空けた選択肢を制作した。和紙やUI装飾を重複生成せず、この背景1点で有限の納品単位とする。

生成りの和紙、淡灰の山、左下の小塔を残し、山は主に左右端、上下は霧へ抜く。敵・枝・攻撃・核・撃破は含めない。既存背景を置換せず別ファイルとして保存。

## 由来

- built-in `image_gen.imagegen`、2026-10-23 JST、個別新規生成1回、修正0回、transparent_background=false。ツールはモデル番号を公開していない。
- 出力元: `$CODEX_HOME/generated_images/01a16134-e305-7543-954d-eb9c2c8c17c7/exec-a3de09c0-9f0a-41fd-8120-2329ee599a4f.png`。生成原本をバイト変更せず納品先へコピー。
- 既存画像は方向と不足の目視判断だけに使用。画像ファイルを生成ツールへ渡していない。第三者の写真・ロゴ・文字・ゲーム画像の切抜き、コード/素材取込みなし。新規契約なし。
- imagegenスキルを適用。検査用縮小表示のみSystem.Drawingで作成し、原本の描き直しや画素編集はしていない。

## 検査結果と限界

- ファイルをデコードし実寸・RGB形式・bytes・SHA256を取得。opaque背景の指定に一致。1024×1536目標一致。
- 原寸生成表示と360×540縮小表示を目視。不要文字/ロゴ/枠、敵/枝/核/UIの混入なし。中央に広い淡い余白あり。紙繊維は小表示で控えめ、山の輪郭は端に残る。
- プロンプトの「中央70%幅を全高で完全に空ける」は厳密には未達。薄い山裾が内側へ入る箇所がある。70%の無地保証、安全領域マスクとして扱わない。
- 現行ゲーム上の合成・戦闘視認性、各画面比率のトリミング、Unity import/圧縮、実機/性能は未検証。360px幅表示は端末QAではない。
- 統合時は既存背景と比較し、黒墨・朱・金の識別、敵密集時、上下UIとの境界を確認。2:3以外の表示では余白を保つFitまたは適切な切抜きを検討する。最終採用・商品完成の判定は未実施。

## 作業境界

書込みはart-source/delivery-v2/と本書のみ。今回の個別分担指示を優先しSTATE/TASKS・共有台帳・ゲームコード・Runtime・Unity Editor・Git操作に触れていない。次は統合担当による背景比較と採否。音制作は今回の素材1単位の範囲外で、音源を生成したとは扱わない。

## 統合担当の採用確認（2026-10-23）
縦戦場用として採用。Resources/BokumyakuArt/へ原本をコピーし、TextureImporterでmipmap無効/readable無効/Clamp/最大2048に設定。横画面とホームは既存背景を維持。360×640の隔離Play、seed42を7000tickへ加速してWave20の実画面を確認。中央の余白が広がり黒墨/朱/金を識別。証拠docs/evidence/portrait-background-v2.png。複数画面比率・実機・性能は未検証。攻撃筆跡の位置整合は別途精査する。
