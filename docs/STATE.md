# 現在の開発状態

更新: 2026-10-22 JST。以下の現行要約と末尾の最新記録を優先する。

## 現行要約 — ゲームシステム α v0.9（長期基盤）
- 最新変更: 新規rules5を100,000Wave/36,000,000tickへ拡張。旧rules1〜4は60Waveのまま継続。数値上界・同時存在上限を検証し、保存先campaign-v6へ原本保護移行する。
- 独立検証: 全100,000Wave数式、30高Wave注入条件、最終境界、JSON復帰一致に合格。自然進行12条件は全て敗北まで実行し、最高79Wave突破・80Wave敗北。数万Waveへ自然到達した記録ではない。
- 次の実装は長期育成・周回強化の拡張と、放置周回/記録ビルドの調整。現行の強化上限は据置きであり、長期バランスは未完成。Unity/画面/Windowsの今回確定結果は末尾を参照。

- 今回のUnity184/184、画面44/44に合格。実装SHA dca9fb9。Windows版はBuilds/Bokumyaku-v0.9へ生成済み。実Player確定結果は末尾。

### 前版v0.8の受入と継続課題
- Unity 6000.3.26f1 / Pipeline 0.9.0-exp.1を利用可能。描画PC初版v0.2は受入済み。この部屋はゲームのみを変更する。
- 墨脈は5敵/6武器/60Wave上限、枝供給、7周回強化/4恒久強化、核修復、3構成、履歴、途中保存/復帰、自動再出撃を実装。完成美術やオンラインを含む製品完成ではない。
- v0.7は3候補の有限比較から分岐角±0.65を採用。初期meta0の同seed/同武器60組で記録深度56勝、周回効率55勝。G-V2-04合格を維持。初回5〜8分は45/50、G-V2-05未合格。
- v0.8は記録構成の後半供給優先＋枝上限9を採用、新規rules4のみ。育成Pointは旧Record比平均+2.6Wave、固定後+2.1。育成全120組で53勝66同1敗（Bleed seed18は-1Wave）。ゲート合格ではなく限定改善。
- 保存先campaign-v5へ原本保護移行（構造schema3/configv3）。旧rules1/2/3の途中周回は元の計算を継続。実v0.7 DLL由来fixtureの未来/終端一致、旧版互換を検証。
- 次は初回尺と育成後の役割差。初期既定seedのG04合格は維持したが追加11..20の深度42/60で一般化未達（基準版も同値）。60Wave上限で同着の条件と、未到達武器の投資配分を分けて扱う。採墨倍率と恒久育成速度も未調整。今回の3案比較は終了。
- 操作手順: BOKUMYAKU_PROTOTYPE.md。実行証拠と残件は本書末尾、TASKS.md、QA_AND_EVIDENCE.mdを参照。
- 全体工程: BOKUMYAKU_PRODUCTION_PROGRESS.md。素材専用室の第一弾6点は取込み済み。スマホ参考に沿う縦戦場・敵・核・攻撃・音と実機検証は後続。
- 最新検証: Unity157/157、独立12/12、候補と本体480条件＋旧初回50条件一致。実画面41/41、Windows6武器と別プロセス復帰600tick一致、両exit0。起動先 `Builds/Bokumyaku-v0.8/Bokumyaku.exe`。実装・証拠SHAはGit履歴を参照。

以下は経緯を保管した記録。初期の「Editor不在」「ゲーム未実装」は現在の状態ではない。

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
## 2026-10-21 描画実装の事前設計
- ユーザー依頼によりRENDERING_IMPLEMENTATION_READY.mdを作成。0b48158のCore/Canvas/Brush Lab/テストを読み、実装差分・R0〜R7・操作仕様・固定比較入力・v2再生案・履歴上限・計測条件を具体化。
- 既存APIを維持し、まずP0実動、その後Seed比較/質感/乾燥停止/完全再生/性能の順。ゲームファイルと実装コードは変更なし。
- 検証: 計画と現ソースの照合、git diff --check。Unityと既存純C#試験は未実行。P0〜P2の合格状態は変更なし。
- 次: Unity利用可能後に開始票R0〜R2を実施。計画完成をエンジン完成と扱わない。

