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
