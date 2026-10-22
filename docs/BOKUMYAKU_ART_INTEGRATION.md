# 第一弾素材のゲーム統合 — v0.4

2026-10-22 JST。素材室の6点を統合担当が採用し、UnitySandboxのBokumyakuPrototype/Resources/BokumyakuArtへ取込み。原本はArtSource~/Bokumyaku/batch-001/、生成経緯/プロンプト/実寸/alphaはdocs/art。原本の画素とbytesは変更していない。

|素材|Unity実寸|形式|使用箇所|
|---|---|---|---|
|和紙|512×512|RGB24|画面背景|
|山水|683×1024|RGB24|戦場の背景|
|円相|512×512|RGBA32|出撃前/結果パネルの淡い装飾|
|点墨/貫筆/滲墨|各256×256|RGBA32|名前付き武器選択ボタン|

6点ともsRGB、Clamp、Bilinear、MipMapなし、Read/Writeなし、uncompressed。Unity Editor APIでimport設定を固定。原本の1254角と厳密な透明余白10%未達は原本仕様として残し、import max sizeとUI側の余白、ScaleToFitで扱う。原本を1024角・余白10%達成済みとはしない。モバイル圧縮/Atlasは未調整。

枝は既存Coreの顔料量からalpha=1-exp(-density*pigment)を算出し、透明Textureを上に重ねる。Engine側は変更しない。枝が増えた時だけ再生成し、毎OnGUIで画素走査しない。元のSumiCanvasの紙面Textureも維持するため、総メモリ最適化完了とはしない。

検証: bokumyaku-art-import-v4.jsonに6点の原本一致/実寸/形式。Editorで取得した6Textureの割当合計5,773,568bytesは部分的な計測であり、ゲーム総メモリ/FPS/モバイル性能ではない。GameViewで6点のロード、透明194,767画素/墨1,841画素、Coreとalpha不一致0、停止中に枝Texture再生成なしを確認。

横長の戦闘画面、440×1060の武器選択/下端スクロール画像を目視。敵の朱、枝の黒、供給点の金が淡い山水上で識別できる。武器アイコンは文字も併記。序盤・少数敵の評価であり、最大敵数/全成長段階/モバイル画面の美術・可読性を保証しない。

再取込: scripts/UnityAutomation~/BokumyakuArtImport.csを非Play・未保存変更なしでEditor APIから実行。既存コピーが原本と異なる場合は上書きせず停止する。
