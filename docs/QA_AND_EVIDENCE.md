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

## 2026-10-22 v0.2最終QA
- Compile: Unity6000.3.26f1、最後の背景Camera追加もfailed=false/errors=[]。
- A: release-editmode.xml total42/pass42/fail0。途中の微小移動試験41/42失敗はrelease-jitter-first-failure.xmlに保存。原因は長い末尾保持と始筆減衰により短線が見えないこと。短線は元圧力で保持点を放出して修正。
- 追加テストAssetのGUIDが33桁だったため初回34件しか発見されず、32桁GUIDへ修正して全件実行を確認。単なる成功フラグだけで受け入れていない。
- R/UI: release-ui.json 30/30 PASS。全10preset、Pressure/Radius/Pigment/Water/Roughness/Bristles、紙RGB/色preset/濃度、筆変更の色保持、Clear、実経路内払い、紙外Up、PNG読戻し、15/50stroke見本。GameView.SendEvent使用。focus-lossはhandler直接呼出しでOSフォーカス移動とは異なる。
- R: release-effects.pngは768x1024のCore出力。release-editor-screen.pngはEditor画面。windows-player/player-screen.pngは実Windows Playerの1280x900画面。画面拡大は出力PNGの解像度を増やさない。
- Build: release-build.json Succeeded/error0/warning0/90188222 bytes。Windows64、BrushLabシーンのみ。
- Player: windows-player/player-smoke.json、exit0、frame1→139、50stroke、PNG384x512一致、通常Replay一致。player-paper.pngとplayer-screen.pngを目視。非表示起動のキャプチャは黒画面だったため通常ウィンドウ起動で再検証。画像が黒い状態を成功画面として扱わない。
- Console: release-console.json groundTruth consoleErrors0/consoleWarnings0/compilationFailed=false。過去の失敗履歴は消していない。
- Player probeは--sumi-smoke-outputを明示した時だけ起動する検証コード。通常起動では自動描画/終了しない。
- 未実施: 人による物理マウス長時間操作、モバイル、全解像度性能、別プロジェクトUPM導入。完成範囲はPC描画初版。

## 最新: 墨脈ゲーム縦切りの初回実装（2026-10-22）
- 開始HEAD 7524efb、作業ツリーclean。Unity6000.3.26f1/Pipeline ready、BrushLab未保存変更なしを確認。完成済SUMIコードを変更せず、Assets/BokumyakuPrototypeへSimulation/Presentation/Persistence/Tests/Editor/Sceneを追加。
- 固定30Hz、seed分離、3敵、枝成長/供給分散/自動迎撃、6周回強化、3成長方針、4恒久強化、敗北/報酬/保存/再出撃。濃墨筆による枝描画、日本語UI、速度1/2倍と一時停止。
- Unity EditMode30/30 PASS。初回27/30で未知版/不正セーブの例外捕捉漏れ3件を検出、修正して全件再合格。初回XMLを保持。独立.NET7検査PASS。
- 実GameView合成クリック＋Update進行で15項目PASS。出撃/一時停止/2倍速/方針変更、実時間2周敗北、分岐購入、報酬保存、恒久購入と読戻し、再出撃。frame1580→11254。QA保存はlogsへ隔離。人の物理マウス試験とは区別。
- 120条件（10seed×3方針×4仮購入戦略）比較済。全敗北、最長71.83秒。供給+範囲が30対応条件中23で単独最長。全120で分岐購入0のため成長/供給トレードオフのバランス受入は未達。操作試験では分岐用に貯蓄して購入できた。
- 次: 枝へ投資する余裕と成長量、初回5〜8分目標を調整。放置/記録プリセット、点墨/貫筆/滲墨、分裂/ボスを段階追加。現3敵/20Wave試作を5敵版GDD完了とはしない。P3/G5全体未合格、モバイル未検証。
- 手順 docs/BOKUMYAKU_PROTOTYPE.md。証拠 docs/evidence/bokumyaku-{editmode.xml,play.json,balance.csv,gameplay.png}。
- Windows Player build Succeeded、90,233,387 bytes、11.433秒、error/warning0。Builds/Bokumyaku/Bokumyaku.exe（Git対象外）。Player起動ログで6000.3.26f1/描画初期化を確認、Player上の操作・長時間フレーム進行は未検証。EditorのPlay/操作試験と区別。
- 初回ビルド予約の報告が取得できず、公式Pipeline build runで成果を確認。CLI beta.14公開を検出したが環境変更は行わず既存beta.13で検証。初期編集途中コンパイルエラーと修正済みテスト失敗は履歴へ保持。

