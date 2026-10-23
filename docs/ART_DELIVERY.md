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

# 素材納品 — delivery-v3 / 横長ボタン筆枠

2026-10-23 JST。候補1点、新規生成1回、修正0回。既存素材の置換なし。

## 原本と由来

- [button_ink_frame_03.png](../art-source/delivery-v3/button_ink_frame_03.png): **2172×724、3:1、PNG RGBA、328,912 bytes**。目標1536×512とは寸法不一致、比率一致。原本はリサイズ/画素編集せず保持。
- SHA256: `E2B77E2D7688A8D255EA53A17FFD935DFB3090F454C373B134C8A1C6A44BE1A5`
- imagegenスキル、built-in image_gen.imagegen、transparent_background=true。モデル番号はツール非公開。[全文プロンプト](../art-source/delivery-v3/PROMPT.txt)。
- 生成元 `$CODEX_HOME/generated_images/01a16134-e305-7543-954d-eb9c2c8c17c7/exec-1db98a5d-f4a9-4c49-a106-85aa90985d78.png`。
- menu-home-1080x2400.pngとmenu-scroll-research.pngを目視。以前の参考ビジュアルの掠れ・筆端を方向として、新規の横長構図を生成。画像を生成ツールへ入力していない。写真/ロゴ/文字/ゲーム素材の切抜きなし、第三者素材の直接取込みなし。

## 形と検査

円相を引き伸ばす構造から、近水平の上下線と短い左右線、筆の入り/払いを持つ柔らかな四隅へ変更。文字・アイコン・和紙・塗り背景なし。主な掠れは長辺に沿い、筆端の変化は端へ集めた。黒/炭色の枠で、金色への着色結果は未検証。

[検査JSON](../art-source/delivery-v3/inspection-final.json)、[再実行スクリプト](../art-source/delivery-v3/Inspect-Asset.ps1)。全画素走査: alpha=0が1,392,042、半透明180,418、不透明68。中央1画素alpha=0。a>0の外接範囲x62..2109/y37..651で原本端に接触なし。

九分割の中央領域x300..1871/y240..483は383,568画素中164画素に微小なalphaが残るため、中央全域が完全透明とは扱わない。**uGUI Image.fillCenter=false**を推奨し、中央セルを描画しない。文字用の中央を透明として使う場合はこの設定が必要。

[九分割検査表示](../art-source/delivery-v3/nine-slice-preview.png): 生成原本を明るい背景に重ね、原比率と240×80、560×80、800×64を目視。四隅の形と辺の接続は維持、不要文字/紙背景は認めず。800×64では内側が狭くなる。これはSystem.Drawingの検査合成でUnity実表示ではない。初回プレビュー生成はPathInfoの型変換で失敗、文字だけの画像を不合格とし、文字列パスへ修正して再生成した。

## 統合用の初期値

[設定JSON](../art-source/delivery-v3/slice-settings.json)。原本ピクセルで **Border L=300 / B=240 / R=300 / T=240**、Image.Type=Sliced、Fill Center=false。import時に縮小する場合はBorderも同率に換算する。検査表示の固定隅幅36px/高さ29px、通常ボタンはこの隅尺度で高さ80px以上を推奨。実際のCanvas尺度・Pixels Per Unitに合わせて調整が必要。

枠は装飾用でありタップ判定/ラベル余白を定義しない。元画像に外余白があるため実線はRectTransform端から内側へ入る。背景色面を別に置く場合、元の不透明な矩形色面が残ると矩形感も残るため、枠だけの差替えではその問題は解消しない。ボタンの色面と文字位置は統合側で合わせる。

## 限界・受渡し

候補納品。Unity import/実9-slice/1080×2400のホーム・研究操作/実機は未実施。黒背景上の黒枠は識別できないため色の組合せは統合時に確認。最終採用・商用権利の保証はしていない。

書込みはart-source/delivery-v3/と本書への追記のみ。ゲームコード・STATE/TASKS・Runtime・Unity・Git操作なし。統合担当が枠とボタン色面を合わせ、ホームと研究で判読性を確認する。
統合追記: delivery-v3の枠をメニューボタンへ採用。ImportMenuFrame.csでSprite設定、fillCenter=false。1080×2400ホーム目視と主要操作合格。menu-home-1080x2400.png参照。

# 素材納品 — delivery-v4 / 縦札の墨縁＋控えめな金縁

2026-10-23 JST。upgrade-help.png/result-reward.pngの5枚カードを確認し、68px幅で輪郭を残す不規則な墨縁1種類を制作。細い鈍金の内縁を添え、文字・アイコン・和紙背景は含めない。既存素材は未変更。

- [納品PNG](../art-source/delivery-v4/card_ink_gold_04.png): 1024×1536、RGBA、1,425,075bytes。SHA256 `41130E991704066A47E30FDC805096AC246A882FD75F5DA4FB3628A8E476E0EF`。
- [由来・検査詳細](../art-source/delivery-v4/README.md)、[全文プロンプト](../art-source/delivery-v4/PROMPT.txt)、[実alpha検査](../art-source/delivery-v4/inspection-final.json)。imagegen専用ツールで新規1回、修正0回。参考は目視のみ、生成に画像入力なし。
- [360px幅プレビュー](../art-source/delivery-v4/preview-360.png): 原比率68×102、九分割68×96/68×120、同じ素材の5枚間隔表示。黒の輪郭と細い金縁を確認。小表示で微細な掠れは省略される。
- [9-slice案](../art-source/delivery-v4/slice-settings.json): 原本pxでL180/B220/R180/T220、Fill Center=false。白tintでRGBを保持。previewの隅12×15px。import縮小時はBorderも換算。
- 中心点alpha=0。ただし中央セル727,744画素のうち1,729画素に微小alphaが残るため中央セル非描画を推奨。左/下端に微小alphaが接し、厳密な外周透明余白4%は未達。主線に見える端切れは認めず。
- Unity未統合。既存アイコン/文字との重ね、無効状態、紙背景、実機判読性は統合担当の確認待ち。元画像を画素加工せず保存。今回書込みはdelivery-v4と本書のみ、Unity/ゲームコード/Resources/Git操作なし。

統合追記: ImportCardFrame.csでL180/B220/R180/T220、Sprite Sliced/fillCenter=false、白tint/PPU multiplier13として戦闘5カードへ採用。360×640で墨不足の無効状態、枠と名前/価格の非干渉を確認。evidence/card-frame.json/png。検証はEditor Play限定。