## 最新: Unity操作環境の実動確認（2026-10-21〜22 JST）
- 6000.3.26f1 (de8167e7440c)をC:/Program Files/Unity 6000.3.26f1/Editor/Unity.exeで確認しCLI登録。6000.6.7f1も併存するが未使用。Editor側ライセンス解決成功。
- UnitySandboxを実生成。未保存初期Sceneによる追加生成例外を修正し、BrushLab/BranchGrowth/ConnectionTestを保存。Pipeline 0.9.0-exp.1 ready、MCP166ツールとeval実読取、CLIでScene/GameObject/Play/Console操作成功。
- Unity EditMode18 PASS、隔離PlayMode1 PASS。意図的CS1519を2件検出後に除去し、再compile error/warning0。最終Console現在件数もerror/warning0。履歴の診断・試験エラーは残して分類。
- Brush LabはAPI入力で描画しPNG384x512出力、Game View確認。実マウス入力、3preset比較、紙面レイアウト/濃度品質は未合格。P0全体を完成扱いしない。
- 続きの一文: 環境ブロッカー解消。P0-03の実入力/preset比較と画面改善を描画担当が進める。Editor操作は同時に複数チャットから行わず、最新状態を確認して着手する。
## 最新: 描画室の入力・PNG検証（2026-10-22 JST）
- ユーザーがこの描画室でのEditor操作継続を指定。34253a9の環境構築を保持し、Brush Labを修正。
- 紙面を384:512の比率で表示し入力矩形と一致。操作欄をスクロール対応・ボタン2段にし、小さいViewでの重なりを改善。PNG名はミリ秒/UUIDで衝突防止。
- Unity再コンパイル成功、EditMode18/18を正式XMLに保存。GameView.SendEventによるMouseDown/Drag/Upで1ストローク3点、紙外解放後drawing=false。PNGボタン操作による保存384x512はTextureと全画素一致。
- 3presetを同入力/Seed32でAPI比較、筆圧0.2/0.6/1.0の断面幅9/16/20画素。枝デモ69→72ストローク、フレーム進行確認。Console現在error/warning0、過去履歴は保存。
- 未達: 人の物理マウスによる全手順、3preset各ボタン/sliderの一通りの操作、フォーカス復帰等の手動QA、淡い画質の改善。P0-03/05は進行中を維持、P1/P2未合格。
- 次: BRUSH_LAB_MANUAL_QAの未確認操作を埋めてP0を閉じ、R3固定入力比較へ進む。EditorはBrushLabを開いた非Play状態、背景実行overrideは復元済み。

## 2026-10-22 描画室: 色変更と参照筆の第一段階
- 墨色/紙色RGB・色プリセット・表示濃度をColorsパネルに追加。既存線も一括再着色。多色顔料混合ではない。
- Dense/Dry/SplitプリセットとStudyボタン（紙面置換）を追加。濃墨/乾筆/割れ筆を直線・円相・曲線で比較。docs/evidence/ink-study.pngはUnityで実Coreから生成した768x1024画像。
- OSS調査と採用範囲はOSS_AND_PROVENANCE.md。乾燥停止を独自実装。物理的な滲み拡大・煙・飛沫・筆文字全体の参考画像品質は未達。P1/P2は未合格を維持。
- Unity6000.3.26f1再compile成功。EditMode23/23 PASS（rendering-color-editmode.xml）。初回の色テストは乾いた中心点を誤って選び失敗、最大顔料画素へ修正。失敗XMLも保持。Seed731のReplay画素一致、Seed732の差を検証。
- 目視でPNGの濃墨・掠れ・割れを確認。新UIの全ボタン実マウス操作は未検証。以前の手動QA未達も維持。
- 元の10stroke描画はlogs/pre-color-change/painting.pngとstrokes.jsonに退避し、Editor Playへ点列を復元。v1は拡散時間を記録しないため元PNGと完全一致する湿潤状態復元ではない。背景実行override解除。
- 確認方法: UnityのBrushLab Gameビューで描画、Dense/Dry/Splitで筆切替、Colors +でInk/Paper/RGB/Display density。Studyは現在の紙を置換するため先にPNG保存。PNGボタンの保存先は画面上部に表示。
- 次: 筆に沿う不均一な毛束と鋭い終端、濡れ境界の多尺度表現、UI手動QA。参考画像全体の達成と誤認しない。