## 墨脈 v0.2検証（2026-10-22）
- Compile: Unity6000.3.26f1、最終SelectedPolicy修正後もcompleted/failed=false/errors=[]。その後のWindows PlayerビルドもSucceeded。ゲームのみ変更し、描画42試験は再実行していない。
- A: bokumyaku-editmode-v2.xml 39/39 PASS（36.32秒）。seed毎tick再現、費用/報酬/保存、手動停止、自動分岐、グラフ/敵上限、最高恒久Lvでの終了を含む。最終UIの方針保持修正は次のR/UIで確認。
- A: scripts/Test-Bokumyaku.ps1、PowerShell7.6.5/.NET10.0.11、9 PASS/0 FAIL。Unity NUnitとは別。3周の恒久進行、2自動構成の終端、切替を確認。
- R/UI: bokumyaku-play-v2.json 15/15 PASS。GameView.SendEventと実Updateによりframe778→10018、3610tick・59撃破・14枝ノード。出撃前の集中保持、一時停止、2倍速、均衡へ変更、自動購入、多枝/手動切替、手動90tickの購入停止、公開API再出撃時の選択保持。最後の再出撃は自然敗北後ではない。人の物理マウスとは区別。
- R/UI: bokumyaku-scroll-v2.json、合成ScrollWheelで操作欄y=99へ移動。bokumyaku-gameplay-v2.pngは実GameView1536x672、Wave8の戦闘中の画面。縦長レイアウト・各端末・全手動操作は未検証。
- Balance: bokumyaku-build-comparison-v2.jsonは10独立seed×3構成=30条件。sourceSha256と現RunSimulator.cs一致。全Defeated。手動群は無購入対照。広域の生存441.83〜529.40秒/中央値502.52秒、多枝472.30〜530.07秒/中央値505.05秒。全seedで各9/12末端。墨/分の中央値470.82/472.53。多枝の生存8勝1敗1同点、Wave3勝7同点、広域の墨/分優位1件。役割分担と5〜8分目標は未合格。初版の120条件CSVは別バージョンの証拠として保持。
- Build: bokumyaku-build-v2.json、StandaloneWindows64、Bokumyakuシーンのみ、90,245,041 bytes、36.369秒、errors0/warnings0。exeのSHA256はbokumyaku-artifact-v2.json。同梱Data/DLLが必要。
- Player: bokumyaku-player-v2.json。--bokumyaku-smoke-output明示時だけQAを起動し専用保存先へ隔離。exit0、frame2→132572、1200tick、8ノード、15撃破、4購入、顔料2204.4077。非表示起動の実行検査であり画面/物理操作/完走/性能保証ではない。通常起動では自動操作も自動終了もしない。
- Console: bokumyaku-console-v2.json、groundTruth compilationFailed=false/consoleErrors0/consoleWarnings0。過去の診断/失敗履歴は保持。Player.logにはException/Error/Failedの一致なし。
- 未達: 周回/記録の経済的差別化、5敵版、武器別戦闘、モバイル、物理入力と長時間手動評価、オフライン収益。今回の増分合格と全体完成を区別する。

