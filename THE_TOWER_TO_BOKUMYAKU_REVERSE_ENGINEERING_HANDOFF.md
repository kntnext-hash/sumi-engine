# The Tower → 墨脈（BOKUMYAKU）システム解析・再設計引き継ぎ書

**版**: v1.0  
**作成日**: 2026-10-21  
**用途**: Codex / Unity に渡す、The Tower型Incremental Defenseの機構理解と『墨脈—BOKUMYAKU』独自実装の技術資料  
**対象環境**: Unity 6系 / URP / C# / WindowsローカルCodex / Android・iOS（縦画面）  
**先行資料**: `BOKUMYAKU_GDD_v0.2_Unity.md`、`SUMI_ENGINE_Technical_Spec_v0.1.md`（2026-10-18）、旧`BOKUMYAKU_GDD_v0.1.md`（Godot草案）

> **重要な事実認定**：以前の対話で、The TowerのAPK実体に対する完全な逆コンパイル・内部データ抽出・独自検証済み実計算式の取得は行っていない。以下の**The Tower側**は公開ゲーム情報とゲームデザイン上の分析であり、**墨脈の数式は独自設計の初期仮説**である。両者を混同して「本家から抽出した内部式」と記述してはならない。

---

## 0. これを読むCodexへの最重要指示

1. **The Towerのソースを復元・複製することが目的ではない。** 公開されたプレイ構造を分析し、『墨脈』で独立したシミュレーションを構築する。
2. The Towerの「周回中の強化→限界到達→周回外の恒久強化→再挑戦」という**状態遷移と因果構造**を参考にする。
3. 墨脈固有の核は、**自律増殖する枝グラフ**と**枝数に応じた墨供給分散**。中央固定砲台への単純な見た目の置換にしてはならない。
4. GDD v0.2の係数を**唯一の初期バランス基準**とする。追加の推測式を本家実測値と称して入れない。
5. ロジックをUnity描画から分離して、固定seed＋固定tickで多数周回をシミュレーションできるようにする。
6. 『The Tower的な成長の手触り』の評価は、ソース・UI・固有テーブルの模倣ではなく、定量指標で行う。

---

## 1. 何をThe Towerから分析したか

### 1.1 The Towerの公開プレイ情報から確認できること（機構レベル）

| 仕組み | 観察・公開情報で確認できる内容 | 墨脈に応用する設計原理 |
|---|---|---|
| 自動防衛 | 防衛ユニットが自動攻撃し、プレイヤーは強化と構成選択に集中する | 枝先端の自動標的・自動攻撃、介入は成長方針と強化 |
| Wave進行 | 敵が段階的に迫り、Waveによって負荷が上がる。通常は10Waveごとにボス | HP・密度・速度・敵混合比を別々に拡大させる |
| 戦闘中Cash | 敵撃破・Wave終了などで獲得し、その周回中の強化に使う | 周回内通貨「流墨」 |
| 恒久Coins | 周回外のWorkshop・Labs等の成長に使う | 永久通貨「墨晶」＋将来の研究 |
| Workshop | Attack/Defense/Utility系。周回前はCoinsによる恒久強化、周回中はCashによる一時強化 | 墨脈に合わせた周回中6系統＋恒久4系統に再編 |
| Labs | 時間をかけた研究によって長期成長する | 正式版候補「古筆研究」。MVPからは除外 |
| Free Upgrades | Wave終了時、条件・確率によって無償で強化が進む仕組み | 将来の『自律学習』など。MVPでは不要 |
| Perks / Cards / Modules等 | 通常のレベル増加と異なる修飾・ビルド選択が存在 | 将来『印譜』『書巻』に独自転換。MVPでは不要 |
| Tier・到達記録 | 挑戦難度や到達Waveを長期目標にする | 限界Wave、自己ベスト、節目解放 |

