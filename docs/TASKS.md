# 実装タスク / 完了ゲート

タスクは「未着手 / 進行中 / 合格 / BLOCKED」で管理。ユーザーと相談せず技術的細部を決めてよいが、証拠がない工程を合格としない。P0から順に進め、同じ環境にないUnityの実行証拠を捏造しない。

|ID|優先|成果物|受入条件|状態|
|---|---|---|---|---|
|P0-01|P0|Repo/Code監査|Git状態、v0.1ソース、Unity有無、既存契約の監査記録|合格（2026-10-21、STATE参照）|
|P0-02|P0|Compile|Unityに読み込ませConsole error 0、asmdef/testの互換性|合格（6000.3.26f1、復旧後compile error/warning 0）|
|P0-03|P0|Brush Lab|入力→筆跡変化→PNG、3 preset、2サンプルシーン|進行中（2シーン/API描画/PNG確認、実入力と3 preset比較は未合格）|
|P0-04|P0|Test/Replay|EditMode NUnit、同seed同条件で決定的、範囲外/負値/極端入力テスト|合格（Unity EditMode 18/18、v1のStep時系列再生は対象外）|
|P0-05|P0|証拠採取|Unityバージョン/実行方法/スクリーンショット/QA表|進行中（A/R画像取得済み、実マウス操作確認待ち）|
|P1-01|P1|筆先品質|入り止め払い・追従・毛先割れ比較画像を採取|未着手|
|P1-02|P1|乾湿プリセット|Dry / Dense / Wet の定量差と画質レビュー|未着手|
|P1-03|P1|紙・滲み|筆跡の周囲でのみ滲み、乾燥後は固定化|未着手|
|P2-01|P2|負荷計測|複数解像度/筆跡数/CPUメモリ測定をログ化|未着手|
|P2-02|P2|高速化|dirty rectまたは固定レイヤー導入、退行なし|未着手|
|P2-03|P2|UPM配布|別UnityプロジェクトにGit URLからインストールできる|未着手|
|P3-01|P3|墨脈戦闘縦切り|敵3種 / Wave1-20 / 核・枝最大12、攻防進行|未着手|
|P3-02|P3|経済とリトライ|6強化、敗北・報酬・周回、保存の基本|未着手|
|P3-03|P3|独立QA|3戦略比較、停止しない経済、負値やオーバーフローなし|未着手|

## 重要な受入テスト
- 筆圧を変えると筆幅・濃度/毛束が実際に変わる（UIのスライダーだけ動くのはNG）。
- 筆速/墨量の変化が視覚に見える。Seed固定で同じ入力に対して同じ筆跡。
- 描画が続いてもリソースが無限増加しない。古い墨跡の定着が機能。
- デモを起動し、ボタン/ドラッグ操作を使って通常速度で試せる。
- Wave成長、墨圧分散、4つ以上の異なるビルドで100回以上のシミュレーションが可能。到達Waveの中央値を比較し、明白な必勝ビルドは見直す。
- 編集用テストの成功を実機FPSの成功と混同しない。

## セッションの停止・引き継ぎ条件
- 重要機能の受入条件が達した/真正の外部ブロッカー/作業上限。このいずれかで区切り、STATE/WORK_LOGに正確な位置を書いてコミット。
- 成功未確認の自動再試行を延々続けない。失敗ログに原因と対処を記録し、できる独立タスクを続ける。

## 部屋の分担（2026-10-21ユーザー指定）
- この部屋: 墨脈のゲーム仕様・ゲームロジック・ゲームQA。描画独立部分の準備は可能。P3実プレイ合格は描画の実動確認後。
- 水墨描画エンジン開発室: P0/P1/P2の描画、Brush Lab、Core/Unityアダプタ。
- 初期移行: 元スターターcommit 1a4419aをmainへpush済み。独立Core検証18 PASS、Unity NUnit/実画面は未実行。

## 描画室再開点（2026-10-21）
- P0-02から再開。Unity Editor導入・ライセンス有効化が前提。セットアップ後にコンパイル、EditMode、Brush Lab入力・PNG・Consoleの順で証拠を取得する。今回ゲート状態の変更なし。