## 墨脈 v0.3検証（2026-10-22）
- Compile: Unity6000.3.26f1で最終UI色修正もcompleted/failed=false/errors=[]。Bokumyakuのみ変更、エンジン42試験は再実行していない。
- A: bokumyaku-editmode-v3.xml 51/51 PASS、37.4557秒。貫通の前方/幅/距離/3体制限/同位置順、滲墨の48tick寿命/圧力/重複2/領域96上限、切替クールダウン、二重撃破報酬防止を含む。遅い密集5体では貫筆/滲墨、孤立した高速1体では点墨が有利になる同費用の固定配置試験。自然seedのバランス証拠とは区別。
- A: bokumyaku-standalone-v3.txt、PowerShell7.6.5/.NET10.0.11の純C#実コンパイル、10 PASS/0 FAIL。再現Snapshotは武器/AttackEvent/領域も含む。
- R/UI: bokumyaku-play-v3.json 18/18 PASS。GameView.SendEventと実Update、frame192→16427。3武器の攻撃、滲墨の実領域、保持、一時停止/2倍速、自動購入/成長、スクロール207px、自然敗北4529tick/W12/結晶12、保存、恒久購入/読戻し、実再出撃ボタン。自然敗北の初期化と恒久購入は公開API、他の記載ボタンは合成イベント。物理マウスとは異なる。
- R/UI: bokumyaku-portrait-v3.json、440×1060固定GameView、滲墨ボタン選択とScrollWheelで435px。portrait-top/bottom-v3.pngを目視。重なっていた版表記と暗色Editor skinの白いhoverラベルを修正して再確認。これは縦レイアウト試験でありモバイル実機ではない。
- R: bokumyaku-bleed-v3.pngは実GameViewの持続領域、bokumyaku-result-v3.pngは実自然敗北。領域画像はシミュレーションのFieldsをそのまま可視化。参考画像相当の完成美術の達成とはしない。
- Balance: bokumyaku-weapons-v3-initial.jsonに初期候補、bokumyaku-weapons-v3.jsonに最終候補を保存。各10seed×3投資×3武器=90条件、恒久0/成長Balanced/武器固定、手動群は無購入対照。sourceSha256と測定時ソース一致。全Defeated、最大領域42、最大末端12、容量終了0。
- Balance: 広域の点墨/貫筆/滲墨の生存中央値502.52/426.88/516.62秒、結晶/分5.92/5.60/5.92。多枝505.05/492.80/249.88秒、5.93/5.91/4.87。役割差と5〜8分目標は未合格。報酬式/敵式/供給式の変更や武器固有報酬倍率はなし。
- Build: bokumyaku-build-v3.json、build_d3d50cdd3530、StandaloneWindows64/Bokumyakuシーン、Succeeded、90,251,697 bytes/12.446秒、errors0/warnings0。Builds/Bokumyaku-v0.3/Bokumyaku.exeと同梱Data/DLLが必要。ハッシュはbokumyaku-artifact-v3.json。
- Player: bokumyaku-player-v3.jsonとplayer-launch-v3.json。明示--bokumyaku-smoke-output時だけ動作、QA保存先隔離、exit0、frame2→291212。3武器とも900tick/7枝ノード/9撃破/2購入/顔料1891.2052、実武器とAttackEventの種別一致。滲墨で最大4領域を観測。非表示起動のデータ検査であり、画面/物理入力/完走/FPS計測とは異なる。通常起動では自動操作/終了なし。
- Console: bokumyaku-console-v3.json、現在compilationFailed=false/consoleErrors0/consoleWarnings0。過去履歴は保持。最終Player.logのException/Error/Failed一致0。
- 未達: 経済役割差、5敵、正式120条件、全武器/装備進行、オフライン収益、モバイル、物理入力、長時間手動評価。実装増分の合格とゲーム全体完成を区別する。

## 2026-10-22 全体進捗・素材室準備の確認
- c6f0f17/cleanを開始時に確認し、実コードの3敵/3武器/6周回/4恒久と直前の証拠を照合。既存51NUnit/10独立/18UI/Player結果を進捗へ参照。今回コード変更なし、テスト再実行なし。
- list_projectsで水墨プロジェクトと既存描画室を確認。別の素材専用チャットをcreate_threadで作成、wait_threadsでactiveと着手コメントを確認。画像生成の成功や6点完了は未確認。
- 新規進捗表と素材依頼書をgit diff --checkで確認。素材室はUnity未取込みのフォルダへ納品し、実alpha/寸法/小サイズ識別/出所記録を受入条件とした。素材制作とゲーム表示/実機QAを区別。

