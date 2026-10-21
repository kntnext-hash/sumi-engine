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

## 2026-10-21 The Tower追加調査
- 公開TheTowerSDKのREADME/LICENSE/NOTICEを確認。敵成長・強化費用等の参照先を発見。非公式・未実行であり、直接取り込みなし。
- docs/research/THE_TOWER_DATA_ADDENDUM.mdへ版情報、出所、具体例、公式と攻略ガイドの不一致、未確認事項を記録。
- 調査追記は完了。ゲームG1/G2・Unityゲートは未合格のまま。次は既存独自式でシミュレーションを実装し、購入間隔・実効DPS・死因を記録する。
## 2026-10-21 描画実装の事前設計
- ユーザー依頼によりRENDERING_IMPLEMENTATION_READY.mdを作成。0b48158のCore/Canvas/Brush Lab/テストを読み、実装差分・R0〜R7・操作仕様・固定比較入力・v2再生案・履歴上限・計測条件を具体化。
- 既存APIを維持し、まずP0実動、その後Seed比較/質感/乾燥停止/完全再生/性能の順。ゲームファイルと実装コードは変更なし。
- 検証: 計画と現ソースの照合、git diff --check。Unityと既存純C#試験は未実行。P0〜P2の合格状態は変更なし。
- 次: Unity利用可能後に開始票R0〜R2を実施。計画完成をエンジン完成と扱わない。

## Unity実動証拠（2026-10-21〜22 JST）
- Unity: 6000.3.26f1 (de8167e7440c)、Windows。Test Framework1.6.0、Pipeline0.9.0-exp.1。実体・プロジェクト版固定、ライセンス解決。
- A: [EditMode18件](evidence/automation-editmode-result.json)、[PlayMode1件](evidence/automation-play-result.json) 全件PASS。Unity Editor内のTest Runner経由。XMLではなくPipelineの構造化JSONを保存。
- A: [意図的compile失敗](evidence/repair-failure.json)でCS1519×2、[修復後](evidence/repair-success.json) error/warning0。専用RepairProbe.csとmeta残存なし。Scene生成の実例外も修正して成功。
- Console: [最終状態](evidence/automation-console-final.json)のgroundTruthはcompile失敗false、errors0、warnings0。buffer countsは過去履歴を含む。[履歴](evidence/automation-console-history.json): 呼出しの引用符誤り、意図的コンパイルエラー、Input Manager非推奨、重複Play/Stop、削除後meta警告。復旧後に現症はなし。Input Manager移行は未実施。
- CLI: 専用Sceneの4ルート読み戻し、Cube UpdateCount1787→4025と回転変化を観測。別途PlayModeテストで3フレーム以上/角度1度超を検証。MCP:166ツール、evalでEditor版とScene実取得。ツール短縮名重複があるため全ツール互換は未保証。
- R画像: [Cube](evidence/automation-cube.png)、[Brush Lab画面](evidence/automation-brush-lab-view.png)、[出力PNG384x512](evidence/brush-lab.png)を実取得し目視。筆跡はAPIで入力。人のドラッグ/PNGボタン/3preset比較は未検証。小さなGame Viewで紙面が狭く、筆跡が薄い品質課題あり。
- D/H: 未実行。P0全体未合格。環境構築ENVゲートは合格。
## 描画室の検証（2026-10-22 JST）
- Unity 6000.3.26f1 / Windows / Pipeline0.9.0-exp.1、対象ルートのSumiBrushLabを管理コピーへ同期後、再compile failed=false/errors=[]。
- A: [正式XML](evidence/rendering-editmode.xml) total18/passed18/failed0/skipped0。標準TestRunnerApi出力。先行JSONはrendering-room-editmode-18.json。
- R/自動操作: GameView.SendEventのMouseDown/Drag/Upで描画、紙外Upで終了。物理マウス操作とは区別。[画面](evidence/rendering-input-gameview.png)。
- R/A: PNGボタンをSendEventで操作。[PNG](evidence/rendering-input-export.png)は384x512、LoadImage後のColor32配列が表示Textureと一致。[結果](evidence/rendering-input-export.json)。比較中はdiffusionFps=0。向きも紙面表示と整合。
- R: [3preset画像](evidence/rendering-three-presets.png)左からVein/Slash/Bloom、同Bezier入力/Seed32、Stepなし。これはAPI比較で各ボタン操作試験の代用ではない。顔料量251.761429/453.33847/338.54303。[条件値](evidence/rendering-three-presets.json)。全体に淡く質感未合格。
- A: [筆圧](evidence/rendering-pressure.json)同じ直線/Seed32/Vein、中央断面Pigment>.001の画素数9/16/20。筆圧sliderの全操作は未検証。
- R: [枝画面](evidence/rendering-branch-growth.png)、69→72ストローク、frame6643→13481。別waitでもframe4979→5036。枝画面の伸張表示は既存制約として残る。
- Console: [取得結果](evidence/rendering-console.json)groundTruth.errors0/warnings0/compilationFailed=false。履歴には以前のCLI指定誤りと別工程の意図的CS1519が残る。消去による合格化なし。
- 未確認: 人の物理マウス、全preset/slider、Clear/フォーカス復帰の一連の手動QA、モバイル、性能、主観品質。P0-03/05は進行中。