## 2026-10-22 描画表現の追加完了
- 未実装だったBleed（滲み）、Pool（縁に墨が溜まる表現）、Mist（煙状の淡墨）、Splatter（飛沫）をCoreの入力可能な筆として実装。Dense/Dry/Splitは連続毛束と筆圧による尖端へ更新。既存texture省略のv1記録はLegacyとして維持。
- Effects studyは50ストローク・12セル。乾筆/割れ/払い、滲み/墨だまり/飛沫、淡墨/煙/円相、枝/垂れ/渦。全部独自手続き描画で参照画像の貼り付けなし。
- 滲み輪郭・墨だまりの縁は多尺度ノイズと堆積プロファイルによる表現。その後既存保存的拡散が作用。物理流体や参照写真との同一画質を達成したとは主張しない。
- 横長画面で操作欄を右側に配置。背景再描画で文字残像を修正。Colors表示の横スクロールも解消。
- Unity6000.3.26f1 compile成功、EditMode31/31 PASS。新筆5種のReplay/保存性、0圧無描画、異常mode拒否、墨だまり縁/飛沫衛星、12セルSeed差を検証。
- 実GameViewへの合成Mouseイベントで新4preset切替、Splatterの1stroke/3points入力、朱色BE3023切替、PNGボタン保存を確認。保存PNG384x512は表示Textureと全画素一致。人の物理マウス・全sliderの手動評価とは区別。
- 現在Console error/warning0。フレーム1→3021。EditorはBrushLab Play・12セル見本表示、GameView倍率1、背景override解除。元のユーザー描画はlogs/pre-texture-change/painting.pngとstrokes.jsonに保護。
- 今回の描画機能追加は完了。今後の別ゲート: ユーザーによる筆感レビュー、全物理入力QA、v2時系列再生、性能・モバイル・別プロジェクトUPM配布。ゲームには変更なし。

## 最新: Brush Lab v0.2 Windows初版完成（2026-10-22）
- 今回の完成範囲はPCで毛筆/淡墨を描き、色・濃さを調整してPNG保存できるBrush Lab。v0.2.0へ更新。Windows実行ファイルとZIPをBuilds/へ生成（Git対象外）。
- 自動払いは実入力経路内で始終端を細くする。2px未満の手ぶれはタップを維持し、短い線の始終筆が相殺される問題を補正。末尾32点、始筆判定最大201点。記録済みセッション全体の履歴上限は別増分。
- Dry/Splitの紙面接触と連続毛束を改善。Poolは均一な灰色円と黒縁から、内部濃淡と不均一な縁へ改善。Legacy筆の圧力0描画を修正。
- 筆を替えても色と表示濃度を保持。PNG保存前に筆を終了し、出力フォルダーを開く操作を追加。Standalone用の背景Cameraも自動生成。
- Unity6000.3.26f1 compile成功。正式NUnit42/42、実GameView合成イベントのUI30/30。全10preset・全調整slider・色保持・紙外Up・focus-loss handler・PNG読戻し・見本を確認。
- Windows build Succeeded、error/warning0、約90.19MB。実Player起動でframe1→139、50stroke、Seed Replay/PNG384x512全画素一致、1280x900の実Playerスクリーンショットを目視確認。通常ウィンドウで合格、非表示起動の画面キャプチャは黒くなるため証拠に使わない。
- 成果物: Builds/BrushLab/SumiBrushLab.exe、Builds/SumiBrushLab-v0.2-Windows.zip。SHA256はdocs/evidence/release-artifacts.json。
- P0とP1のPC初版受入を合格へ更新。物理マウスによる人の操作は実施したと記録せず、実画面合成入力と手動確認手順を根拠に初版を受け入れる。
- 今回の対象外: モバイル実機、P2全般（性能保証/別プロジェクトGit導入）、v2拡散時系列再生、ゲームP3。画像の完全一致や物理流体の校正は主張しない。
- 再開時は機能追加の依頼がなければP2の計測から。元描画はlogs/pre-release-changeにPNG/点列を保護。Editorは非Play、背景override解除済み。

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