## 墨脈 v0.4検証（2026-10-22）
- Compile: Unity6000.3.26f1でcompleted/failed=false/errors=[]。最終Console groundTruthもcompilationFailed=false、現在error/warning0。過去buffer履歴は保持。変更なしのEngine42試験は再実行していない。
- A: bokumyaku-editmode-v4.xml、67/67 PASS、26.9829105秒。自動再出撃16ケースを追加（初期OFF/未保存/終了状態/停止/OFF/単発発火/無効delta/手動cancel）。scripts/Test-Bokumyaku.ps1で純C#実コンパイル、11 PASS/0 FAIL（bokumyaku-standalone-v4.txt）。
- R/UI: bokumyaku-play-v4.json、18/18 PASS、frame1694→4663。GameView合成ボタン入力、実Update、通常保存先と分離。3秒待機/停止/OFF/再ON/手動開始/選択保持、結晶12→計23/完了2回の一度だけ集計、保存障害で予約停止・明示再試行成功を確認。保存障害はディレクトリを保存先に使い期待されたwarning1件を出した。正常ファイル未変更も確認。
- 上記敗北fixtureは純シミュレーションを自然敗北1tick前まで加速し、最後のtick・報酬・保存・待機・再出撃を実Updateで実行。v3の実時間自然敗北試験とは区別。約3秒は1frame最大0.25秒のactive countdownであり、サスペンド中の壁時計待機やオフライン進行ではない。
- R/Art: 6Textureロード、枝alphaは透明194767/墨1841画素、Coreの光学濃度から計算した全alphaと不一致0。停止中のrebuilds=4を維持。bokumyaku-art-import-v4.jsonは原本bytes一致/実import寸法/形式と5,773,568bytesのTexture割当。ゲーム総メモリ/FPS/モバイル計測ではない。
- R/Portrait: bokumyaku-portrait-v4.json、440×1060、Bleedボタンとscroll541px PASS。初回はGameView拡大と同じ評価内のDown/Upでクリック未反映、initial.jsonを保持。fit＋別イベントに分けてDown/Upを送り成功。ゲームコードの修正なし。自動化helperにもfit/イベント間隔を記録。top/bottom PNGを目視。人の物理入力/モバイル実機とは区別。
- R/Images: bokumyaku-gameplay-v4.png（序盤戦闘）、portrait-top/bottom-v4.png、start-v4.png（円相と出撃前）を実GameViewから取得。朱敵/黒枝/金ノードと淡い山水の識別を目視。最高密度・最終美術の受入ではない。
- Balance: bokumyaku-reward-candidates-v4.json。v3の90条件の後処理、戦闘を再走していない。RunSimulator SHAがv3と完全一致、90/90の元報酬一致。p1.2/1.0は各10/10seedで無購入Manualが最効率、採用しない。詳細BOKUMYAKU_REWARD_REVIEW.md。式据置で役割差未合格。
- Build: bokumyaku-build-v4.json、build_9228027bf70d、Succeeded、94,980,185bytes/22.076秒、error/warning0。実行ファイルとDLLのSHAはbokumyaku-artifact-v4.json。
- Player: bokumyaku-player-v4.json、launch-v4.json、exit0、62.99秒、frame2→293258。3武器各900tick/7枝/9撃破/2購入、実武器とAttackEvent一致、滲墨4領域。自動再出撃seed42→43/完了1/結晶12/速度2/設定保持、6Texture、透明195342/墨1266画素。明示probe時のみ自動操作、隔離セーブ。非表示起動のデータ検査であり画面やFPS計測ではない。Player.logのException/Error/Failed一致0。
- ツール失敗の分類: 画像toolのsave_pathをTempからAssets/Tempへ補正。Play再入場中の一時Pipeline切断はCLI statusでreadyを再確認して復旧。statusに非対応caller/skillを付けた1回は引数エラーで、対応書式へ修正。ゲーム不具合/不合格を隠して成功に置換していない。
- 未達: G-V2-04/05、5敵/全武器/正式120条件、長時間周回/途中復帰/オフライン、物理入力、モバイル、最終美術/音/競技/課金。

## 墨脈 v0.5検証（2026-10-22）
- Compile: Unity6000.3.26f1、最終recompile completed/failed=false/errors=[]。新規snapshot/保存/画面を実コンパイル。Engineは変更せず再試験対象外。
- NUnit: bokumyaku-editmode-v5.xml、102/102、65.274秒。初回92/99の失敗をattempt1.xmlへ保持。JsonUtilityがdoubleを隣接値に復元し、未来の厳密一致を壊した。全doubleの正確bit payloadを深copyへ復元し、隣接1ULPだけ整合を認める。テスト許容値の拡大や計算の丸めで隠さず、負値/signedzero/非有限/改変拒否を追加。
- Pure C#: Test-Bokumyaku.ps1 12/12（standalone-v5.txt）。snapshot-v5.jsonの12検査（6武器JSON未来一致＋精度/破損）。Unity未実行の代用品ではなく別証拠。
- UI: play-v5.json 31/31、frame1565→5209。GameViewの合成Down/Upを0.2秒離し、固定30Hzのlive Updateで停止/再開/敗北決済/待機/再出撃。3周で初回結晶15→研究後次周12→3周目16、最高11Wave、研究Lv1。repairは実ダメージ/実獲得墨の状態から+2、checkpointへ保存。
- 敗北fixtureだけ純C#で最終1tick前まで加速。3周の壁時計耐久試験ではない。保存失敗はFileShare.Noneで1回発生させ、失敗中は停止/報酬未確定/自動再出撃なし→解除後明示再試行で1回決済。期待warningは正常動作。失敗初回play-v5-attempt1.jsonの唯一の失敗はJSON厳密復帰、修正後31/31。
- 縦: portrait-v5.json 440×1060、共鳴ボタン/scroll897 PASS。一時GameViewサイズは復元・削除。portrait-top-v5.pngはscroll後を含む実画面。物理入力/タッチ/実機ではない。横gameplay/records-v5.pngも取得・目視。これは機能確認用UIで、参考スマホ美術の合格ではない。
- 比較: systems-v5.json。180武器+120正式戦略+10meta5=310、全状態再走310。固定参加4撤廃。通常係数.05/.08/.10の平均結晶/分2.26/3.72/4.67、中間.08は仮進行速度。子の追加報酬なし。PBボーナスは効率比較に混ぜない。標準初回317.93〜493.13秒/中央値462.07秒/5〜8分7/10。meta0→5の平均Wave37.3→47.5。Record優位と構成均衡は未達。
- 測定後の変更はsnapshot輸送だけ。元測定sourceHashesは保持、変更後hashと独立12検査をsnapshotTransportUpdateへ記録。新しい戦闘310再走を実施したとは書かない。
- Independent QA: campaignのcopy-on-write、同ID再送、古いrevision拒否、移行/破損保護を読取。Presentation例外フィルターのInvalidDataException漏れを修正、本物の例外捕捉/NullReference対照を独立C#で確認。
- ツール上の途中リロードで初回NUnitのXML未出力が1回、変更が落ち着いてから起動し結果取得。recompile toolのHTTP400はstatusでcompleted/failed=falseを確認。evalで存在しないTestRunnerApi.IsTestRunActiveを参照した診断は中止し、実XMLを根拠にした。製品例外とツール診断を混同しない。
- Windows build/別プロセス復帰/最終Consoleは下の最終追記へ。長時間・モバイル・物理入力・面白さ・完成美術・オンライン/課金は未合格。

