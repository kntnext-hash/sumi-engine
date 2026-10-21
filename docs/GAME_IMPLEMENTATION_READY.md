# 墨脈：Unity導入後の実装開始票

2026-10-21 JST。担当はゲーム部屋。描画Core・Brush Labは別室所有。
今回の完了は「実装順、接続契約、試験、数式検査の準備」であり、プレイ可能版の完成ではない。

## 今回固定する範囲

正本はAGENTS/HANDOFFのルートUPM構成と現コード。GDD末尾の別リポジトリ階層、`com.bokumyaku.sumi`、URP必須の記述は古い案。実パッケージは `com.connectpiece.sumi-engine`、現CPU描画アダプタを接続する。ゲームは生成済みUnitySandboxの `Assets/BokumyakuPrototype/` に置く。先に同名の空プロジェクトを作らない（既存セットアップが未検証フォルダを保護して停止するため）。

最初の小さな実装ではWave1-20、3本から最大12先端、通常/高速/重装の3敵、6周回強化、敗北→墨晶→恒久4種→再挑戦を作る。GDD全体版の5敵のうち分裂とボスは次の増分。最初の3敵版ではボス撃破報酬は0。Wave20終了のtick=7200（30Hz）でTestCompletedを出す。通常敗北として記録せず、正式版の初回5〜8分目標も評価しない。残敵はその時点のsnapshotに残して追加撃破報酬を出さず、試験用の永続保存先だけを使う。同tickでHP0ならDefeatedを優先する。20Waveは最大240秒の試験区間。

## 実装順と受入

|順|成果物・予定配置|受入条件|
|---|---|---|
|G-PREP（今回）|この開始票、The Tower分析、Test-GameBalance.ps1|GDD数式から表を生成し、不整合と未検証を明示。実ゲーム合格とは別。|
|G1|Scripts/Simulation/{GameConfig,RunState,WaveScheduler,EnemyState,RunSimulator}.cs|固定30Hz、seed付き、1x/2x同tick結果一致、敵96上限、HP0で終了、入力なしでも有限tickで停止。|
|G2|Scripts/Simulation/{InkNetwork,SupplyModel,TargetSelector,CombatResolver}.cs|ノード64/tip12、循環なし、供給と枝長の効果、3成長方針の結果差。戦闘は画素に依存しない。|
|G3|Scripts/Simulation/{UpgradeCatalog,RunEconomy,MetaProgress,SaveData}.cs|6強化・4恒久、資金不足/上限/二重報酬を拒否、保存・破損復旧、3周連続。|
|G4|Scripts/Unity/{GameCoordinator,GameHud,EnemyViewPool,InkNetworkView}.cs と Scenes/Bokumyaku.unity|開始→操作→敗北→強化→再挑戦。Game viewで実操作し、描画と判定の位置一致、Console error 0。|
|G5|固定seed10本×3成長方針×4購入戦略=120条件の比較＋手動評価|到達Wave/漏敵種/先端数/供給/購入履歴を比較。120条件は10seedの条件比較であり120独立seedではない。別途同条件を再走して決定性を検査。各方針は一走中固定し、方針切替は別の入力リプレイ試験にする。単一ビルドの常勝を要調整として扱う。|
|後続|分裂・ボス→長期目標→大会ローカル試作→オンライン→課金|各増分に個別試験。本番契約・公開・課金有効化は別途承認。|

初期20Waveでは差が天井に潰れる場合がある。調整用の長いWave上限で別計測し、未強化初回5〜8分というGDD目標をその段階で判定する。G1〜G3は描画ゲートと独立して開発可能。G4の正式SUMI連携合格は描画室の実動確認後。暫定表示を使う場合はBrush Lab合格として扱わない。

## データ契約（最初のコードにする仕様）

- `Bokumyaku.Simulation` はUnityEngine/SUMI参照なし。`Bokumyaku.Unity` のみSimulationと実在 `ConnectPiece.SumiEngine` を参照。試験はゲーム専用Editorアセンブリへ置く。
- 一周の入力: configVersion、runId、seed、恒久Lvスナップショット、tick番号付き購入/方針変更コマンド。描画用乱数は戦闘乱数と分ける。
- 座標: 中心(0,0)、論理半径360、Y上向き。画面の表示変換はAdapterだけで行う。経路長は枝の実長合計、Unity pixelの長さと混同しない。
- 状態: Ready → Running → Defeated または TestCompleted → Result → Ready。Pauseはtickを進めない。描画FPSを戦闘のdtに使わない。
- 1tick順序案: コマンド検証/反映 → 出現 → 移動 → 核接触 → 標的選択/攻撃 → 撃破/報酬 → 成長 → 終了判定 → snapshot。核接触した敵はそのtickの攻撃対象に戻さない。
- 状態snapshot: tick、wave、coreHp、runInk、ノード/先端、敵、購入Lv。表示イベント: BranchGrown、AttackResolved、EnemyKilled、CoreHit、RunEnded。描画が遅くても結果は変えない。
- 初期ターゲットはETA昇順、同値ならHP、同値ならenemyId。固定刻みでも計算順を固定する。異なるCPUのfloat完全一致は別検証。
- 敵上限は黙って出現を捨てず、上限到達をログ化してその走を負荷/バランス不合格とする。GDDの自動核ペナルティは値が未確定のため初版では導入しない。
- 購入は残高/上限検証後に費用減算＋Lv加算を一処理で行う。流墨はリトライで消去、墨晶は保持。報酬のroundは中点を偶数へ丸めるToEvenを採用する実装案（GDDは丸め方式未指定）。
- 保存: schemaVersion、configVersion、通貨、恒久Lv、報酬処理済runId。初版は周回終了後の恒久状態を原子的に保存し、途中中断の続きからの復帰は次の試験で追加。失敗時は直前正常版を保持。
- ランキング/購入用のIDはゲーム保存側に置く。墨の顔料やStrokeLogに通貨・順位・購入権利を混ぜない。