## 最新: 墨脈 v0.2 — 自動投資と成長の調整（2026-10-22）
- 開始HEAD 8032784、main/clean。Core・Presentation・独立QAをファイル分担し、Unity操作は統合担当のみ実施。SUMIのRuntime/Editor/Brush Labは変更なし。
- 分岐費用を貯める2つの自動投資を追加。広域・滲みは9末端、多枝・火力は12末端の候補。手動へ切替可能。基礎攻撃/射程/成長半径と成長閾値を調整し、成長不能時の資源消費を修正。60Wave上限へ延長。
- Unity EditMode39/39、独立C#9/9、GameView合成入力15/15とスクロール確認に合格。出撃前/再出撃で成長方針が失われる不具合をレビューで発見し、保持処理を追加して実UI再検証。Console現在error/warning0。
- Windows build Succeeded、90,245,041 bytes、36.369秒、error/warning0。Builds/Bokumyaku-v0.2/Bokumyaku.exe。実Playerはexit0、1200tick、枝8ノード、自動購入4回、顔料2204.4077を確認。物理マウスやPlayerの周回完走試験ではない。
- 新30条件（10seed×3構成、未強化・均衡）では全自然敗北。自動構成の生存中央値502.52/505.05秒、最終末端9/12。記録候補の生存時間は8勝1敗1同点、到達Waveは3勝7同点。周回候補の墨/分優位は1/10のみ。5〜8分目標と周回/記録の役割差は未合格。
- 今回の有限増分は受入済み。P3/G5全体は進行中。次は周回効率の指標（永続結晶/分も含む）を決め、点墨/貫筆/滲墨の第一弾を段階実装し、同seedでビルドの得意条件を検証。分裂/ボスを足す前に現3敵の比較を残す。
- EditorはBokumyakuシーンの非Play、背景実行falseへ復元。手順BOKUMYAKU_PROTOTYPE.md、証拠docs/evidence/bokumyaku-*-v2.*。初版証拠と元CSVを保持。今回の画面は実GameViewで、参考画像素材の貼り付けなし。

## 2026-10-22 墨脈 v0.3の再開点
- 開始HEAD d39db32、main/origin同期・未コミット変更なし。完成済み描画エンジンを維持してゲーム領域を更新した。
- 実装: 点墨/貫筆/滲墨、切替時のクールダウン保持、実領域の表示、出撃/再出撃の武器保持、結晶/分、縦画面の文字配置とhover色。既存Bokumyakuシーンで動作し、Scene YAML変更なし。
- 検証: 51 NUnit、10独立C#、実GameView18項目、縦画面440×1060、Windows Player3武器各900tick。ビルド90,251,697 bytes/12.446秒、error/warning0。Player exit0、現在Console error/warning0。
- バランス: 10seed×3投資×3武器=90条件、全自然敗北。滲墨は有限2候補の比較で調整。広域・滲みの生存中央値は点墨502.52秒/貫筆426.88秒/滲墨516.62秒、多枝・火力は505.05/492.80/249.88秒。詳細はevidence/bokumyaku-weapons-v3.json。
- 未達: 稼ぎと記録の経済的役割差、5〜8分の一貫した達成、分裂/ボス、5敵版120条件、6武器全体、モバイル/物理入力。成長方針固定の90条件を正式120条件の代わりにしない。
- 次: G-V2-04/05。現報酬式では到達Waveが結晶/分も押し上げるため、Tierと報酬曲線の候補を既存式対照で評価し、周回/記録の役割を設計する。大きな経済設計転換は案と根拠を整理して相談する。その後に分裂/ボスを段階追加。
- EditorはBokumyakuの非Play、runInBackground=false、GameViewの一時縦解像度は削除して元へ復元。通常セーブは保護し、QAはlogsの独立保存先を使用。操作手順はBOKUMYAKU_PROTOTYPE.md。
- Unity公式Plugin/Pipeline/MCPは導入・実接続済み。CLIのbeta.14更新要求に対しインストール済みはbeta.13のままという環境差を保持。MCPと明示グループCLIで検証した。認証/再インストールは今回不要。
- 戦闘実装/テスト/90条件測定のコミット: f4d84e2。UI・Player・台帳の続きは同じmainの後続コミットを参照。

