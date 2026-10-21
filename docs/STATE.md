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
