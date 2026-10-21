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

## 2026-10-21 Wave baseline照合
- 対象: BOKUMYAKU_WAVE_BASELINE_v0.2.csv、Wave1-60/60行。原本SHA256はevidence/wave-baseline-review.json。
- S/独立数式計算: HP/出現率/9秒期待数/基本HP流入/速度の300値、ボス指定60件、Wave連番/仮説ラベルが一致。許容差5.01e-7、最大絶対誤差4.966580036125379e-7。
- 原本CSV・引継ぎMDは変更なし。式への一致であり、The Tower実測や墨脈バランス/120走/Unity試験ではない。
## 2026-10-21 描画完成依頼の環境再確認
- 開始HEAD: 38e6a6c、作業ツリーclean。最新の指定文書、ソース/テスト一覧、CODEX_UNITY_SETUPを確認。
- 実ユーザー側の公式CLIでstatus（UnitySandbox絶対パス指定）: STATUS_NO_INSTANCES。pipeline list: totalInstances/runningInstances/reachableServersすべて0。editors --installed: data=[]。
- sandbox内LOCALAPPDATAは別配置のためCLIを解決できず、実ユーザーのWindowsApps/unity.exeを指定して上記を取得。CLI不在とEditor不在を区別した。
- P0-02〜05はBLOCKED継続。Unity compile/EditMode/PlayMode/画面/PNGはNOT RUN。純C#の既存合格テストは再実行していない。
- 次: 人がUnity 6.3 LTS Editor導入とログイン/ライセンス有効化を完了後、実在EditorパスでSetup-UnitySandboxを実行し、コンパイル→EditMode→Brush Lab入力/PNG/Consoleを確認する。管理者操作・認証で停止する規約に従い導入の再試行なし。

## 2026-10-21 Editorインストーラ失敗の診断
- ユーザー実行の導入はINSTALL_FAILED。実ログでインストーラ起動後3.4秒、終了コード2を確認。原因は未確定。
- MSIX LocalCache内の保存済みEXEは4,206,721,368 bytes、Authenticode Valid / Unity Technologies SF。C空き約672GB。Editor登録は引き続き0件。
- Git対象外logs/installers/UnitySetup64-6000.3.26f1.exeへコピー済み。再ダウンロードせず、ユーザーが対話式起動して具体的エラーを確認する。管理者操作は未代行。
- 再開: 対話式インストール結果を確認し、成功ならEditor登録/ライセンス/UnitySandbox/Pipeline/既存QAへ進む。P0はBLOCKED維持。