## 2026-10-22 全体進捗と素材室の分業開始
- 実装基準c6f0f17を確認して、BOKUMYAKU_PRODUCTION_PROGRESS.mdへシステム、経済、長期育成、競技/課金、美術、UI、音、QA、公開準備の現状と残件を整理した。根拠のない全体完成率は付けない。
- 同じ水墨プロジェクトに素材専用チャットを作成。threadId: 01a16134-e305-7543-954d-eb9c2c8c17c7。activeで依頼書/スキル確認に着手したことをwait_threadsで確認。
- 初回6点の仕様と受入はBOKUMYAKU_ART_BRIEF.md。素材室はArtSource~/Bokumyaku/batch-001/とdocs/art/だけを所有。ゲーム室がUnity操作/統合/共有4台帳/commitを所有する。
- 本記録時点は制作開始、PNG納品/Unity統合の合格ではない。次回は素材室のdocs/art/ASSET_MANIFEST.mdと成果を確認し、採用候補をゲーム画面へ統合して評価する。ゲーム実装はG-V2-04/05から継続。

## 最新: 墨脈 v0.4 自動周回・第一弾素材統合（2026-10-22）
- 開始HEAD 04e9061、main同期済み。素材室の未コミットArtSource~/docs/artを保護して内容/出所/実寸を確認。Core/Demo/Independent QAを分担、共有Editor/統合/台帳はこのゲーム室が担当。
- 自動周回の初期値OFF、自然敗北の報酬保存成功後のみ約3秒で再出撃。停止/OFF/手動再出撃/保存失敗で待機を制御し、武器/投資/成長/速度を保持。起動中の完了周回数・獲得結晶を重複集計しない。オフライン進行や途中ラン復元は未実装。
- 素材室の和紙/山水/円相/3武器アイコンを採用。原本画素は維持、Unity importのサイズ上限とUI余白で表示。枝の透過合成はゲーム側の追加時更新。SUMI Runtime/Editor、Scene YAMLは未変更。
- Unity6000.3.26f1 compile成功、67 NUnit/11独立検査 PASS。GameView18項目で保存失敗停止/再試行/二重集計防止/実再出撃/透明枝を確認。縦440×1060の選択とスクロール541pxを確認。初回クリック失敗は倍率とイベント間隔を補正、初回JSONも保持。
- Windows v0.4 build Succeeded（94,980,185bytes、22.076秒、error/warning0）。実Player3武器各900tick、6素材、自動再出撃seed42→43/完了1周/結晶12、exit0。Builds/Bokumyaku-v0.4/Bokumyaku.exe。旧v0.3も保持。成果物hashはevidence/bokumyaku-artifact-v4.json。
- 報酬候補p1.4/1.2/1.0を既存90条件から後処理。低指数2案は10seed全件で無購入短命周回が最効率になるため不採用。戦闘/報酬式は据置、G-V2-04/05は未合格。REWARD_REVIEW参照。
- 続き: 固定報酬4と完了実績の積算方式を比較し、自己最高の達成報酬/解放を設計→独立試験→実装。次に分裂/ボス、現行5敵版の正式120条件、敵/核/攻撃美術。全体進捗はBOKUMYAKU_PRODUCTION_PROGRESS.md。
- v0.4増分はPC試作受入。長時間自動周回、物理入力、モバイル性能/品質、オフライン、ランキング/課金、最終美術は未合格。素材室第一弾は納品/統合済み、追加依頼はまだ送っていない。
- 終了状態: Bokumyakuシーンの非Play、isDirty=false、runInBackground=false、GameView最大化解除。一時縦画面サイズを削除。旧ユーザーセーブを変更せずQA保存はlogsへ隔離。

