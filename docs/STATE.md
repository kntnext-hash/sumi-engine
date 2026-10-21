# 現在の開発状態

更新: 2026-10-21 JST。

## 担当する部屋
- この初期セットアップのチャットは今後『墨脈 — BOKUMYAKU』のゲーム制作担当。Wave、敵、墨供給、強化、敗北・周回、ゲーム統合を扱う。
- 同じ「水墨」プロジェクト内の「SUMI ENGINE 水墨描画エンジン開発室」が描画専任。Core、Unity描画アダプタ、Brush Lab、描画QAを扱う。
- このセッションで着手済みの描画修正は保存して引き継ぐ。以降、ゲーム側から描画ファイルを並行修正しない。共通台帳は同時編集せず、開始時に最新状態を読む。

## 実測と成果物
- origin: https://github.com/kntnext-hash/sumi-engine.git 。空であることをGitHub接続とgit ls-remoteで確認し、元36ファイルを初期コミット `1a4419a` としてmainへpush済み。履歴の上書きなし。
- Unity Editor/Hub: PATH、C/Eの標準配置先、Hub設定、インストール登録で未検出。実在パッチ番号は未確定。UnitySandboxと保存シーンは未生成。
- .NET SDK: 未検出。PowerShell 7.6.5 / .NET 10.0.11のAdd-TypeでCoreを実コンパイルし、独立runnerで18検証PASS。Unity NUnit合格を意味しない。
- Core: ストローク間の筆先方向漏れを修正。負水分、非有限値、過大入力、負シミュレーション係数の検証と回帰テスト追加。NUnitは18ケースを用意、未実行。
- Brush Lab: 実筆圧slider、入力終了処理、保存2シーン生成メニュー、固定.metaを追加。Unity画面では未確認。
- セットアップ: scripts/Setup-UnitySandbox.ps1。実在6000.3系Editorからプロジェクト・BrushLab/BranchGrowthシーンを生成する。配布ソースだけLocalPackages~へ同期し、ホストの再帰取込みを防ぐ。正本はルート。
- scripts/Import-Starter.ps1: 既存ファイル差異・dirty状態・誤originを検出して停止するよう修正。
- 証拠: docs/evidence/core-checks.txt、docs/QA_AND_EVIDENCE.md。手動手順: docs/BRUSH_LAB_MANUAL_QA.md。

## 未達と次の一手
- P0-01監査は合格。P0-02〜05はUnity実行が必要なためBLOCKED、P0全体未合格。P1以降とモバイルは未合格。
- 描画室の次: Unity 6.3 LTS実行ファイルを検出/導入後、セットアップ→EditMode→実画面・PNG確認。導入・ライセンスはユーザー環境で必要。
- このゲーム部屋の次: docs/specs/BOKUMYAKU_GDD_v0.2_Unity.mdをもとに、描画に依存しないWave/枝供給/敵/強化/敗北・周回の最小仕様と数値検証を進める。完成した描画を使う実プレイ統合の合格は描画ゲート通過後。
- ゲーム本体のコードは未実装。The Tower公開資料の分析を実施（下段のゲーム実装準備参照）。コード/素材取込みは未実施。
- 続きの一文: ゲームはこの部屋、描画は専用室。git statusと最新台帳を読んで担当範囲から再開する。
- 引継ぎ実装SHA: Core `2cca288`、セットアップ/Brush Lab `d46ca04`。

## 描画室の引継ぎ確認（2026-10-21）
- b8bfd52のcleanな作業ツリーからCore/Unityアダプタ/Editor/Brush Lab/描画QAの責任を引継ぎ。ゲームファイルは対象外。
- 最新ソース・セットアップ・手動QAを読取確認。PATHとC標準配置でEditorなし。既存の純C#18検証は再実行していない。
- 次の未達はP0-02。Unity 6.3 LTS Editorの導入とライセンス有効化後、実在Unity.exeを指定してSetup-UnitySandbox.ps1を実行する。P0-02〜05はBLOCKEDのまま。

## 2026-10-21 Codex × Unity 操作環境（最新追記）
- Unity公式Plugin 0.1.8-betaとUnity CLI 1.0.0-beta.13を新規導入。MCPをUnitySandbox指定で登録、initialize/tools/list成功。ただしツール0件、Editor操作は未達。
- Editor 6000.3.26f1導入はELEVATION_REQUIREDで停止。導入済みEditorは0件。Hub未検出、ライセンスクライアント未接続。
- 専用Scene生成/回転/PlayMode試験のソースとインストーラをscriptsに準備。UnitySandboxは未生成。Core独立18検証再実行PASS、Unityコンパイル/画面/PNGは未実行。
- 手順: docs/CODEX_UNITY_SETUP.md。証拠: docs/evidence/unity-automation-setup.txt。P0-02〜05はBLOCKEDを維持。
- 続きの一文: 人がEditor導入と認証を終えたらSetup-UnitySandbox→操作probe→Pipeline→CLI/MCP実操作→EditMode/PlayMode→Brush Lab画面確認から再開する。

## ゲーム実装準備（2026-10-21）
- ユーザーがUnity導入中。WindowsAppsのunity.exeエイリアスは検出したが、6000.3系Editor実体/ProjectVersionは未確認。インストールは操作していない。
- [GAME_IMPLEMENTATION_READY.md](GAME_IMPLEMENTATION_READY.md)にG1〜G5の実装順、3敵/Wave1-20の初回範囲、描画との契約、保存/順位/課金の後続工程を準備。
- [The Tower分析](research/THE_TOWER_SYSTEMS.md): 公式情報から成長、課金、大会の循環を分析。収益や継続の因果は仮説。アプリ/バイナリ/通信解析はしていない。
- scripts/Test-GameBalance.ps1を実行、421数式検査PASS。docs/evidence/game-balance.jsonへWave/供給表を保存。戦闘シミュレーション・GDD Gate A・P3は未合格。
- 次のゲーム作業: UnitySandbox作成確認後にG1（Pure C# RunSimulator/出現/敗北）、続いてG2供給・戦闘。描画Coreは別室所有を維持。

## 追加資料確認（2026-10-21）
- ルートのTHE_TOWER_TO_BOKUMYAKU_REVERSE_ENGINEERING_HANDOFF.mdとBOKUMYAKU_WAVE_BASELINE_v0.2.csvを読取照合、原本維持。
- CSV Wave1-60、300数値・60ボス指定がGDD式に一致（小数6桁許容差）。docs/evidence/wave-baseline-review.json。
- 実験計画は10seed×3成長方針×4購入戦略=120条件へ更新。追加資料の90条件を包含。実走は未実施。
- docs/ASSUMPTIONS.mdへ未知仕様を集約。3敵/20Waveは中間増分、5敵/自然敗北までの本来ゲートは未合格。
- 次: 未確定な敵構成・成長候補等をbalance.jsonへ明示し、描画から独立したRunSimulatorの実装へ進む。
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

## 2026-10-21 The Tower追加調査
- 公開TheTowerSDKのREADME/LICENSE/NOTICEを確認。敵成長・強化費用等の参照先を発見。非公式・未実行であり、直接取り込みなし。
- docs/research/THE_TOWER_DATA_ADDENDUM.mdへ版情報、出所、具体例、公式と攻略ガイドの不一致、未確認事項を記録。
- 調査追記は完了。ゲームG1/G2・Unityゲートは未合格のまま。次は既存独自式でシミュレーションを実装し、購入間隔・実効DPS・死因を記録する。