### v0.5 Windows確認
- 6武器各900tick、7枝/9撃破/2購入、実AttackEvent/武器一致、滲墨領域4、各checkpointの厳密復帰。実再出撃seed42→43/完了1周/結晶15/速度2、6素材と透明枝を確認。
- 保存した状態でアプリを終了し、別プロセスを起動。停止/速度2/累計1周/全snapshot一致を確認し、続行後600tickの未来も全snapshot一致。非表示のopt-in probeであり、FPS/タッチ/実機の証拠ではない。
- 試験用背景実行設定がEditorに戻る挙動を検出。Play終了後PlayerSettingsとApplicationをfalseへAPIで復元。ProjectSettings差分0を確認し、背景設定を戻した最終ビルドで2プロセス試験を再実行し合格。常駐していた別のBokumyakuプロセスは停止・変更していない。

- 最終build_f4ea763aa193: Succeeded、95,018,441bytes、4.102秒、error/warning0。最初のビルドは29.484秒。最終Player97.346秒、別プロセス14.132秒、両exit0。最終ログのException/Error/Failed一致0。成果物/DLLハッシュはartifact-v5.json。Console groundTruthもcompilationFailed=false/error0/warning0（履歴bufferとは区別）。

## 墨脈 v0.6検証（2026-10-22）

- 開始ec15566/main/clean、GitHubも同HEAD。Unity6000.3.26f1/Pipeline0.9.0-exp.1、保存済みBokumyakuシーン。ゲームのみ変更し、Engine/BrushLab/Sceneは変更なし。
- NUnit: bokumyaku-editmode-v6.xml、124/124 PASS、41.217秒。初回122/124は既存の武器等費用テスト2件が強化配列6要素を期待したため失敗。7要素・採墨0へ修正し合格、attempt1.xml保持。新規8ルール試験と14移行ケースは初回から合格。
- 独立C#: Test-Bokumyaku.ps1、12/12（standalone-v6.txt）。Test-BokumyakuRules.ps1は実v0.5 DLL由来seed42 Farming/Pointの1800→2700tickと終端13991tickの全snapshot一致、現行2構成×6武器のJSON復帰後900tick毎tick一致12件を検証。System.Text.Jsonの独立検証とUnity JsonUtilityのNUnitを区別。
- 旧fixtureは変更前Bokumyaku.Simulation.dll（hash/取得時刻はbalance-v6-legacy-fixture.json）から生成。現行snapshotのschema番号だけを書き換えたものではない。旧rules1は6強化/旧AutoBuy/倍率1を維持、現行schema2に明示して再保存。新rules2は7強化と購入cursor、cursorはmod6で蓄積を防止。
- 独立レビュー: v2ヘッダで新snapshotを旧扱いできる不備を補強。移行元activeは実旧schema1/6強化/metadata0だけ許可。v3の欠落/未知履歴RulesVersion拒否、同runIdの精算防止、原本/bak保護、v3破損時のv2への巻戻し禁止を確認。
- UI: play-v6.json、37/37、frame2527→7012。6武器/3構成/新規Farming/停止/速度/3周精算/研究/履歴、採墨の実ボタン購入と過去報酬不変、7強化の厳密読戻し、旧実fixture移行/旧採墨無効/次周から新ルールを確認。旧ファイルbytesも不変。
- UIの敗北fixtureは最終1tick前まで純シミュレーションで加速し、最後のtick・精算・3秒待機・再出撃を実Updateで行った。FileShare.Noneによる意図的保存失敗1件を発生させ、解除後の再試行で一度だけ精算。物理マウス/端末/長時間放置の試験ではない。
- Portrait: portrait-v6.json、440×1060、共鳴ボタンとScrollWheel949px合格。top/bottom-v6.png、横gameplay/records-v6.pngを目視。文字と操作は表示されるが、縦UI下部に余白があり完成スマホ戦闘構図ではない。一時GameViewサイズ・拡大率を復元、Play終了後背景実行false/QAキー空/Scene dirty=falseを確認。
- 数値: 購入順3案300条件→採墨3案各140条件。採用後に未使用21..30のPoint Farming10条件追加。G04は深度54/60・効率51/60で事前閾値合格。G05は合計26/30で未合格。11..20は全候補で測ったため独立holdoutではない。元データ・各中央値・組別差・source hashはbalance-v6-*、選定と限界はselection.json。最大16倍とPoint平均165.7結晶の長期経済影響は未調整。
- Build: build_6138de5220e8、StandaloneWindows64/Bokumyakuシーン、Succeeded、95,022,537bytes、13.055秒、error/warning0。新Builds/Bokumyaku-v0.6、旧成果物保持。Console現在compilationFailed=false/error0/warning0、過去bufferは別に保持。
- ツール: Refresh直後に1回Pipelineが一時未検出。status readyを確認してrecompileはup_to_date。認証や導入を再試行せず、テストXML/画面/ビルドを実証拠にした。
- Windows2プロセスの最終結果は次の追記へ。初回尺・長期経済・長時間/強制終了・IL2CPP/モバイル・物理入力・面白さ・完成美術/音は未合格。