**参考**：The Tower WikiのGameplay、Currency、Workshop Upgrades、Enemies、Free Upgrades、Perks。末尾の参照リンクを参照。公開Wikiはコミュニティ編集情報なので、現行ゲームのバージョンと一致する保証はない。

### 1.2 取得していないもの・未検証のもの

- The TowerのAPKを対象にした逆コンパイル結果、DLL/IL2CPPメソッド、内部クラス、実装アルゴリズムの検証結果。
- The Tower本体の厳密なWave別HP関数、敵出現頻度関数、敵速度関数、難易度Tier倍率。
- 本家の全Workshop費用計算、レベルごとの数値表の完全収集。
- 画面内の攻撃順序、フレーム同時更新、ターゲット選択や敵の衝突処理の内部実装。
- パッチバージョン別の実測差分。

従って、**下記の `16 × 1.072^(w−1)` や `1.16^level` などはThe Towerの式ではない。墨脈用に新規設計した係数である。**

---

## 2. The Tower型Incremental Defenseの因果エンジン（抽象分析）

```text
難易度Tier / 開始時の恒久強化
            ↓
     Wave w の負荷生成
    ├─ HP増加
    ├─ 出現数増加
    ├─ 速度・属性変化
    └─ 定期ボス / 特殊敵
            ↓
  攻撃力 × 攻撃速度 × 有効射程 × 標的効率
            ↓
  実効撃破数 / 漏れ数 / 拠点被害
       ↙            ↘
周回内資金増加        拠点耐久減少
   ↓                    ↓
周回内強化              敗北
   ↓                    ↓
より高いWave          恒久資源の獲得
                         ↓
                    永続アップグレード
                         ↓
                      再挑戦
```

設計上の要点：

- **HPだけを上げて難しくしない。** HP、出現率、敵タイプ、速度、配置、到達時間が異なる種類の圧力を形成する。
- **カタログDPSと実効DPSを区別する。** DPSが十分でも、射程外・標的変更ロス・敵の偏在により防衛は破れる。
- **経済成長と難易度成長を別曲線で扱う。** 収入が増え、強化費も上がり、選択によって到達限界が変わる。
- **失敗しても累積する価値がある。** 敗北は周回外成長の入口であり、次のランの初期条件が変化する。
- **長期維持には複数の進行軸がいる。** ただし墨脈MVPでは研究・カード・上位Tier等を最初から詰め込まない。

### 2.1 見るべき測定量（DPS一個では不十分）

| 指標 | 定義 | 意味 |
|---|---|---|
| `incoming_hp_per_sec` | 敵スポーン量×敵種別HPの期待値 | 理論的な処理需要の目安 |
| `theoretical_dps` | 各tipの1撃ダメージ×攻撃頻度の合計 | 火力の上限、配置ロスを含まない |
| `effective_dps` | 実ダメージ合計／経過時間（過剰ダメージ除外） | 実際の処理効率 |
| `coverage_ratio` | 戦闘領域や脅威セクターを射程で覆う割合 | 枝の場所の良し悪し |
| `leak_rate` | 墨核へ到達した敵／スポーン敵 | ビルドの弱点 |
| `kill_by_type` | 敵種ごとの撃破数・漏れ数 | 高速・重装・分裂への耐性 |
| `income_per_minute` | 周回中に得た流墨／経過分 | 経済圧 |
| `spend_by_upgrade` | 強化系統ごとの累計支出 | 一択化・死に強化の発見 |
| `max_wave` | 到達Wave | 周回の進行度 |
| `crystals_per_minute` | 墨晶／周回時間 | 短時間稼ぎの最適化問題 |

---

## 3. 墨脈の確定済み構造（GDD v0.2から引継ぎ）

### 3.1 体験の定義

- 生成りの和紙上の中央「墨核」から**3本の墨の枝**が発生する。
- 枝は敵の脅威に応じて**伸長・分岐**する。
- 墨の供給は有限。**枝を増やすとカバーが広がる一方、各tipの威力が下がる**。
- プレイヤーは**均衡・迎撃・集中**の成長方針を随時変更し、6強化から選ぶ。
- 敗北すると「墨晶」を獲得し、永続強化して再挑戦。
- 周回の経路は抽象水墨画になる。毛筆表現は独立したSUMI ENGINEが描画する。

