# SUMI ENGINE — Procedural Japanese Ink / Unity UPM

独立した毛筆演算コア（C#）とUnity描画アダプタ。筆圧、速度、仮想毛束、顔料、水分、紙の滲み、乾燥を扱う**開発中の試作コード**です。最初の利用先はゲーム『墨脈 — BOKUMYAKU』ですが、このPackageに敵Waveやゲーム内経済は含めません。

> **status 2026-10-21:** GitHub初期登録済み。純C#の実コンパイル・回帰検証を実施。Unity Editorが未検出のため、Unityコンパイル/PlayMode/実画面/実機は未検証。P0全体は未合格です。

- 新規Codexセッション: [START_CODEX.md](START_CODEX.md)
- 完全な引継ぎ: [docs/HANDOFF_CODEX.md](docs/HANDOFF_CODEX.md)
- Windows導入: [docs/INSTALL_WINDOWS.md](docs/INSTALL_WINDOWS.md)
- Tasks: [docs/TASKS.md](docs/TASKS.md)
- 運用規約: [AGENTS.md](AGENTS.md)
- 初版旧API説明: [Documentation~/CHANGELOG.md](Documentation~/CHANGELOG.md)

Unity Package ManagerからGitURLで使用できるよう、`package.json` は**レポジトリルート**に設置しています。例：`https://github.com/kntnext-hash/sumi-engine.git`（GitHubへpush後）。

他リポジトリの素材/コードは同梱していません。公開ライセンスは未決定のため、独断でオープンソースライセンスを付与しないでください。

PowerShell 7で `pwsh -NoProfile -File scripts/Test-Core.ps1` を実行すると、Unityに依存しないCore検証を再実行できます（Unity NUnit試験とは別）。Unity導入後は `scripts/Setup-UnitySandbox.ps1 -UnityEditor '<実在するUnity.exeの絶対パス>'` を実行してください。[Brush Lab手動QA](docs/BRUSH_LAB_MANUAL_QA.md)にセットアップと実画面の受入手順があります。保存シーンはUnityが生成する方式で、現時点では未生成です。