### v0.6 Windows最終結果

- player-v6.json: 6武器各900tick/7枝/9撃破/2購入、実AttackEventと選択武器一致、滲墨4領域、全checkpoint復帰合格。実自動再出撃seed42→43、完了1周/結晶15、6素材と枝透明画素を確認。
- player-resume-v6.json: 保存後に終了して別プロセスで起動、停止状態と速度2、全snapshot一致、再開後600tickも完全一致。resumeレポートの未使用autoRepeat等のfalseは未実施欄で、別プロセス復帰passed=trueを判定に使う。
- 初回106.145秒、復帰14.126秒、両exit0、各ログException/Error/Failed一致0。artifact-v6.jsonへexeとゲームDLLのSHA256を保存。非表示opt-in probe/隔離セーブで、描画FPS・物理入力・モバイル性能の測定ではない。900tickは採墨自動購入前であり、採墨の実ボタンはEditor37項目、後半自動購入は独立実走とNUnitで検証した。

## 2026-10-22 ゲームv0.7 独立検証（最新）

|層|結果・範囲|証拠（docs/evidence/）|
|---|---|---|
|Unity実コンパイル/NUnit|141/141、初回実行で全合格、44.708316秒|bokumyaku-editmode-v7.xml|
|Unity非依存C#|12/12|bokumyaku-standalone-v7.txt|
|旧版継続|変更前v0.6 DLLのFarming/Record、6000→6900tickと終端一致。旧v0.5互換もNUnit対象|bokumyaku-pacing-v7-legacy-fixtures.json、rules-checks.json、Tests/Fixtures/v6-*.json|
|rules3保存|6武器×2構成、各900tickの毎tick復帰一致|bokumyaku-pacing-v7-rules-checks.json|
|候補比較|3案×140条件、採用C固定後20seed。仮案と本体160条件の指定終端指標が一致|bokumyaku-pacing-v7-candidate-*.json、comparison.json、adoption-check.json|
|初回尺|28/30＋17/20＝45/50。G05未合格、5seed約244秒|bokumyaku-pacing-v7-candidate-2*.json|
|初期役割|meta0で深度56/60、効率55/60、各武器の閾値合格|bokumyaku-pacing-v7-comparison.json|
|長期経済|旧rules2で80周回＋同meta20条件。現在rules3の同固定meta20条件は記録1勝2同点7敗、平均−1.4Wave。育成後役割は未合格|bokumyaku-progression-v7.json、bokumyaku-mature-builds-v7.json|
|実画面|41/41、合成MouseDown/Upとlive Update。原本保護移行/旧rules2継続/次周rules3/通常報酬表示|bokumyaku-play-v7.json、gameplay/records/result-v7.png|
|縦画面|440×1060、共鳴選択とスクロール成功、PNG目視確認|bokumyaku-portrait-v7.json、portrait-top/bottom-v7.png|
|Windowsビルド|Succeeded、95,024,073bytes、13,410ms、error/warning0。旧版保持|bokumyaku-build-v7.json|
|Editor終状態|Play停止、compile failed=false、Scene dirty=false、runInBackground=false。現在Console error/warning0|bokumyaku-console-v7.json|

CLI Consoleのbuffer累積error15/warn23と現在groundTruth error0/warn0を区別する。UI試験には意図的な保存ロック警告がある。縦画面probe起動時の誤ったCLI引数type/methodはEditorへ到達前に拒否、発見済みentry指定で修正し合格。CLI更新aliasの既知制約は継続、Editor接続成功と区別する。