## 最新: 墨脈 v0.5 一人用システムα（2026-10-22）
- ユーザーの「システム面完成まで」を受け、40bb883/clean/main同期から開始。SUMI Runtime/Editor/Brush Lab、既存シーンは変更せず、ゲーム領域だけを拡張。Core/Persistence/Presentationを分担し、統合とEditor/Gitはゲーム室が担当。
- 5敵/6武器/修復、6周回強化/4恒久強化、ホーム/戦闘/研究/記録、3構成、通常/一度きりの節目報酬、最高記録/履歴20、途中保存/停止復帰、自然敗北後の自動周回を接続。
- 保存v2: runId/revision、一時検証→置換→メモリ反映、再送二重報酬防止、旧v1読取移行/原本保持、正常bakと破損原本保護。Unity JSONの隣接doubleへの丸めを実試験で検出し、IEEE754 bit payloadで正確復元。初回の失敗XML/UI JSONも保存。
- 機能検証: Unity6000.3.26f1コンパイル成功、102 NUnit、12独立C#、数値保存12検査、GameView31項目（3周/研究/slot/再開/保存失敗復旧）、縦440×1060の共鳴選択と897pxスクロールPASS。PC物理入力・モバイルとは分ける。
- 比較: 武器180/GDD戦略120/恒久追加10=310条件と310再走は同状態。標準初回中央値462.07秒、5〜8分7/10。恒久Lv5で平均Wave37.3→47.5。Record候補の深度優位は未成立。候補3回で区切り、G-V2-04/05は未合格を維持。
- コード/保存/テストのコミットec30c7e。画面・Player・台帳は後続コミットへ。Windows v0.5のbuildと別プロセス復帰検証は下の最終追記を参照。
- 次セッション: BOKUMYAKU_SYSTEMS_ACCEPTANCE.md / BOKUMYAKU_PRODUCTION_PROGRESS.md / 最新QAから再開。まずビルドの役割/初回尺を調整し、人のプレイでテンポを評価。縦戦場/完成美術/音、長時間/実機は別工程。Tier/実時間研究/オフライン/大会/課金は後続で契約・有効化を独断で行わない。
- 通常ユーザーセーブへ試験データを書かない。Editorはone-shot隔離パスをStart前に注入。Playerも明示probe引数の専用パス。旧Builds/Bokumyaku-v0.4を保護。

### v0.5終了時の再開点
- Windows実行版 `Builds/Bokumyaku-v0.5/Bokumyaku.exe`。6武器と別プロセス復帰検証は合格。主要一人用機能の受入表はBOKUMYAKU_SYSTEMS_ACCEPTANCE.md。
- EditorはBokumyakuの非Play/isDirty=false。PlayerSettings/Application.runInBackground=false、GameView最大化解除、一時縦解像度削除。QA one-shotキー消去。試験中の背景設定をビルドへ残さないよう最終再ビルドと再実行を完了。両プロセスexit0、Console error/warning0。
- 次はG-V2-04/05とプレイ評価。機能の存在を面白さ/実機/完成美術の合格に置換しない。
- GitHub反映: ec30c7e（戦闘/保存）、5fa8116（画面/Windows検証/台帳）をmainへpush済み。最終整形で末尾の空行を除去した。ゲームコード・ビルドには追加変更なし。

## 2026-10-22 — v0.6最終引継ぎ
- 実装af48ab9。採墨・記録構成・旧ルール継続・v3移行、124NUnit/37画面/Windows6武器と別プロセスの保存復帰を完了。Windows95,022,537bytes、実行106.145秒/復帰14.126秒、両exit0、ログエラー一致0。QA・TASKS・操作ガイド・全工程表・受入票を更新。
- 本ターンの有限候補比較は終了。G04は事前条件で合格。G05は26/30のみ5〜8分で未合格、採墨最大16倍の恒久進行も未調整。次はseed4/20/25/28の早期敗北を分析し、序盤漏れ/供給の改善仮説と長期成長の受入条件を決める。無期限に倍率探索しない。
- ユーザーの通常セーブ/旧exe/既存Engineを保護。EditorはBokumyaku非Play、Scene dirty=false、背景実行false、QA overrideなし。一時GameViewサイズ削除。Build/Library/logsはGit対象外。
- 元の購入順比較はExplore-BokumyakuBalance.ps1がec15566のGit blobを読む形へ固定。現在の採墨ルールで旧比較を上書きしない。ValidateSourceOnlyはblob読込/構文のみで、再測定と誤認しない。
- 再開の一文: 最新git状態と本書先頭/末尾を読み、G-V2-05と長期経済の小さな検証単位から継続。参考スマホ画像の完成美術、音、端末性能、実時間研究/オフライン/大会/課金は別工程。