### 3.2 セッション・戦場

| 仕様 | 初期値 | 備考 |
|---|---:|---|
| Wave長 | 12秒 | 9秒スポーン＋3秒の間。前Waveの残敵がいても次Wave進行 |
| プレイ速度 | 1x / 2x | 固定tickでシミュレーションを進める |
| 初回目標 | 5〜8分 | 約Wave25〜40で自然敗北を狙う仮目標。未検証 |
| 墨核耐久 | 10 | 通常侵入で1損失、重墨2、ボス4 |
| 戦場半径 | 360 | ゲーム内論理単位 |
| 初期の枝 | 3 | rootから約70単位 |
| 成長一回の距離 | 25〜55 | 根から外へ。折返し・過密配置を抑制 |
| 最大tip | 12 | `branch_lv`で増加 |
| 最大InkNode | 64 | MVP上限 |
| 最大同時敵 | 96 | 上限負荷時は難度が下がる単純スポーン破棄を避ける |
| Tick | 30Hz | ゲームシミュレーションは描画非依存 |
| 描画目標 | 60fps | ハードウェア上の目標値 |

### 3.3 敵Wave成長式【墨脈独自の仮説】

`w` は1から始まる整数Wave。

```text
HP_base(w) = 16 * 1.072^(w-1) * (1 + 0.12 * floor((w-1)/10))
spawn_per_sec(w) = min(4.8, 0.50 + 0.027*w + 0.0007*w^2)
enemy_speed_base(w) = 35 * (1 + 0.006*(w-1))
expected_enemies_per_wave = 9 * spawn_per_sec(w)
```

- Wave10ごとに別枠でボス「朱核」を1体出す。
- 速度は敵種別係数を掛ける。
- 一般敵の墨核接触ダメージにWave倍率は掛けない（MVP）。
- スポーン時刻・タイプ・角度は**固定seed**の生成規則から決定し、1x/2xで一致させる。
- 流墨/敵HP等を1Tickごとに再計算せず、Waveや購入イベントで必要な更新のみ行う。

**参考ベース値（厳密な本家データではない）**

| Wave | 基本敵HP | 出現/秒 | 9秒間の期待出現数 | HP流入/秒の単純積 |
|---:|---:|---:|---:|---:|
| 1 | 16.0 | 0.528 | 4.75 | 8.44 |
| 10 | 29.9 | 0.840 | 7.56 | 25.13 |
| 20 | 67.1 | 1.320 | 11.88 | 88.64 |
| 30 | 149.0 | 1.940 | 17.46 | 289.06 |
| 40 | 327.5 | 2.700 | 24.30 | 884.34 |
| 50 | 714.4 | 3.600 | 32.40 | 2571.75 |

> 最終列は基本敵のHP×出現頻度のみ。敵種の混合、同時スポーン、射程ロス、残敵、ボス、過剰ダメージ、移動による接触を反映しない。**「必要DPS」そのものではない。**

### 3.4 敵種【墨脈独自】

| 内部タイプ | 名称 | 開始Wave | HP倍率 | 速度倍率 | 核接触被害 | 特徴 |
|---|---|---:|---:|---:|---:|---|
| Basic | 点墨 | 1 | 1.00 | 1.00 | 1 | 標準 |
| Fast | 疾墨 | 4 | 0.65 | 1.80 | 1 | 死角を突く |
| Tank | 重墨 | 8 | 3.20 | 0.65 | 2 | 高HP |
| Splitter | 裂墨 | 15 | 0.90 | 1.00 | 1 | 死亡時HP35%の子2体。再分裂なし |
| Boss | 朱核 | 10の倍数 | 12.00 | 0.55 | 4 | 定期ボス・通常出現枠と別 |

