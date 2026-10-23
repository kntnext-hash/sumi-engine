# delivery-v4 — 縦札の墨縁・金縁

2026-10-23 JST。新規生成1回、修正0回、1種類のみ。card_ink_gold_04.pngが納品原本。initial-preview.pngは初回目視用、preview-360.pngは最終九分割確認用で素材原本ではない。

生成: imagegenスキル / built-in image_gen.imagegen、transparent_background=true。モデル番号非公開。PROMPT.txtが実プロンプト。生成元exec-918bd9bc-9ad2-48dd-9833-45b10365fcbf.pngをバイト変更せずコピー。
参照: docs/evidence/upgrade-help.pngとresult-reward.pngを目視。参考ビジュアルの筆縁方向を踏まえた新規構図。生成への画像入力なし、第三者画像・ロゴ・文字の切抜きなし。

1024×1536 / RGBA PNG / 1,425,075 bytes。
SHA256: 41130E991704066A47E30FDC805096AC246A882FD75F5DA4FB3628A8E476E0EF

全画素alpha走査はinspection-final.json、再実行Inspect-Asset.ps1。alpha=0:1,212,560、半透明360,304、不透明0。中心点は完全透明。中央セルx180..843/y220..1315の727,744画素中1,729画素に微小alphaが残る。fillCenter=falseで中央セルを描画しない設定を推奨。a>0境界はx0..1006/y9..1535で微小alphaが左/下の端に接する。厳密な透明外余白4%は未達。主線の見える端切れは目視で認めず。

黒い不規則墨縁＋細い鈍金の内縁。文字/アイコン/和紙背景なし。中央はゲーム側の手続きアイコン・名称・値を別描画する。原本の加工なし。System.Drawingによる検査用表示のみ作成。

九分割案: L180/B220/R180/T220（原本px）、Sliced、Fill Center=false、RGB保持のためImage tint白。縮小importする場合Borderを同率に換算。検査では隅12×15px固定、68×96/68×120を確認。360px幅に同じ1素材を5回反復表示し、墨縁の見分けと連続性を目視。5種類の案を作ったものではない。金は小表示で細く、細かな掠れは失われるが黒の主輪郭は残る。

Unity import/アイコンとの重ね/実画面操作/実機は未検証。紙色と無効状態の色、ラベル余白を統合時に確認する。候補納品、最終採用や権利の法的保証はしない。担当外ファイル/Unity/Resources/Git操作なし。