## 2026-10-22 v0.7 分岐・互換・経済診断

- 基点397b666。3案の有限比較から分岐角±0.65を採用。ボスHPと修復案は不採用。新規rules3、途中rules1/2は旧角度±0.35を保持。
- G04: 初期meta0、60組で記録深度56勝/周回効率55勝、各武器は深度8/10以上・効率7/10以上。G05: seed1..30で28/30、固定後31..50で17/20、合計45/50。25/26/31/40/49は約244秒で未達。追加調整やseed選別なし。
- 旧rules2の80周回測定と20条件の同meta比較を保存。Farming20周で恒久購入54回、Record29回。確定rules3の同固定meta14/13/14/12・20条件では記録1勝2同点7敗、平均−1.4Wave。育成後の役割差は未合格（v0.7連続20周育成の結果ではない）。
- campaign-v4へ原本保護コピー、JSON構造schema3/configv3継続。結果画面は通常結晶/分に初達成を含めず、侵入合計と最多種を表示。死因判定ではない。
- Unity141/141（44.708秒）、純C#12/12、画面41/41、縦440×1060の選択/スクロール合格。実v0.6 DLL由来fixtureの未来/終端一致、旧v0.5互換、rules3の12構成900tick毎tick復帰一致。仮案と本体160条件のtick/Wave/報酬/状態/修復/強化Lv一致。
- 仕様・診断: BOKUMYAKU_PACING_V7.md。証拠: evidence/bokumyaku-*-v7.* と bokumyaku-pacing-v7-*.json。Builds/Bokumyaku-v0.7を新規作成、旧バイナリ/通常セーブ/Scene/描画基盤は保護。
- 次: G05と育成後記録構成。供給条件を維持した記録分岐上限6→9を有限比較する候補（未実装・未測定）。初期G04の退行、同meta役割、初回尺を別々に判定。長期経済/美術/音/実機/長時間/物理入力は未合格。

### v0.7 Windows最終結果
- 6武器各900tick、攻撃/購入/checkpoint復帰、自動再出撃42→43を実Playerで確認。別プロセスで同checkpoint復帰＋600tick未来一致。両passed=true/exit0（105.811秒、14.209秒）。通常セーブは使用しない。PlayerログにException/Error/Failedなし。
- 証拠: evidence/bokumyaku-player-v7.json、bokumyaku-player-resume-v7.json、bokumyaku-player-launch-v7.json、bokumyaku-artifact-v7.json。ビルドサイズ95,024,073bytes。エンジン/Scene/ProjectSettingsにコミット差分なし。

実装コミット: a448fb5。検証スクリプト/証拠/台帳は次の独立コミット。

## 2026-10-22 v0.8 記録構成の後半供給（最新）
- 基点03628c2、main clean/同期。Unity6000.3.26f1/Pipeline0.9.0-exp.1。Core比較/QA永続化/Demo照合/親統合で所有分割し、Engine/Scene/BrushLabは変更なし。
- 最大3案を比較。A枝上限のみは供給条件に届かず不変。B後半供給優先を採用。C滲墨を供給へ置換は初期から悪化し不採用。各480走＋B固定後480走＝1920走（反復基準含む、独立1920seedではない）。
- rules4: Recordの枝Lv6以降、Damage条件が足りれば次枝のSupply>=2*nextまで有償供給を優先、枝上限9。既存の毎秒購入枠/費用/敵/報酬は不変。旧rules1〜3は従来処理。保存先v5、schema3/configv3、旧原本/bak保護。
- 育成Point旧Record比平均+2.6Wave、固定後+2.1。全育成120組53勝66同1敗、Bleed seed18は57→56Wave。対Farming育成役割は未合格。Sweep/Ensoは双方60上限の同着で優位を判定できない。
- 初期G04は既定seed深度56/60・効率55/60を維持、追加seedは42/60・58/60で一般化未達（基準版と同じ）。初回Farming50条件は旧版不変、G05は45/50の未合格を保持。
- Unity157/157、独立12/12、採用仮案と本体480条件＋初回50条件一致。初回NUnit156/157の1件は試験で注入した所持墨とincomeEarnedの不整合、試験準備を修正して再実行157/157。失敗XMLも保持。実v7 DLL fixtureの旧未来/終端は初回から合格。
- 詳細BOKUMYAKU_RECORD_V8.md、evidence/record-v8-*.json。次はG05、上限未到達武器の役割、上限で評価不能な条件の整理。今回の候補を増やして再探索しない。完成美術/音/実機/長時間/物理操作は別工程。