結果画面PNGでは通常4結晶＋初達成10を分離し、通常結晶/分1.91と侵入5体を確認。これは機能試作の画面でありユーザー参考の完成美術への到達ではない。スマホ実機、物理操作、FPS、長時間、楽しさ、経済テンポの合格を主張しない。生成ログ/ビルドキャッシュ/通常セーブはコミット対象外。

### v0.7 Windows最終結果
- 6武器各900tick、攻撃/購入/checkpoint復帰、自動再出撃42→43を実Playerで確認。別プロセスで同checkpoint復帰＋600tick未来一致。両passed=true/exit0（105.811秒、14.209秒）。通常セーブは使用しない。PlayerログにException/Error/Failedなし。
- 証拠: evidence/bokumyaku-player-v7.json、bokumyaku-player-resume-v7.json、bokumyaku-player-launch-v7.json、bokumyaku-artifact-v7.json。ビルドサイズ95,024,073bytes。エンジン/Scene/ProjectSettingsにコミット差分なし。

実装コミット: a448fb5。検証スクリプト/証拠/台帳は次の独立コミット。

## 2026-10-22 v0.8 記録後半供給の検証（最新）
- 初期状態03628c2/main clean、Unity6000.3.26f1/Pipeline0.9.0-exp.1接続確認。WindowsApps CLIはsandbox内でアクセス拒否、承認付き同CLIで正常実行。既知CLI更新alias制約を再試行せず使用。
- 3候補各480走＋固定Bの追加480走＝1920走。immutable Git blob基準、独立seed1920件ではない。Bは初期同値、育成Point旧版比+2.6Wave/追加+2.1。育成120組53勝66同1敗、Bleed18は-1Wave。育成Sweep/Ensoの60上限を自然敗北と分離。record-v8-candidate1/2/3.json、fixed11to20.json、selection.json。
- 初期G04既定seed56/60深度・55/60効率は合格維持。追加seed42/60・58/60は一般化未達、基準も同値。G05の初回50条件を旧v7と照合し不変、45/50未合格。純C#本体照合480+50=530PASS、13終端指標（整数・levels完全一致、浮動値1e-10以内）。全snapshot/端末性能検証とは別。record-v8-production.json。
- NUnit157/157（29.1701686秒）。初回156/157はRecordSupplyTestsの合成snapshotに所持墨だけを注入して収入整合検証に拒否されたもの。試験準備で版を直接設定して購入優先のみ検査するよう修正、実v7fixtureの復帰テストは別に保持。attempt1.xmlと最終editmode-v8.xml。製品保存検証を緩和していない。
- 実v7 DLLから変更前に採取した固定育成meta/seed42のFarming・Recordを6000→6900tick/終端まで完全一致。旧v5/v6互換も同NUnit。rules4の供給購入費用/不足時fallback禁止/攻撃条件/旧rules3切替の保持を検証。record-v8-legacy-fixtures.jsonとTests/Fixtures/v7-*。
- Unity非依存C#12/12: bokumyaku-standalone-v8.txt。UI41/41: bokumyaku-play-v8.json。6武器/3周/研究/履歴/保存失敗再試行/旧保存コピー/旧rules3再保存/次周rules4をlive Updateと合成入力で確認。gameplay/records/result-v8.png。結果画面目視で規則4と通常/初達成分離を確認。
- QA保存はlogs内隔離。UIは加速fixtureの最後のtickをlive Updateで進める方式であり、長時間放置/物理マウス/スマホではない。今回レイアウト変更なし、縦画面440×1060試験の最新はv0.7、v0.8で再測定とは記録しない。Play後に背景設定false・QAキー解除・GameView復元。
- rules4の採用は有限比較で得た限定改善。育成役割ゲート/初期追加seed/初回尺/長期経済/美術/実機は未合格を保持する。Windowsの確定結果は次追記。

### v0.8 ビルドと終了状態
- Windows64 build_3efd91eb98b0: Succeeded、95,024,585bytes、12,298ms、error/warning0。Builds/Bokumyaku-v0.8へ新規出力し旧版保持。
- 現在Console groundTruth error0/warning0、compilationFailed=false。Play停止、Scene dirty=false、runInBackground=false、QA SessionStateキー空。累積buffer件数と現在Consoleを区別。
- 実装コミットbb9886f。実画面41/41、旧保存原本保護と新規rules4を確認。次セッションは初回尺G05と上限未到達武器の役割から再開。候補Bの追加seedで1件退行した事実を維持する。