**未固定**：Wave別の通常敵タイプ抽選確率。Codexがこの未確定項目を設計・明示すること。既にGDDに定めた登場Waveとタイプ倍率は維持すること。

### 3.5 墨脈（木グラフ）と成長【墨脈独自】

```text
InkNode { id, parent_id, position, length_from_core,
          width, child_ids, is_tip, growth_bias }
InkEdge { parent_id, child_id, control_point, visual_seed }
```

- `growth_charge += 1` / 敵撃破。
- `T_growth = 6 + 2 * growth_event_count`。イベントごとに再判定。
- tip枠に余裕がある場合は、親tipを非tip化し、**子2本へ分岐**（稼働tipは+1）。
- tip枠が上限に達していれば、**子1本へ延長**（稼働tip数は維持）。
- ノードはrootを含め最大64。これ以上は成長イベントを安全に停止・処理。
- 領域は12セクター（30°ずつ）、脅威は2秒ごとに更新。

```text
ETA_e = distance_to_core_e / max(speed_e, epsilon)
threat[s] = Σ_(enemy e in sector s) ((0.5 + HP_e/HP_base(w)) / max(0.7, ETA_e))

均衡  score = 0.6*threat + 1.5*uncovered
迎撃  score = 2.0*threat + 0.3*uncovered
集中  score = 0.5*threat + 1.5*pressure_proximity
```

`uncovered`/`pressure_proximity`は0〜1に正規化。微小ノイズは固定seed依存。衝突回避・端クランプ・角度制約を実装。

### 3.6 墨の供給と威力【最優先の独自数式】

```text
n = 稼働tip数
L_i = root から tip i までの合計経路長
Q0 = 12 * 1.09^supply_lv * (1 + 0.035*meta_supply_lv)
Q_i = (Q0 / n) * exp(-0.0013 * L_i)
P_i = clamp(sqrt(Q_i / 3.0), 0.45, 1.65)

damage_i = 4.0 * 1.10^damage_lv * (1 + 0.025*meta_damage_lv) * P_i
attack_rate_i = min(4.5, 1.25 * 1.07^speed_lv)  // attacks per second
range_i = 65 + 4 * width_lv
```

- 初期設定では各tipが独立した攻撃クールダウンを持ち、最も墨核到達までの見込み時間が短い敵を優先。
- 射程にいる敵がいなければ撃たない。命中は計算上即時、絵として払いのStrokeを発行する。
- `Q_i` と `P_i` の値をHUD・デバッグログへ出すこと。
- **注意**：tip増加は1tipあたり威力を下げるが、全tipの理論合算火力は増え得る。『枝増加が必ず弱い』とはならない。総DPS、実効DPS、セクターカバーと同時に検証する。
- `Q0`の永続強化倍率は先行GDDの『基本Q0+3.5%/Lv』を一元化するため上式を初期実装案とする（複数の乗算順をテスト）。

#### 滲み（bleed）

```text
bleed_lv >= 1 なら命中時に0.7秒のフィールドを発生
radius = 10 + 2*bleed_lv
DOT_per_sec = base_damage * 0.08 * bleed_lv
同一対象へのフィールド重複は最大2スタック
```

範囲判定は円距離。紙へ滲む見た目とは別に処理する。将来の物理流体を戦闘結果へ直結させない。

### 3.7 周回中の経済・6強化【墨脈独自】

```text
cost_u(level) = ceil(base_cost_u * 1.16^level * (1 + 0.008*level^2))
run_ink_on_kill = round(4 * enemy_yield_multiplier * (1 + 0.018*w)
                        * (1 + 0.03*meta_ink_lv))
```

- `level=0`で初回価格が`base_cost_u`となる。購入後にレベルを1増やす。
- 通貨は不足時に減算しない。maxレベルを超える購入も禁止。
- 報酬丸めの方式（MidpointRoundingなど）を決め、プラットフォーム差を出さない。