### v0.8 ビルドと終了状態
- Windows64 build_3efd91eb98b0: Succeeded、95,024,585bytes、12,298ms、error/warning0。Builds/Bokumyaku-v0.8へ新規出力し旧版保持。
- 現在Console groundTruth error0/warning0、compilationFailed=false。Play停止、Scene dirty=false、runInBackground=false、QA SessionStateキー空。累積buffer件数と現在Consoleを区別。
- 実装コミットbb9886f。実画面41/41、旧保存原本保護と新規rules4を確認。次セッションは初回尺G05と上限未到達武器の役割から再開。候補Bの追加seedで1件退行した事実を維持する。

### v0.8 Windows最終結果
- 6武器各900tick・checkpoint・自動再出撃、別プロセスで保存復帰後600tick未来一致。2プロセスともpassed=true/exit0、105.917秒/14.320秒。PlayerログException/Error/Failedなし。
- 証拠はbokumyaku-player-v8.json、player-resume-v8.json、player-launch-v8.json、artifact-v8.json。通常セーブは未使用。描画FPS/モバイル/物理入力/長時間放置の証明ではない。
- 起動先Builds/Bokumyaku-v0.8/Bokumyaku.exe。実装bb9886f、今回の有限比較と機能回帰の増分を受入。G05/育成役割/追加seed初期役割の未達は維持。

### v0.9 Unity受入確定
- 実装SHA dca9fb9。Unity184/184（57.7711037秒）、独立基礎12/12、長期独立検証PASS。Unity初回/2回目の失敗XMLも保持し、最終合格と区別。
- 実画面44/44。旧v8保存原本保持・新周回rules5・七強化・3周・報酬・保存失敗再試行に加え、Wave60越え、注入Wave30000のlive Update進行/保存/復帰一致。highwave-v9.pngで30000/100000と5999:48表示を目視確認。合成入力/注入試験であり物理入力・自然高Wave到達・長時間実測ではない。
- Windows64 build_6788b70521a9成功、95,027,145bytes、29,898ms、error/warning0。出力Builds/Bokumyaku-v0.9/Bokumyaku.exe。ツールの開始/終了時刻は返却値をそのまま保存し、検証時刻とは混同しない。
- Unity現在Console error0/warning0・compilationFailed=false。過去buffer件数を現在エラー数と扱わない。Play停止、Scene dirty=false、背景実行false、QA保存キー解除。
- Engine/BrushLab/Scene/ProjectSettingsの差分なし。旧Measure-BokumyakuSystems/Weaponsは歴史用で現行検証に使用しない。再現手順はTest-BokumyakuLongRunV9.ps1と今回UnityAutomation/PlayerV9スクリプト。

### v0.9 Windows最終受入・再開点
- Windows実行: 6武器各900tick、checkpoint、自動再出撃に合格。別プロセスから復帰し600tick後の未来一致。両passed=true/exit0、106.373秒と14.078秒。PlayerログのException/Error/Failedなし。
- 証拠: bokumyaku-player-v9.json、player-resume-v9.json、player-launch-v9.json、artifact-v9.json。隔離セーブのみ使用。モバイル/長時間実測/描画FPSの合格ではない。
- 起動先Builds/Bokumyaku-v0.9/Bokumyaku.exe（Data/UnityPlayer/MonoBleedingEdge同梱のまま使用）。実装dca9fb9。今回の長期基盤を受入し、システム全体完成とはしない。
- 次: 強化上限と恒久成長を長期向けに拡張。複数seed・同metaで放置周回の収益と記録構成の到達を比較する。現状は最高79Wave完了の小標本、数万Wave攻略バランスは未達。初回尺G05/育成役割/実機/美術の残件も維持。
