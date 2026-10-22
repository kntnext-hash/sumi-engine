# OSS参考と出所台帳

## 現時点
v0.1の初期ソースはこの会話で生成した独立実装であり、下記OSSのコード/アセットは同梱していない。**参考とコードの直接流用を分けること。** 現在リポジトリ全体の公開ライセンスは未決定。Licenseファイルを無断で作って再ライセンスしない。

|参考OSS|URL|参考範囲|取扱い|
|---|---|---|---|
|libmypaint|https://github.com/mypaint/libmypaint|筆圧・速度などのブラシ駆動|コード混入前にファイル単位の条件確認|
|Hokusai|https://github.com/reearth/hokusai|再利用可能なbrush設定・MyPaint互換実装|Rust/WASM; Unityへ直接移植とは限らない|
|unity-moxi-ink|https://github.com/komietty/unity-moxi-ink|LBM的墨拡散の研究|Unity互換と元論文/コードの権利確認|
|InkPainter|https://github.com/EsProgram/InkPainter|テクスチャ描画・ランタイム入出力|コード・サンプルの条件確認|
|VirtualBrush|https://github.com/Cyril-Meyer/VirtualBrush|仮想毛束の研究|GPL-3.0直接取り込み禁止、参考のみ|

新たに外部コード/フォント/素材を導入した場合、名前・バージョン・URL・ライセンス・著作権表示・使用ファイル一覧・改変内容をこの文書に追記し、必要なNOTICEを成果物へ含めること。ライセンス条件は原著作元から実際に確認する。

## 2026-10-21 初期セットアップ
- 権限のあるGitHub接続でnyanstarの `docs/development/TEAM_WORKFLOW.md` を参照。責務分担、独立QA、証拠と再開記録の運用のみ参考とし、文書本文・コード・素材は転載していない。
- 今回のCore修正、入力UI、セットアップスクリプト、回帰検証は本リポジトリ内で実装。外部コード・画像の直接取込みなし。
- The Towerを調査・分析する手法の利用許可をユーザーから受領。今回のP0では未着手。将来のゲーム調査は挙動・設計の分析結果を独立実装へつなぐ。対象版・観察条件・出所・利用範囲をここへ追記する。

## 2026-10-21 — 開発用Unity公式ツール
- Unity-Technologies/unity-agent-plugin 0.1.8-betaをユーザーのCodexへ導入。LICENSE.mdはUnity Companion License for Unity-dependent projects。開発手順のみ参照し、Pluginコードは本リポジトリへ同梱しない。
- Unity CLI 1.0.0-beta.13（winget 1.0.0.20013）はUnity Technologies公式MSIX、Proprietary。外部開発ツールとして導入、再配布しない。
- scripts/UnityAutomation~/とMCPスモークは今回の独立実装。外部画像/モデル/ゲームAssetの取込みなし。

## 2026-10-21 The Tower公開情報の分析
- docs/research/THE_TOWER_SYSTEMS.mdに公式App Store商品説明、開発元v29/v29.1パッチ、Premium Pass/購入FAQ等への出所リンクを記録。
- 参考範囲は成長・課金区分・大会設計。コード/アセット/数値テーブルの直接取込みなし。APKや通信の解析なし。
- ゲーム数式検証は既存BOKUMYAKU GDDの独自仮説式を使用し、The Towerの数値移植ではない。


## 2026-10-21 公開データ参照先の追加
TheTowerSDKのREADME/LICENSE/NOTICEを読取。MIT表記に加えて由来別の条件未確定部分を確認し、直接取込みは行わない。Towerstatsの大会/Workshopは非公式参考資料。URL・blob SHA・不一致はresearch/THE_TOWER_DATA_ADDENDUM.md。ゲームコード・表・素材・個人セーブの取り込みなし。

## Unity公式UPM依存（実導入）
- UnitySandboxにcom.unity.pipeline 0.9.0-exp.1とTest Framework1.6.0を導入。解決版はPackages/packages-lock.json。第三者実体はLibrary/PackageCache内（Git除外）。レジストリ依存として利用しパッケージソースを転載しない。
- docs/evidenceのCube/Brush Lab画像は本プロジェクトをUnityで実行して取得したもの。外部ゲーム画像を使用していない。

## 2026-10-22 色・濃墨・参照筆見本
- MIT確認: https://github.com/msurguy/watercolor-playground （LICENSE・THIRD_PARTY_NOTICES）、https://github.com/komietty/unity-moxi-ink （LICENSE.md）、https://github.com/EsProgram/InkPainter （README内ライセンス）、https://github.com/sighack/watercolor-simulation （LICENSEとwatercolor.pde）。
- 直接取り込みはゼロ。watercolor-playground READMEの「乾燥後停止」を参考に独自CPU実装。微小残留顔料をfixedへ移して質量を保持し、描画再開時に活性化。スペクトルデータ・フォント・画像・第三者コードは取り込まない。
- MoxiのGPU/LBM、sighackの多層不規則ポリゴンは将来候補。今回採用済みと扱わない。InkPainterはUnityアダプタ分離の比較参考。
- ユーザー添付の墨表現シートは見た目の目標のみ。素材としてリポジトリへ複製しない。InkStudyは独自の点列による15ストローク。

## 2026-10-22 表現アルゴリズムの実装
- InkPaper.Textures.cs / InkStudy.Effects.csは独自実装。先行調査の多尺度・多層描画という一般的発想を参考に、格子補間ノイズで輪郭・毛束・濃淡を生成。sighackの再帰ポリゴンソースやMoxi LBMは移植していない。
- 墨だまり縁は堆積プロファイルであり、流体の表面張力や実紙の毛細管現象を再現したという主張はしない。

## 2026-10-22 ゲーム参考ビジュアル
ユーザー提供4画像は色・構図・モチーフの参考のみ。画像やロゴの直接同梱なし。枝は既存SUMI、敵/UIは独自手続き表示。日本語は実行OSのYu Gothic/Meiryo等を参照し、フォントファイルの再配布なし。

## 2026-10-22 素材室の生成PNG・第一弾
- built-in image_gen.imagegenによる新規生成6点（和紙/山水/円相/点墨/貫筆/滲墨）。モデル番号非公開。提供画像は方向の目視参考、生成入力への添付や切抜きなし。外部ゲームのアセットは同梱しない。
- 原本: ArtSource~/Bokumyaku/batch-001/*.png。生成初稿は同revisions/。プロンプト・生成回数・SHA256・alpha検査: docs/art/PROMPTS.md、ASSET_MANIFEST.md、inspection-final.json。検査用縮小合成はalpha-small-preview.png。
- 使用ファイル: UnitySandbox/Assets/BokumyakuPrototype/Resources/BokumyakuArt/{paper_ivory_01,landscape_mist_01,enso_frame_01,weapon_point_01,weapon_pierce_01,weapon_bleed_01}.png。原本bytesを維持しUnity importのサイズ/透過/フィルタを設定。ゲーム表示の判断はBOKUMYAKU_ART_INTEGRATION.md。
- 新しい外部コード/OSS/フォントファイルの取り込みなし。生成物に独自のOSSライセンス・権利の独占性保証を付与していない。
