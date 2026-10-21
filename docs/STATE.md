# 現在の開発状態

更新: 2026-10-21 JST。新たなCodexセッションはここを最新の実測値へ更新すること。

- GitHub: `kntnext-hash/sumi-engine`。この引き継ぎ資料作成時、GitHubレポジトリは存在するが **空**（`main`, public）。このZIPを初期コミットするまではGitHub上に開発コードはない。
- ソース: 会話で生成した v0.1 Unity Package のC#実装がスターターZIPに同梱されている（レポジトリルートに再配置）。
- コード検証: ZIPの内容・JSONは確認可能だが、Unity Editorでのコンパイル・描画・端末試験は **未検証**。
- 最初の工程: P0-01 / P0-02。`package.json`, asmdef, C#のUnity importから確認。
- 実装済みとされるもの: 毛束筆/紙面拡散/BrushLab/枝描画デモ/一部NUnitテスト（実動は未認定）。
- **未実装**: 実際の墨脈ゲーム（Wave/敵/戦闘/周回）、本格GPU墨演算、Android/iOSビルド。
- 参照したにゃんスター: private `kntnext-hash/nyanstar` の `AGENTS.md`、開発引継ぎプロンプト、WORK_LOG、発売ゲートを運用設計に反映。コード・素材は取り込んでいない。
- 下一手: Unityを検出 → 初期コンパイル失敗を特定・修正 → UnitySandboxのBrush Lab動作とEditModeテストを確定 → 証拠を採取する。
- 目標: ユーザー対応なく可能な作業はCodexが続ける。Unityアカウント/実端末など必要なことだけ、人に要求する。