| 項目 | Key | Base Cost | Level効果 | Max Lv |
|---|---|---:|---|---:|
| 筆威 | `damage_lv` | 10 | `1.10^Lv`倍 | 30 |
| 筆速 | `speed_lv` | 12 | `1.07^Lv`倍、4.5回/秒まで | 25 |
| 墨圧 | `supply_lv` | 15 | `Q0×1.09^Lv` | 25 |
| 枝分 | `branch_lv` | 22 | 最大tip=`min(12,3+Lv)` | 9 |
| 筆幅 | `width_lv` | 12 | 射程 `65+4Lv` | 10 |
| 滲み | `bleed_lv` | 18 | 滲み半径・DoT増加 | 10 |

- `enemy_yield_multiplier`: 点墨1.0、疾墨1.1、重墨2.0、裂墨1.4、ボス12.0。
- 補修は例外的な消耗品：初回25、以後×1.5、1周5回まで、使用時に耐久2回復（最大10）。
- 周回開始時`run_ink = 0`。次周回へ持ち越さない。
- Wave終了時の追加資金・利息は**墨脈MVPの既定要素ではない**。本家に存在しても無断追加しない。

### 3.8 恒久育成（墨晶）【墨脈独自】

```text
crystals = floor(4 + 0.25 * max_wave^1.40 + 3 * bosses_defeated)
permanent_cost_u(level) = ceil(base_cost_u * 1.48^level)
```

| 項目 | Base Cost | 増加効果 | 上限 |
|---|---:|---|---:|
| 核の強さ | 8 | ダメージ+2.5%/Lv | 20 |
| 墨の供給 | 10 | Q0+3.5%/Lv | 20 |
| 成長速度 | 8 | 成長閾値−2%/Lv、最大−35% | 20 |
| 墨の採集 | 12 | 流墨報酬+3%/Lv | 20 |

**課題**：成長速度メタボーナスを閾値の浮動小数のまま加算するか整数化するか未固定。固定seedリプレイのため、GDDに明示のない丸め則はテストの前に`balance.json`または仕様へ記録する。

### 3.9 成長ビルドの検証対象

| ビルド | 主軸 | 強み仮説 | 脆さ仮説 |
|---|---|---|---|
| 疾筆 | 筆速＋枝分＋筆幅 | 速い敵・広範囲 | tip当たり墨圧が減る |
| 剛筆 | 筆威＋墨圧 | ボス・高HP | 空間的な死角 |
| 滲筆 | 筆幅＋滲み＋墨圧 | 密集・分裂敵 | 序盤単体火力 |

成功判定は「どれかが常に最強」ではなく、敵構成とseedにより有利不利が変わること。

---

## 4. Unity実装アーキテクチャ

```text
BokumyakuUnity/
├─ Assets/Bokumyaku/
│  ├─ Scenes/
│  ├─ Scripts/Simulation/             # UnityEngine非依存のPure C#
│  │  ├─ WaveDirector.cs
│  │  ├─ EnemyModel.cs
│  │  ├─ CombatSystem.cs
│  │  ├─ InkGraph.cs
│  │  ├─ InkGrowthSystem.cs
│  │  ├─ InkPressureSystem.cs
│  │  ├─ EconomySystem.cs
│  │  ├─ MetaProgression.cs
│  │  ├─ DeterministicRng.cs
│  │  └─ RunSimulator.cs
│  ├─ Scripts/Unity/                  # Views、入力、描画橋渡し
│  │  ├─ GameCoordinator.cs
│  │  ├─ InkGameplayBridge.cs
│  │  ├─ EnemyViewPool.cs
│  │  └─ InkNetworkView.cs
│  ├─ Scripts/UI/
│  └─ Balance/balance.json
└─ Packages/com.bokumyaku.sumi/      # 汎用描画エンジン（独立Package）
   ├─ Runtime/Core/
   ├─ Runtime/Unity/
   ├─ Runtime/Rendering/
   └─ Tests/
```

