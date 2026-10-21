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
