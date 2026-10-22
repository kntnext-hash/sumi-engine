# SUMI ENGINE — Procedural Japanese Ink / Unity UPM

独立した毛筆演算コア（C#）とUnity描画アダプタ。筆圧、速度、毛束、顔料、水分、紙の滲み、乾燥を扱う **Brush Lab v0.2** です。最初の利用先はゲーム『墨脈 — BOKUMYAKU』ですが、このPackageに敵Waveやゲーム内経済は含めません。

> **2026-10-22:** Unity 6000.3.26f1でコンパイル・EditMode試験・画面描画を確認。検証結果は [QA台帳](docs/QA_AND_EVIDENCE.md) を参照してください。モバイル実機、ゲーム本体、物理流体の再現は今回の完成範囲に含めません。

## すぐ描く

ローカルWindows版は `Builds/BrushLab/SumiBrushLab.exe`。同じフォルダーのDLLと`SumiBrushLab_Data`も必要です（生成物はGit対象外）。Unityでは `UnitySandbox/Assets/BrushLab/Scenes/BrushLab.unity` を開きPlayしてください。

- 紙をドラッグして描画。Dense＝濃墨、Dry＝乾筆、Split＝割れ筆、Bleed＝滲み、Pool＝墨だまり、Mist＝煙状淡墨、Splatter＝飛沫。
- Mouse taperはDense/Dry/Splitの始終端を入力経路内で細くします。切るとPressureの指定をそのまま使います。
- Colors +から墨色・紙色・RGB・表示濃度を変更。既存線も一括再着色します。筆を切り替えても色は維持します。
- PNGで保存し、Open exported PNG folderで保存先を開きます。
- Stroke study / Effects studyは現在の紙を置換する比較見本です。必要な絵は先にPNGで保存してください。

表示する滲みや墨だまりは手続き的な視覚表現です。点列v1のReplayは筆設定とSeedを再現しますが、描画間の拡散時間は記録しません。

- 新規Codexセッション: [START_CODEX.md](START_CODEX.md)
- 完全な引継ぎ: [docs/HANDOFF_CODEX.md](docs/HANDOFF_CODEX.md)
- Windows導入: [docs/INSTALL_WINDOWS.md](docs/INSTALL_WINDOWS.md)
- Tasks: [docs/TASKS.md](docs/TASKS.md)
- 運用規約: [AGENTS.md](AGENTS.md)
- 初版旧API説明: [Documentation~/CHANGELOG.md](Documentation~/CHANGELOG.md)

## 墨脈のゲーム試作

ゲームは `UnitySandbox/Assets/BokumyakuPrototype/` に分離しています。ローカルWindows版は `Builds/Bokumyaku-v0.8/Bokumyaku.exe`。一人用システムαとして5敵・6武器、7周回/4恒久強化、3構成保存、途中保存・再開、報酬と履歴、自動周回を実装。採墨による稼ぎと記録の投資差、旧版の途中周回を保つ保存移行を追加しました。60Wave上限で、長期経済・初回の尺・完成美術は調整中です。[操作と検証範囲](docs/BOKUMYAKU_PROTOTYPE.md)を参照してください。

Unity Package ManagerからGitURLで使用できるよう、`package.json` は**レポジトリルート**に設置しています。例：`https://github.com/kntnext-hash/sumi-engine.git`（GitHubへpush後）。

他リポジトリの素材/コードは同梱していません。公開ライセンスは未決定のため、独断でオープンソースライセンスを付与しないでください。

PowerShell 7で `pwsh -NoProfile -File scripts/Test-Core.ps1` を実行すると、Unityに依存しないCore検証を再実行できます（Unity NUnit試験とは別）。新しい環境では `scripts/Setup-UnitySandbox.ps1 -UnityEditor '<実在するUnity.exeの絶対パス>'` を実行してください。[Brush Lab手動QA](docs/BRUSH_LAB_MANUAL_QA.md)にセットアップと画面の確認手順があります。保存シーンはUnityが生成する方式です。