**依存の向き**：`Bokumyaku.Unity → Bokumyaku.Simulation + Sumi.Unity`。`Sumi`がゲームロジックを参照してはならない。

- ロジックは固定30Hz、レンダリングは可変で60fpsを目標。`Time.deltaTime`の大きさやGameView表示負荷でWave結果が変化しないようにする。
- RunSimulatorは**ヘッドレスで連続周回できる**ようにし、まずUnity Editorを使わないC#テストで検証する。
- 入力イベントはtick順に処理する。購入・方針変更はリプレイ可能なコマンドログに残す。
- RNGストリームは「Waveスポーン」「枝成長候補」「エフェクト描画」を分け、描画の乱数消費でゲーム内運命が変わらないようにする。
- 敵の位置・攻撃判定はシミュレータが判定。Strokeの輪郭と当たり判定を混同しない。
- `balance.json`にすべての係数、上限、敵出現率、価格表を集約。構造が増える場合はScriptableObjectからエクスポート可能な同等データでもよい。
- 保存は`save_version`を持つJSON（`Application.persistentDataPath`）。破損時の安全復旧、バージョンマイグレーションの最低限を用意。

### SUMI ENGINEとの接続イベント

```text
InkNodeGrowth(parent, child, path, pressure) → StrokeCommand(Vein)
InkTipAttack(tip, enemy, damage, speed)    → StrokeCommand(Slash)
EnemyDefeated(position, bleed_lv)          → StrokeCommand(Bloom)
BossDefeated(position)                     → StrokeCommand(Seal)
```

描画パラメータ（顔料、水分、筆圧）とゲーム資源（墨供給Q0、流墨）を別データ型に保つ。初期は軽量な疑似滲み（Lite）を採用。

---

## 5. 実測・模倣研究をする場合のプロトコル

The Towerの公開情報と**正当にプレイして観察できる挙動**から、成長体験を分析する。未許諾の保護回避や本家のリソース転用を前提にしない。

### 5.1 観察シート（The Tower）

| 項目 | 記録例 | 検証したいこと |
|---|---|---|
| ゲーム版・OS・Tier | バージョン/端末/難易度 | パッチ差・Tier差を混同しない |
| Wave開始・終了時刻 | 秒単位 | 一波の長さ、敵残存処理 |
| Wave別出現数 | 敵種ごと | スポーン成長曲線 |
| Wave別表示HP・速度 | UI表示から読める範囲 | 難度上昇は指数/多項式/区分関数か |
| 強化購入前後の表示値 | レベル／価格／効果 | 価格曲線、効果の増分と上限 |
| 撃破収益・Wave収益 | 取得前後 | 経済ループと強化タイミング |
| 本体耐久の推移 | Waveごと | 負け方・難度の壁 |
| 到達Waveと周回時間 | 複数試行 | 体験のペース |
| 恒久強化後の再試行 | 比較 | 成長の体感価値 |

**解析手法**：観測値に複数候補曲線（指数、多項式、区分指数、シグモイド等）を当てはめ、残差・外挿誤差を出す。見た目の一致のみで「内部式確定」と主張しない。データがない項目は`UNKNOWN`。

### 5.2 墨脈の試験（本実装で優先）

**固定seed10本 × 方針3種 × 強化戦略3種**（計90ラン、後に拡張）を最低単位にする。

- 初期／強化後の`max_wave`分布、中央値、四分位、95パーセンタイル。
- `effective_dps`、`leak_rate`、`coverage_ratio`、`tip_pressure_distribution`。
- Wave別の流墨収支、強化購入回数とその生存寄与。
- 各敵種の撃破／漏れ／撃破に要した時間。
- 3方針を変更したとき枝の空間分布・根からの距離・Wave結果が変化するか。
- 永続強化ゼロ／ダメージ5Lv／供給5Lv／成長5Lv／採集5Lvの比較。
- 序盤Wave1〜10、中盤10〜30、終盤30〜50それぞれの処理圧。
- `crystals_per_minute`が短時間周回だけを過剰優遇しないか。