## Unity自律操作環境ゲート（2026-10-21）
|ID|受入条件|状態|
|---|---|---|
|ENV-01|Unity公式Plugin installed/enabled、CLI起動|合格: 0.1.8-beta / 1.0.0-beta.13|
|ENV-02|MCP設定・initialize/tools/list|合格: 166ツール公開、MCP evalで版/Scene実取得。重複名あり、CLIグループ呼出しを優先|
|ENV-03|Unity 6.3 LTS導入・ライセンス・Pipeline接続|合格: 6000.3.26f1 / Pipeline 0.9.0-exp.1 ready。Editorライセンス解決、Cloudログインは未実施|
|ENV-04|専用Scene/4 GameObject/C# compile/Play進行/Console/修正ループ|合格: 4オブジェクト/PlayMode 1 PASS/CS1519検出→復旧、最終Console 0|
|ENV-05|AGENTSと再構築/有限ループ手順|合格: CODEX_UNITY_SETUP.md|
ENVの構築検証は完了。描画P0全体は未合格で、次はP0-03の実入力・preset比較と画面配置確認。

## ゲーム準備ゲート（描画とは独立）
|ID|成果物|状態|
|---|---|---|
|G-PREP|実装開始票・公式競合分析・数式検査421項目|合格（準備のみ、2026-10-21）|
|G1|固定tick/seed/出現/敗北のシミュレーション|未着手|
|G2|枝成長/供給/標的/戦闘|未着手|
|G3|周回購入/恒久強化/保存|未着手|
|G4|Unity画面・SUMI連携|未着手（描画実動確認に依存）|
|G5|10seed×3成長方針×4購入戦略と手動評価|未着手|
詳細はGAME_IMPLEMENTATION_READY.md。P3全体の受入を置き換えない。大会・課金は本体周回の実証後。

## 追加引継ぎ反映（2026-10-21）
- G5は10seed×3成長方針×4購入戦略=120条件へ更新（再走による水増しではない）。恒久設定5種の比較は後続600条件。
- 5敵/自然敗北までのGate A/B評価を完了前提とする。3敵/20Waveの中間増分だけでは達成としない。
- 仕様未確定点はASSUMPTIONS.md。CSV1-60の数値照合合格は戦闘・経済の合格を意味しない。
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
## 描画室のP0増分（2026-10-22）
- P0-03: GameViewへの合成マウスイベントによる入力終了/PNGボタン、APIによる3preset/筆圧比較を確認。紙面比率/操作欄/PNG名を修正。
- P0-05: rendering-editmode.xml、入力/PNG画素比較、3preset/枝デモ画像、Console証拠を追加。
- 物理マウスの全手動QAと各preset/slider操作は未完。P0全体を完了へ変更しない。次はこの残件を閉じる。

## 2026-10-22 描画室: 色変更と参照筆の第一段階
- 墨色/紙色RGB・色プリセット・表示濃度をColorsパネルに追加。既存線も一括再着色。多色顔料混合ではない。
- Dense/Dry/SplitプリセットとStudyボタン（紙面置換）を追加。濃墨/乾筆/割れ筆を直線・円相・曲線で比較。docs/evidence/ink-study.pngはUnityで実Coreから生成した768x1024画像。
- OSS調査と採用範囲はOSS_AND_PROVENANCE.md。乾燥停止を独自実装。物理的な滲み拡大・煙・飛沫・筆文字全体の参考画像品質は未達。P1/P2は未合格を維持。
- Unity6000.3.26f1再compile成功。EditMode23/23 PASS（rendering-color-editmode.xml）。初回の色テストは乾いた中心点を誤って選び失敗、最大顔料画素へ修正。失敗XMLも保持。Seed731のReplay画素一致、Seed732の差を検証。
- 目視でPNGの濃墨・掠れ・割れを確認。新UIの全ボタン実マウス操作は未検証。以前の手動QA未達も維持。
- 元の10stroke描画はlogs/pre-color-change/painting.pngとstrokes.jsonに退避し、Editor Playへ点列を復元。v1は拡散時間を記録しないため元PNGと完全一致する湿潤状態復元ではない。背景実行override解除。
- 確認方法: UnityのBrushLab Gameビューで描画、Dense/Dry/Splitで筆切替、Colors +でInk/Paper/RGB/Display density。Studyは現在の紙を置換するため先にPNG保存。PNGボタンの保存先は画面上部に表示。
- 次: 筆に沿う不均一な毛束と鋭い終端、濡れ境界の多尺度表現、UI手動QA。参考画像全体の達成と誤認しない。
