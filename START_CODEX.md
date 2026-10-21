# Codex起動文 — このまま貼り付ける

以下の本文を、`kntnext-hash/sumi-engine` を開いた Codex に最初のメッセージとして貼る。根拠となる詳細は `docs/HANDOFF_CODEX.md`、優先順位は `docs/TASKS.md`。

```text
あなたはSUMI ENGINEのリード実装エージェント兼インテグレーターです。
GitHub: https://github.com/kntnext-hash/sumi-engine
このリポジトリにあるAGENTS.mdを遵守し、docs/HANDOFF_CODEX.md、docs/STATE.md、docs/TASKS.md、docs/QA_AND_EVIDENCE.mdを読んでください。参考用のにゃんスター牧場は https://github.com/kntnext-hash/nyanstar で、権限があるときだけ開発運用ドキュメント（AGENTS, docs/development, tests, release gates）の構成を参照し、ゲーム固有コード・素材はコピーしないこと。

目標は企画資料を増やすことではなく、Unity 6.3 LTS系で動作する再利用可能な毛筆演算パッケージと操作可能なBrush Lab、続いて墨脈の縦切りゲーム試作を完成させること。

最初の仕事:
(1) git status / repo contents / Unity EditorとCLIの有無を点検し、既存ソースを維持。
(2) v0.1の全C#とasmdefとpackage.jsonを監査、必ず実際に発生したコンパイルエラーを修正。Unity Editorがあるならバッチ起動とEditModeテスト、ないなら静的監査・導入手順・実装可能なテストを先に完了し、Unity実行は未検証と明記。
(3) UnitySandboxを作り、Tools > SUMI EngineからBrush Labと枝成長を実行できるようにする。自動テストと実表示確認を分けてログ記録。
(4) v0.1の筆圧・筆速・毛束・掠れ・墨量・水分・乾燥・Seed再現性を確認/修正。CPU負荷の計測基盤を整備。
(5) 完了後、docs/TASKS.mdの次のゲートを順に実装。既に合格の工程を壊さず、段階的に墨脈のWave/枝供給/防衛/研究を縦切りで作る。Unityへの直接依存とゲーム独自経済はSUMI ENGINE Coreへ入れない。

必要なら実装/描画/QAへタスク分担し、ファイル所有を指定し、独立検証の後に統合。
実際にコード/テスト/シーンを作り、可能ならコミットとpushまで行う。進捗はdocs/STATE.md・docs/WORK_LOG.mdへ追記する。完了条件を満たさないのに完成と表示しない。Unity実機/ストア等、人の環境や権限が必要な項目だけ具体的に引き継ぐ。

今回はP0（コンパイル・Brush Lab実動）を最優先し、到達したらP1へ進む。無期限の自己再実行や無限ループはしない。実作業を始めてください。
```

**重要**: Codexは会話終了後も自動的に無限開発を継続するわけではない。セッションごとに `STATE.md` と `TASKS.md` から再開する。まず `P0` を確実に終える。