### 5.3 最低限のCSV出力カラム

```csv
seed,build,stance,meta_preset,wave,elapsed_s,spawned,killed,leaked,core_hp,run_ink,spent_run_ink,node_count,tip_count,mean_pressure,coverage_ratio,theoretical_dps,effective_dps,damage_taken,crystals_if_dead
```

さらにWave終了時の購入レベル（6項目）と敵種別の撃破・漏れを別テーブルで保持。ログはソート可能にし、フレームレートの違いで増減しないこと。

---

## 6. 数式・実装で必ずテストすること

1. Wave1/10/20/30/50についてHP、スポーン率、期待敵数が上表と一致する。
2. `HP_base(w)`と`spawn_per_sec(w)`は基本的に非減少、スポーン上限は4.8を超えない。
3. `n>=1`。tip上限12、Node上限64、rootにつながらない枝／閉路なし。
4. 同一のQ0・経路長ならtip数増加で`Q_i`が低下する。供給強化で`P_i`は非減少。
5. `P_i`は0.45〜1.65。攻撃速度は4.5/秒を超えない。
6. ボスは10Waveごとに1体、出現重複なし。裂墨の子は再分裂なし。
7. 流墨は負にならない、強化上限で購入不可、補修回数上限5。
8. `core_hp<=0`で1回だけ死亡処理。最大HP以上への回復なし。
9. 1x/2xで同じseed、同じ入力イベントの結果が一致する。
10. Unityの表示品質Lite/Highがゲームイベントログを変えない。
11. 永続強化で統計的な進行改善が確認できる（すべてのseedで必ず増加する必要はない）。
12. 30Hzの長時間シミュレーションでも無限増殖、NaN、メモリ肥大、途中で敵が消失して難易度が下がる異常が起こらない。

### 6.1 開発上の未確定事項：推測のまま隠さない

- Waveごとの敵タイプ出現比率とスポーン時間の分布。
- 成長イベントでの枝候補スコア統合・障害回避の重み。
- `T_growth`への永続成長ボーナスの小数点処理。
- `run_ink_on_kill`の丸め方式、分裂子の資金と成長ゲージの扱い。
- 墨圧`Q0`と永続供給の乗算順、`base_damage`がDoTに含む範囲。
- 2xでのオーバーラップ発射を時間ステップでどの順番に解決するか。
- 敵96の上限に達した場合の負荷保持（単純スポーン取り消し不可）。
- プレイテスト未実施のため5〜8分到達目標は保証しない。

Codexは仕様が曖昧な場合、`ASSUMPTIONS.md`に**最小で可逆的な仮定**を記録してから実装する。勝手に本家パラメータを転記しない。

---

## 7. 実装順序：調査ではなく動くものを作る

### Gate A — 独立したシミュレーション

- WaveDirector / Enemy / InkGraph / InkPressure / Combat / Economy / MetaProgressionを純C#で実装。
- 固定seed10本を周回終了まで実行しCSV化。
- 数式テスト、レベル上限、死亡処理を確認。
- **合格**: 同seed同入力で同ログ、エラー0、数値上限違反0。

### Gate B — 白黒のUnity Vertical Slice

- 生成り背景、墨核、3枝、円形戦場、敵5種、強化ボタン、成長方針3種。
- GameCoordinatorはロジック状態を表示するだけ。MCP/CLIでPlay Mode・Console確認。
- **合格**: 強化と方針による防衛差が実画面でも分かる。

### Gate C — SUMI ENGINE

- 独立PackageでVein / Slash / Bloom / Sealを描画。
- 可変幅・かすれ・紙目、疑似滲みLiteを追加。
- **合格**: 画質差によって勝敗が変わらない。

### Gate D — 長期ループ

