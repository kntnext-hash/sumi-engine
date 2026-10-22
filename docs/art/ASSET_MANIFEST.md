# 素材候補台帳 — Batch 001

制作日: 2026-10-22 JST。基準HEAD: c6f0f1735ec7c6481fc73342d28e5a2906f24940 / ゲームv0.3。

6点の候補納品。最終採用・Unity統合は未実施。生成: built-in image_gen.imagegen（モデル番号非公開）、個別6回＋滲墨修正1回。専用生成ツール以外で原本の画素編集なし。

全プロンプトは [PROMPTS.md](PROMPTS.md)。機械検査は [inspection-final.json](inspection-final.json)、再実行は Inspect-Assets.ps1。alpha-small-preview.pngは検査専用の合成縮小表示であり納品素材ではない。

## 原本・実測

ファイル基点: ../../ArtSource~/Bokumyaku/batch-001/。PNG、RGB/ARGBはデコード後の色形式。alpha境界はa>0、座標は両端を含む。

|ID|ファイル|実寸|色形式|bytes|完全透明/半透明/不透明画素|a>0境界 x0,y0,x1,y1|
|---|---|---|---|---:|---|---|
|ART-001|[paper_ivory_01.png](../../ArtSource~/Bokumyaku/batch-001/paper_ivory_01.png)|1254×1254|Format24bppRgb|2533055|0/0/1572516|0,0,1253,1253|
|ART-002|[landscape_mist_01.png](../../ArtSource~/Bokumyaku/batch-001/landscape_mist_01.png)|1024×1536|Format24bppRgb|2201149|0/0/1572864|0,0,1023,1535|
|ART-003|[enso_frame_01.png](../../ArtSource~/Bokumyaku/batch-001/enso_frame_01.png)|1254×1254|Format32bppArgb|714927|1140913/431243/360|49,59,1227,1218|
|ART-004|[weapon_point_01.png](../../ArtSource~/Bokumyaku/batch-001/weapon_point_01.png)|1254×1254|Format32bppArgb|348358|1393406/178401/709|138,160,885,1155|
|ART-005|[weapon_pierce_01.png](../../ArtSource~/Bokumyaku/batch-001/weapon_pierce_01.png)|1254×1254|Format32bppArgb|284462|1421991/150282/243|43,134,1179,1181|
|ART-006|[weapon_bleed_01.png](../../ArtSource~/Bokumyaku/batch-001/weapon_bleed_01.png)|1254×1254|Format32bppArgb|798203|1212340/359648/528|81,93,1233,1181|

## SHA256

- ART-001: 6C6DFE268A279182EB31F38F288E93F73037CA8398EE54FCBEADE82A5E25B385
- ART-002: DD80439A2DBA638D69942A5DC6B2AE47196AEAA508189C49FD8644A99B459661
- ART-003: 8B79B56FD00B28EAE6572D34B65BF3B636F8E5C4B7CB6D4DA8A62B09EC8651D6
- ART-004: 22FC794BD1B231F35C01D532696BE6F393B22BB92FCACC7F1440DFAA4FF717A0
- ART-005: 69A40BB68BB8EE8DE6A4CFAD3A97059D2E8D9112F37355AFC9FE3EC730A9C144
- ART-006: 1B7F803CD74DCAE4643E18B75AB2CAA1DA6EA60BDE62F15422CBA2212AC51052

## 用途・目視と採否

- ART-001 和紙: 低コントラスト生成り、繊維あり、不要文字なし。背景候補採用。1024角目標に対し1254角のため統合側で寸法調整が必要。タイル保証なし。
- ART-002 山水: 1024×1536の目標一致。山と小塔を端へ配置、中央は淡い霧、文字/敵/実枝/UIなし。背景候補採用。中央60%に薄い山裾は入り、完全無地ではない。戦闘での可読性は未検証。
- ART-003 円相: 中央alpha=0、外周透明、掠れと筆圧差あり、不要文字なし。候補採用。余白は狭め。
- ART-004 点墨: 滴と短い着弾筆。32/64px目視で他2武器と識別可能。32pxでは着弾細部が弱まる。候補採用・余白条件付き。
- ART-005 貫筆: 斜めの一本払いと鋭い先端。32/64px目視で識別可能。候補採用・余白条件付き。
- ART-006 滲墨: 丸い墨だまりと不均一な縁。修正1回で見た目の余白改善。32/64pxでも丸い形は残るが、細かな滲みは小表示で省略される。修正版を候補採用・余白条件付き。初稿はrevisions/weapon_bleed_initial.pngに隔離。

透過4点はすべて1254×1254（目標1024角と不一致）。実alpha確認済みだが、ごく薄い残留alphaにより厳密な完全透明余白10%以上は未達。3武器の最小完全透明余白は点墨98px/7.81%、貫筆43px/3.43%、滲墨20px/1.59%。画像の主形状に見える端切れはないが、微小alphaまで含む境界と目視の余白を混同しない。不要な文字・紙背景の混入は目視で認めず。

## 参照・出所

依頼書記載の提供4枚（1-写真1.jpg〜4-写真4.jpg）をview_imageで目視。墨の掠れ、生成り、淡灰の山水、余白のみの方向確認。生成ツールへ参照ファイルを渡しておらず、原画の切抜き・ロゴ・文字・画面・ゲーム素材をコピーしていない。修正入力は今回生成した滲墨初稿だけ。提供画像は納品物へ同梱していない。新規OSS/フォント/第三者素材取込みなし。生成物の権利や独占性・商用利用の法的保証は未確認であり保証しない。

## 未達・受渡し

- 候補セットの確認・実寸/alpha/出所記録は完了。全美術仕様合格ではない。
- 1024角化と厳密な透明余白の処理を統合前に検討。次回修正残枠: 滲墨1回、他素材2回。
- Unity import/圧縮/フィルタ/戦闘可読性/端末表示/性能はNOT RUN。ゲーム室で評価する。
- 共通台帳、OSS台帳、コード、Scene、ProjectSettingsは未編集。Git stage/commit/push未実施。統合担当が候補と記録をレビューして反映する。

## 統合担当の追記（2026-10-22）
上記は納品時の記録。6点はv0.4へ取込済み。原本の寸法/厳密な透明余白の未達は保持し、Unity import max sizeとUIの余白で扱った。統合判断とゲーム画面証拠は../BOKUMYAKU_ART_INTEGRATION.mdと../QA_AND_EVIDENCE.mdを参照。原本画素は未改変。