## 2026-10-22 描画室: 色変更と参照筆の第一段階
- 墨色/紙色RGB・色プリセット・表示濃度をColorsパネルに追加。既存線も一括再着色。多色顔料混合ではない。
- Dense/Dry/SplitプリセットとStudyボタン（紙面置換）を追加。濃墨/乾筆/割れ筆を直線・円相・曲線で比較。docs/evidence/ink-study.pngはUnityで実Coreから生成した768x1024画像。
- OSS調査と採用範囲はOSS_AND_PROVENANCE.md。乾燥停止を独自実装。物理的な滲み拡大・煙・飛沫・筆文字全体の参考画像品質は未達。P1/P2は未合格を維持。
- Unity6000.3.26f1再compile成功。EditMode23/23 PASS（rendering-color-editmode.xml）。初回の色テストは乾いた中心点を誤って選び失敗、最大顔料画素へ修正。失敗XMLも保持。Seed731のReplay画素一致、Seed732の差を検証。
- 目視でPNGの濃墨・掠れ・割れを確認。新UIの全ボタン実マウス操作は未検証。以前の手動QA未達も維持。
- 元の10stroke描画はlogs/pre-color-change/painting.pngとstrokes.jsonに退避し、Editor Playへ点列を復元。v1は拡散時間を記録しないため元PNGと完全一致する湿潤状態復元ではない。背景実行override解除。
- 確認方法: UnityのBrushLab Gameビューで描画、Dense/Dry/Splitで筆切替、Colors +でInk/Paper/RGB/Display density。Studyは現在の紙を置換するため先にPNG保存。PNGボタンの保存先は画面上部に表示。
- 次: 筆に沿う不均一な毛束と鋭い終端、濡れ境界の多尺度表現、UI手動QA。参考画像全体の達成と誤認しない。

## 2026-10-22 Effects検証
- A: evidence/rendering-effects-editmode.xml total31/pass31/fail0。最終UI背景修正もその後再compile successful。
- R: evidence/ink-effects.pngは768x1024、Seed731、Step前50strokeのCore出力。evidence/ink-effects-gameview.pngは実GameView1536x672・384x512紙面、Play中拡散後。画像を同条件と混同しない。
- UI: Unity GameView.SendEventでBleed/Pool/Mist/Splatter切替と各settings読戻し成功。Splatter紙上Down/Drag/Upで1stroke/3points。Colors→VermilionでBE3023。PNG保存はdiffusionFps=0に止めて全画素一致。ink-effects-ui.jsonとink-effects-input-color.png参照。
- UI操作の初回不一致はGameView倍率5とウィンドウ原点の違い。倍率1にしてtargetInParentを読み、画面座標に49pxを加えたイベントで成功。人の物理マウス検証ではない。
- Console: ink-effects-console.json groundTruth consoleErrors0/consoleWarnings0/compilationFailed=false。履歴の以前のテスト失敗/意図的診断エラーは消していない。
- 指標: ink-effects-preset-metrics.json。単一Editor内の1回の描画17.9〜21.3ms（192x256、1stroke、描画のみ）。包括性能ベンチマークやFPS合格ではない。768x1024の全見本生成+PNGは1回3472ms。
- 限界: 輪郭/縁は視覚近似、多色混合なし、v1はStep経過を保存しない。モバイル未検証。新UI全slider/旧preset全ボタン/物理フォーカス移動未検証。