## 起動時の手順

1. `git status -sb`、最新STATE/TASKSを読む。別室の未コミット変更があればそのファイルは触らない。
2. 実在6000.3系Editorのexeを確認。WindowsAppsのunity.exeという名前だけを導入完了の証拠にしない。実行ファイルの版・ProjectVersionを記録。
3. 描画室がUnitySandboxを作成する。ゲーム室は既存Assets/BokumyakuPrototype以下にゲームを実装する。Package mirror/Brush Lab/Runtimeの同時編集を避ける。
4. G1から小さく実装→ゲームEditor Tests→結果記録→commit。数式はTest-GameBalanceの値と照合。GDDの概算表を直接定数化しない。
5. G4で実画面を検証し、5分の操作動画/静止画・Console・テストXMLを保存。実機性能は端末で別試験。

## 最初に観測するログ

ローカルのみ。外部分析SDKは今回不要。
`run_started`, `upgrade_attempted/result`, `branch_grown`, `core_hit`, `wave_completed`, `run_ended`, `meta_upgrade`, `save_result`。
共通: runId、configVersion、seed、tick。敗北時は漏れた敵種・方角・購入Lv・Qの分布を出す。初回購入までの時間、再挑戦の選択、次の目標を説明できるかをプレイテストで確認する。継続率や課金率は実ユーザーデータを得るまで未評価。

## 大会・課金への橋渡し

[The Tower分析](research/THE_TOWER_SYSTEMS.md)の候補を使う。まず通常周回と恒久育成を実証し、その後、順位判定/同点/2大会報酬比較をローカルfixtureで作る。オンラインは結果検証、再送、締切、付与の重複防止までを一組とする。課金は権利と通貨を分離して設計し、広告解除・成長パック・到達パス等の効果を通常周回と大会の両方で比較する。商品価格・外部サービスは未決定。

## 数式検査で判定しない項目
供給Lv17で3枝→12枝の先端圧が回復しても、購入費用や守備範囲が成立するとは限らない。供給Qは枝数に対して厳密減少する一方、実効圧Pはclampにより下限/上限で横ばいになる。試験の比較対象を分ける。
墨晶のmax_waveは初回実装で完了Waveとして扱い、完了0/手動中断は報酬0とする案。GDDの定数4を開幕リトライで繰返し取得できないことを必須テストにする。購入可能性、敗北Wave、課金効果、大会報酬は今回の421数式検査の保証対象外。

## 追加引継ぎ資料との照合（2026-10-21）

- 原本: `../THE_TOWER_TO_BOKUMYAKU_REVERSE_ENGINEERING_HANDOFF.md` と `../BOKUMYAKU_WAVE_BASELINE_v0.2.csv`。原本の内容は変更しない。The Towerの内部データではなく墨脈GDDの仮説式である。
- CSV全60行の5数値列（300値）は小数6桁の丸め許容差5.01e-7以内で再計算一致。ボス指定60件も一致。証拠: `evidence/wave-baseline-review.json`。既存421検査はWave1-20の式等、今回の照合はCSV1-60。別検証として管理。
- HP流入/秒は通常基本敵のHP×スポーン時間中の出現率。ボス・混合敵・射程外・過剰ダメージを含む必要DPSではない。12秒全体の平均とも区別する。
- 敵5種とGate A〜Eが全体の完成目標。3敵/20Waveは最初の実装増分に限る。分裂/10Waveごとのボスを追加し、Wave上限によるTestCompletedではなく自然敗北まで走らせてから、追加引継ぎ書のGate A/Bを評価する。
- 新しい実験条件: seed10本 × 成長方針3種 × 購入戦略4種（疾筆/剛筆/滲筆/均衡基準）=120条件。これは追加資料の3×3=90条件を含み、常設規約の4ビルドも満たす。決定性の再走は別に数え、独立seed数とはしない。恒久ゼロ/各系統Lv5の5設定で比較する段階は600条件。
- RNGは出現/成長/描画の3ストリームに分ける。Lite/Highで同一戦闘ログになることを検証。
- 戦闘計測にtheoretical_dps、effective_dps（過剰ダメージを除外）、coverage_ratio、leak_rate、kill_by_type、income_per_minute、spend_by_upgrade、crystals_per_minuteを追加。集計の分母・期間は実装前に固定。
- Waveログの最低列は追加引継ぎ書5.3に合わせ、6購入Lvと敵種別撃破/漏れは別表で保存。120条件から中央値・四分位・P95を出す際はseed数10の小標本であることを残す。
- 文書の `BokumyakuUnity/` と `com.bokumyaku.sumi` は構成例。現リポジトリのUnitySandbox/Assets/BokumyakuPrototypeとルート `com.connectpiece.sumi-engine` を維持。URPとSeal/StrokeCommandは現コードに実装済みとみなさない。
- 未確定事項と暫定判断は [ASSUMPTIONS.md](ASSUMPTIONS.md)へ集約する。課金・ランキングの分析は長期工程として継続し、今回のMVPに即追加しない。