### v0.8 Windows最終結果
- 6武器各900tick・checkpoint・自動再出撃、別プロセスで保存復帰後600tick未来一致。2プロセスともpassed=true/exit0、105.917秒/14.320秒。PlayerログException/Error/Failedなし。
- 証拠はbokumyaku-player-v8.json、player-resume-v8.json、player-launch-v8.json、artifact-v8.json。通常セーブは未使用。描画FPS/モバイル/物理入力/長時間放置の証明ではない。
- 起動先Builds/Bokumyaku-v0.8/Bokumyaku.exe。実装bb9886f、今回の有限比較と機能回帰の増分を受入。G05/育成役割/追加seed初期役割の未達は維持。

失敗XMLはGit登録時にstack-trace行末空白のみ除去（失敗本文・結果は保持）。

## 2026-10-22 v0.9 長期進行基盤
- 基点9639f7e、main clean/同期から開始。Core/QA/Demoの所有を分け、Engine/Scene/Brush Labは変更せず。新規rules5は100,000Wave/36,000,000tick、旧rules1〜4は60Wave/21,600tickを保持。
- HP61以降を二次式、速度を有限上限、報酬Wave倍率を対数式へ変更。同時敵96/領域96/枝64を維持し、全100,000Wave数式と整数上界を独立検証。自然進行12条件は最高79Wave完了、80Wave敗北。高Wave注入30条件と自然到達を区別する。
- 保存先v6へ原本保護コピー移行。実v0.8 DLLから変更前に採取したfixtureで旧ルール未来/終端を照合。途中状態のJsonUtilityコピーによる丸め累積を発見し、strict Restore/Captureで正確値を保つCloneへ修正。許容誤差幅は変更しない。
- 初回Unity182件中178合格: 保存丸め3件と旧60Wave停止条件1件。2回目184件中180合格: 旧移行4件のraw JSON比較をexactDoubleBits一致＋厳密復元状態の全Campaign比較へ変更。900tick後旧DLL一致と原本/.bak bytes一致を保持。失敗XMLを保存。
- Unity CLI更新beta14の案内のみ。承認前のself-updateは行わず、既存beta13/Editor6000.3.26f1で検証。接続消失は再コンパイルのドメイン再読込後に復帰、再導入は不要。
- 強化上限/長期経済は今回未拡張。次は長期育成と周回強化、放置周回/記録ビルド。初回5〜8分45/50の未合格は残す。

### v0.9 Unity受入確定
- 実装SHA dca9fb9。Unity184/184（57.7711037秒）、独立基礎12/12、長期独立検証PASS。Unity初回/2回目の失敗XMLも保持し、最終合格と区別。
- 実画面44/44。旧v8保存原本保持・新周回rules5・七強化・3周・報酬・保存失敗再試行に加え、Wave60越え、注入Wave30000のlive Update進行/保存/復帰一致。highwave-v9.pngで30000/100000と5999:48表示を目視確認。合成入力/注入試験であり物理入力・自然高Wave到達・長時間実測ではない。
- Windows64 build_6788b70521a9成功、95,027,145bytes、29,898ms、error/warning0。出力Builds/Bokumyaku-v0.9/Bokumyaku.exe。ツールの開始/終了時刻は返却値をそのまま保存し、検証時刻とは混同しない。
- Unity現在Console error0/warning0・compilationFailed=false。過去buffer件数を現在エラー数と扱わない。Play停止、Scene dirty=false、背景実行false、QA保存キー解除。
- Engine/BrushLab/Scene/ProjectSettingsの差分なし。旧Measure-BokumyakuSystems/Weaponsは歴史用で現行検証に使用しない。再現手順はTest-BokumyakuLongRunV9.ps1と今回UnityAutomation/PlayerV9スクリプト。
失敗XMLのstack-trace行末空白のみGit登録前に除去。失敗本文と結果は保持。

### v0.9 Windows最終受入・再開点
- Windows実行: 6武器各900tick、checkpoint、自動再出撃に合格。別プロセスから復帰し600tick後の未来一致。両passed=true/exit0、106.373秒と14.078秒。PlayerログのException/Error/Failedなし。
- 証拠: bokumyaku-player-v9.json、player-resume-v9.json、player-launch-v9.json、artifact-v9.json。隔離セーブのみ使用。モバイル/長時間実測/描画FPSの合格ではない。
- 起動先Builds/Bokumyaku-v0.9/Bokumyaku.exe（Data/UnityPlayer/MonoBleedingEdge同梱のまま使用）。実装dca9fb9。今回の長期基盤を受入し、システム全体完成とはしない。
- 次: 強化上限と恒久成長を長期向けに拡張。複数seed・同metaで放置周回の収益と記録構成の到達を比較する。現状は最高79Wave完了の小標本、数万Wave攻略バランスは未達。初回尺G05/育成役割/実機/美術の残件も維持。