- 敗北→墨晶→恒久4強化→再挑戦→保存・復元。
- 3周以上連続でクラッシュ・進行不能なし。
- **合格**: 複数seedで恒久育成による平均到達Wave改善が認められる。

### Gate E — バランス調整

- 10〜100seedのヘッドレス実験でWave別の敵負荷/実効DPS/漏れを分析。
- 初回Wave25〜40（5〜8分）は**目標**として検証し、構造上無理なら仕様を再調整。
- どのビルドでも同じ順序の購入だけが正解になるなら調整。
- **合格**: 3方針・3ビルドに意味のある差が生まれ、同じseedで再現できる。

---

## 8. Codexへ貼る実装指示

> 現行プロジェクトを確認し、この `THE_TOWER_TO_BOKUMYAKU_REVERSE_ENGINEERING_HANDOFF.md` と `BOKUMYAKU_GDD_v0.2_Unity.md`、`SUMI_ENGINE_Technical_Spec_v0.1.md` を読み、既存コードとの差分を監査してください。The Tower側は公開プレイ構造の研究資料であり、内部式が抽出済みという意味ではありません。ゲーム固有のバランスはBOKUMYAKU GDD v0.2の初期仮説を使用し、未知の仕様を`ASSUMPTIONS.md`へ記録してください。まずGate AとしてUnityEngineに依存しないC#シミュレータを実装、Wave/経済/枝供給/固定seedに対する単体テストを行い、10seed×3ビルド×3方針のヘッドレス実験でCSVと統計レポートを作成してください。既存のUnityプロジェクトとsumi-engineを壊さず、次にGate B→C→D→Eを進めてください。Unityが利用可能ならEditor接続・Play Mode・Console確認を行い、実行できなかった操作は明示してください。各Gateの完了条件とコミットを残し、数式テスト・描画非依存性・永続育成ループが成立したら結果を報告してください。The Towerのソース、実物の数値テーブル、UI、アセットは転用しないでください。

---

## 9. 情報源・資料

### この開発で既に作った内部設計（正式な数値基準）

1. `BOKUMYAKU_GDD_v0.2_Unity.md` — Unity移行後のGDD、Wave、経済、敵、枝、テストとゲート。
2. `SUMI_ENGINE_Technical_Spec_v0.1.md` — 独立した毛筆レンダラ、データ契約、描画とゲームロジックの境界。
3. `BOKUMYAKU_GDD_v0.1.md` — 旧Godot用草案。**実装時はv0.2を優先**。

### The Towerの公開情報（2026-10-21参照、コミュニティWiki等）

- Gameplay: https://the-tower-idle-tower-defense.fandom.com/wiki/Gameplay
- Currency: https://the-tower-idle-tower-defense.fandom.com/wiki/Currency
- Workshop Upgrades: https://the-tower-idle-tower-defense.fandom.com/wiki/Workshop_Upgrades
- Enemies: https://the-tower-idle-tower-defense.fandom.com/wiki/Enemies
- Free Upgrades: https://the-tower-idle-tower-defense.fandom.com/wiki/Free_Upgrades
- Perks: https://the-tower-idle-tower-defense.fandom.com/wiki/Perks
- The Tower Wiki / Guide (external): https://the-tower-idle-tower-defense.game-vault.net/wiki/Enemies
- Workshop Guide (2026-10-07更新と記載): https://towerstats.org/guide/workshop/

**利用範囲**: 上記のゲーム構造・公開観察を設計思想として参考にする。固有の著作物、コード、レイアウト、データ資産は複製しない。

---

## 10. 最終判断

『墨脈』がThe Towerから継ぐべき中心は、**1ランの負荷上昇／周回中経済／敗北による恒久成長の三層構造**。その上で差別化の中心は**空間を変える自律枝分岐と、枝の配置・長さ・墨圧分散が戦闘に効くこと**。この2つを黒い線と円だけで成立させてから毛筆品質を高める。見た目だけのThe Towerクローンを作らない。
