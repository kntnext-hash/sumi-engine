# QA・証拠管理

にゃんスターの製品レビュー運用を参考に、検証の強さを区別する。

|等級|意味|何を保証しないか|
|---|---|---|
|S|静的なソース・JSON・構文・依存関係チェック|Unity実動|
|A|Unity EditMode/PlayMode自動テスト（正確な実行環境明記）|人間の画面評価、モバイル性能|
|R|Unity Editor/PC実画面・マウス/タッチ操作観測（画像/動画と実行日時）|実端末での60fps|
|D|Android/iOS実機、機種/OS/ビルド/負荷を明記|他の全機種|
|H|ユーザーが見て気持ち良いかの主観評価を記録|客観的市場成功|

P0合格に必要: S + A + R。Unity EditorがなくSのみならP0全体はBLOCKED/未合格。`docs/STATE.md`とここに証拠場所を記録する。

毎回の記録例:
```
- SHA: <commit>
- Unity: <exact version, OS>
- Source import: PASS/FAIL/NOT RUN
- EditMode: X passed / Y failed / NOT RUN
- PlayMode: X passed / Y failed / NOT RUN
- Brush Lab screenshot: <path> / NOT CAPTURED
- Device: Android/iOS model + FPS + memory / NOT RUN
- Outstanding: <repro and next action>
```

WindowsのUnity実行例（Unityの具体パスを導入済みのものに置換）:
```powershell
& 'C:\Program Files\Unity\Hub\Editor\<version>\Editor\Unity.exe' -batchmode -nographics -quit -projectPath (Resolve-Path .\UnitySandbox) -logFile .\logs\unity-import.log
& 'C:\Program Files\Unity\Hub\Editor\<version>\Editor\Unity.exe' -batchmode -nographics -runTests -projectPath (Resolve-Path .\UnitySandbox) -testPlatform EditMode -testResults .\logs\editmode.xml -logFile .\logs\unity-tests.log
```
注: `-nographics` が適切でない描画テストでは外し、R用はGUI Unity Editorでテスト。ホストUnitySandboxが作成されていない状態でのコマンド成功を捏造しない。

## 2026-10-21 初期セットアップ実測
- 元スターターSHA: 1a4419a（mainへpush確認）。修正SHAはこの証拠更新を含むコミット履歴を参照。
- S: package.json/3 asmdef JSON解析PASS。Core C#をPowerShell 7.6.5 / .NET 10.0.11 Add-Typeで実コンパイルPASS。独立検証18 PASS / 0 FAIL。Unity API/asmdefコンパイルとは別。詳細: [core-checks.txt](evidence/core-checks.txt)。
- 移行スクリプト: PowerShell構文PASS。Gitモック＋実ファイルによる衝突時の既存SHA維持/dirty/自己参照拒否PASS。実Git統合試験は未実行。
- Unity version: 未検出（PATH、C/E標準パス、Hub設定、インストール登録）。.NET SDKも未検出。
- Source import / EditMode / PlayMode: NOT RUN。NUnitは18ケース準備。
- UnitySandbox / 2保存シーン: NOT GENERATED。生成コードと実行手順のみ用意。
- Brush Lab screenshot / PNG表示 / Console error数: NOT CAPTURED / NOT RUN / 未計測。
- Android/iOS / 性能 / 主観評価: NOT RUN。
- P0全体: BLOCKED。Unity導入後のA+Rが必要。[手動QA](BRUSH_LAB_MANUAL_QA.md)。
- Replayの保証範囲: 同じ入力・seed・同じStep条件の比較。InkSession v1はStepの時系列を保存しないため、自由描画中の拡散を含むセッション全体の完全再生は未対応。点列履歴の永続化UIとメモリ上限も未対応。
- 検証対象実装: Core `2cca288`、セットアップ `d46ca04`。LTS検査はalpha/betaを拒否するよう修正済み。

## 2026-10-21 描画室の受領確認
- 確認対象HEAD: b8bfd528af671022a5ede4e7e0021ca52b933fb6。開始時clean。最新台帳、Brush Lab、シーン生成、セットアップと手動QA手順を読取確認。
- 環境確認: Get-Command Unity,Unity.exeは検出なし。C:\Program Files\Unity\Hub\Editorは存在しない。新規テスト実行なし、既存18 PASSの再認定なし。
- Unity import/EditMode/実画面/PNG: NOT RUN。Editor導入・ライセンス有効化待ち。

## 2026-10-21 — 公式Plugin / CLI / MCP実測
- 証拠: [unity-automation-setup.txt](evidence/unity-automation-setup.txt)。baseline c17d8aa。
- S/ローカルCLI: Plugin installed/enabled 0.1.8-beta、CLI 1.0.0-beta.13。MCP initializeとtools/list成功、ツール0件。Editorに到達した証拠ではない。
- S/独立.NET: Core18 PASS / 0 FAILを再実行。probe PowerShell構文、3 asmdef JSON、Node構文PASS。未生成プロジェクトへのprobeインストールは拒否、UnitySandboxを作らないことを確認。
- BLOCKED: Editor install ELEVATION_REQUIRED、license LICENSING_CLIENT_UNAVAILABLE、Pipeline install COMMAND_FAILED（プロジェクト不在）。statusはSTATUS_NO_INSTANCES、SafeMode検出0。
- A: Unity compile/EditMode/PlayMode/Scene生成/Console/意図的エラー修正ループすべてNOT RUN。新規C#はソース準備のみ。
- R/D: Game View、Brush Lab入力、PNG、実機すべてNOT RUN。P0全体はBLOCKED。

## 2026-10-21 ゲーム準備の検証
- 実行: scripts/Test-GameBalance.ps1（PowerShell 7.6.5）。421数式検査PASS / 0 FAIL。証拠: docs/evidence/game-balance.json（GDDのSHA256、式、仮定、全判定と生成表）。
- 範囲: Wave1-20の有限性/非負/単調性、式の基準値、先端供給と圧の変化。供給Lv17の回復は同経路長での数式上の結果。
- NOT RUN: ゲームC#、戦闘/経済/再現性シミュレーション、Unity、実画面、120走、ランキング/課金効果。G-PREPのみ合格、GDD Gate A/P3は未合格。
- 独立文書QA: tick7200での20Wave終了、残敵/敗北優先、3成長方針と4購入戦略の区別を修正。
