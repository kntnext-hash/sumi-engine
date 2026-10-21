# SUMI ENGINE（墨エンジン） — 技術設計書 v0.1

- 作成日: 2026-10-18
- ステータス: **提案仕様・未実装**（パラメータはすべて試作初期値）
- 第一利用先: 墨脈 — BOKUMYAKU
- 推奨環境: Unity 6.3 LTS / URP / C# / ShaderLab-HLSL / Android・iOS
- 目標: 毛筆の軌跡・筆圧・掠れ・墨量・滲み・乾燥を計算し、**ゲーム固有ロジックから独立した描画資産**として再利用する
- 原則: **“毛筆らしい見た目”を最初に作り、完全な水・毛細管現象の物理シミュレーションは後段で評価**

## 0. 最初に固定する責務境界

**SUMI ENGINE** が担当するもの:
- 入力経路（2D点列、時間、擬似筆圧、傾き、墨量、乱数seed）から毛筆形状を生成
- 墨色・濃淡・筆先の収束・掠れ・飛沫・紙質・疑似滲み・乾燥の描画
- 画像出力に必要なストロークログ（再演可能なコマンド列）を保持
- 品質段階（Lite/Standard/High）による演算の切替

**BOKUMYAKU GAME** が担当するもの:
- Wave、敵行動、ダメージ、敵との衝突、墨脈の木グラフ、枝の成長、資源・永続育成
- 墨流（ゲーム内通貨）、枝の墨圧分配、戦闘タイミングなどのゲーム数式
- SUMI ENGINEへ「この座標を、この筆圧で、この画風で、何秒かけて描け」と命令するだけ

**混同しないこと**: ゲーム資源としての「墨圧・墨供給」と、紙面描画の「顔料濃度・水分・染み込み」は別のモデル。前者は戦闘数値、後者はビジュアル。相互にマッピングしてもデータ構造は分ける。

## 1. Unity向けアーキテクチャ

```text
BOKUMYAKU / Unity Game
  ├── Combat + Wave + InkNetwork (純C#ロジック)
  ├── InkGameplayBridge (イベントから毛筆コマンド生成)
  └── @SUMI-ENGINE Unity Package
       ├── Core (UnityEngineに依存しないC#)
       │    ├── BrushDynamics: 筆圧・筆速・筆先・残量
       │    ├── StrokeSampler: 経路補間・一定距離サンプリング
       │    ├── StrokeGenerator: 輪郭・掠れ・濃淡・飛沫のサンプル生成
       │    ├── PaperModel: 紙繊維・吸収特性
       │    └── StrokeLog: seedつき再現可能なストローク列
       ├── UnityAdapter (UnityEngine依存)
       │    ├── BrushPreset (ScriptableObject)
       │    ├── SumiCanvas (MonoBehaviour)
       │    ├── DynamicStrokeMesh / StampDrawer
       │    ├── InkSurface (RenderTexture layer)
       │    └── TextureExporter (PNG)
       ├── Rendering.URP
       │    ├── InkStroke.shader
       │    ├── DryBrush.hlsl
       │    ├── WetBleed.shader [optional]
       │    └── PaperComposite.shader
       ├── Editor (筆プロフィール編集・プレビュー)
       ├── Tests (Coreテスト、描画ゴールデンテスト)
       └── Samples~ (Brush Lab、BOKUMYAKU連携サンプル)
```

- パッケージ内に `package.json`、`Runtime`、`Editor`、`Tests`、`Samples~`、`.asmdef` を用意する。Unity Package Managerのローカル/Gitインストールを前提。
- Pure C# Coreの計算関数は**UnityEngine APIを呼ばない**。Unity側と、将来の別プラットフォーム実装との橋渡しが可能。
- `UnityAdapter` では Unity 6 URP 2D + Unlit素材を初期値にする。複雑なRendererFeatureへの依存は必要になってから導入。
- Unity以外のネイティブアプリ・Webへの「使い回し」は自動的には成立しない。**描画命令のデータ形式・純C#演算**を共通資産とし、Web Canvas/WebGL/WebGPU描画は別実装とする。

## 2. データ契約（エンジンAPI）

### 2.1 入力

```csharp
// 概念API。実装前の契約であり、コンパイル済みコードではない。
public readonly struct BrushPoint {
    public readonly float X, Y;      // World/Canvas座標系は呼び出し元で統一
    public readonly float Time;      // 秒。単調増加
    public readonly float Pressure;  // [0,1]
    public readonly float Tilt;      // [0,1] 任意
    public readonly float Azimuth;   // ラジアン、任意
}

public sealed class StrokeCommand {
    public uint Seed;
    public int BrushPresetId;
    public BrushPoint[] Points;
    public float PigmentLoad;        // [0,1]
    public float WaterLoad;          // [0,1]
    public float Opacity;            // [0,1]
    public bool PersistOnPaper;
}

public interface IInkStrokeSink {
    void BeginStroke(StrokeCommand command);
    void AppendPoint(BrushPoint point);
    void EndStroke();
}
```

ストローク入力は人間のペン操作だけでなく、**敵撃破イベント、枝伸長AI、3次Bézierカーブ、パーティクル軌道**からも供給できるようにする。

### 2.2 出力

- `StrokeGeometry`: 中心線、接線、法線、左右エッジ、UV、頂点濃度、乾燥/掠れマスクのパラメータ
- `InkDepositEvent`: `{position, radius, pigment, water, seed}`（湿潤レイヤーへの入力）
- `StrokeLog`: 再描画可能な `commands.json` と brush preset version。解像度変更、保存、PNG出力に利用。
- 描画レイヤー: `ActiveWet`, `ActiveDry`, `BakedInk`, `Paper`, `Effects`（描画順を明示）。

## 3. 毛筆らしさを生む5つの独立変数

| 項目 | パラメータ | 何に効くか |
|---|---|---|
| 筆圧 | `pressure` | 線幅・筆先接地面・濃さ・終筆 |
| 筆速 | `speed` | 細り・揺れ・墨消費・掠れ |
| 墨量 | `pigment_load` | 黒さ・掠れの出現・再筆の回復 |
| 水分 | `water_load` | 滲み半径・境界の柔らかさ・乾燥時間 |
| 紙質 | `roughness/fiber/absorption` | エッジのガタつき・繊維方向への滲み |

**擬似筆圧**: ゲームから命令するときは入力装置の筆圧がないため、速度・曲率・攻撃種別から合成する。

初期設計例（物理実測ではない）:

```text
speed = distance(P[k], P[k-1]) / max(dt, epsilon)
curvature = changeInHeadingPerLength
pressure_target = clamp(p_base - k_speed*speed + k_curve*curvature, 0.05, 1)
pressure = lerp(pressure_prev, pressure_target, 1 - exp(-dt/tau))
width = w_min + (w_max - w_min) * pressure^0.75
```

`pressure` は0〜1。`p_base`・係数はPresetに保存。開始・終了は別のtaper曲線を乗算して「入り・払い・止め」を出し分ける。上式だけで速度の速い文字を一律に細くしすぎないよう、`BrushStyle`ごとに係数を調整する。

**ストロークの形状**:
- 点列→Centripetal Catmull-RomまたはBézierへ補間
- 補間後はカーブの弧長近似で一定距離にリサンプリング
- 各点の接線・法線から左右の輪郭を計算しリボンMeshを作る
- 筆先エッジに低周波ノイズ、筆跡内部に高周波の繊維マスクを適用
- 乱数は `stroke.Seed` とサンプル番号由来の決定的ハッシュに固定
- ジオメトリ密度は画面ピクセル幅とズームから決める。**常に大量の点を発生させない**

## 4. 墨の濃淡・掠れ

### 4.1 顔料堆積の描画モデル

`d`を正規化顔料量とした初期の視覚モデル:

```text
alpha = 1 - exp(-k_density * d)
```

- 複数の筆跡が重なると濃くなるが、alphaが1を超えない。
- 墨色は和紙と単純な加算合成にしない。Premultiplied Alphaの合成または透過率モデルで、白・黒の視認性を保つ。
- 墨が少ない領域に紙目マスクを強く適用して、線の内部に白い掠れをつくる。

掠れの初期判定:

```text
scarcity = saturate(1 - pigment_load)
fray    = scarcity * speed_factor * paper_grain_noise
ink_coverage = clamp(base_coverage - fray, 0, 1)
```

単なる全体ランダムノイズではなく、**筆の移動方向とほぼ平行な微細な繊維の筋**を描画すると、安価なエフェクトでも毛筆らしい。

### 4.2 4種類のBrush Preset（最初の実装）

| Preset | 視覚・用途 | 特徴 |
|---|---|---|
| `Bokumyaku_Vein` | 細い枝の成長 | 先端細、連続性、低飛沫 |
| `Bokumyaku_Slash` | 攻撃の払い | 速度変化、高掠れ、短い尾 |
| `Bokumyaku_Bloom` | 着弾の滲み | 高水分、ソフトエッジ |
| `Bokumyaku_Seal` | ボス撃破・朱印 | 低水分、重い圧、太い止め |

PresetはScriptableObjectで編集できるようにし、JSONへexport/importできる項目だけをEngine Coreの永続フォーマットに使う。

## 5. 滲み・乾燥の計算（3段階）

### Lite: 初期MVP — エフェクトベース
- 滲みは薄いマスクを2〜3回重ね、エッジを時間で緩く膨張させる。
- 水分・顔料量の2変数から半径とalphaを決めるだけ。全面ピクセル計算なし。
- 1ストロークあたりエフェクトの発生数・寿命を制限する。

### Standard: 次段階 — 低解像度Texture上の反応拡散風モデル

2Dテクスチャセル（顔料 `M`、水分 `W`、定着顔料 `A`、紙の吸収係数 `P`）を持つ。下記は**見た目のための近似**であり、流体力学を解いたものではない。

```text
D_eff = D_ink * saturate(W / (W + W_half))
M_new = M + dt*D_eff*Laplacian(M) - dt*k_bind*M*W
W_new = max(0, W + dt*D_water*Laplacian(W) - dt*(k_absorb*P + k_evap)*W)
A_new = A + dt*k_bind*M*W
```

- `Laplacian`は4近傍差分。実装時は`M`・`W`を各ステップで互いに**前ステップのテクスチャから**読み、新しいターゲットへ書き込む（ping-pong）。
- 非負条件、数値安定性、顔料の質量収支をテストする。Mが負になる場合はステップ分割／係数制限を優先。
- 紙繊維方向の異方性拡散は次の段階で追加。MVPではやらない。
- 512×512程度の低解像度RTを開始値に、毎フレームではなく**約10Hz**で更新する。実機プロファイルに応じて変える。
- 乾燥したストローク領域は`BakedInk`に統合し、シミュレーションから外す。

### High: 必要になったら
- 紙繊維方向の異方性拡散
- ブラシ穂先の複数毛束シミュレーション（完全な弾性毛束までは求めない）
- 乾燥境界に顔料が集まるcoffee-ring風表現
- 湿った線同士の混色・再溶解

**原則**: High品質は製品体験上の必要性が測れた後に作る。スマホの通常バトルではLiteまたはStandardで十分な可能性がある。

## 6. GPU・CPU・モバイル戦略

1. CPU（純C#）: 筆の経路、シミュレーションseed、筆圧、輪郭メッシュ、ゲーム内墨脈木グラフ。
2. GPU（URP Unlit HLSL/Shader Graph）: 墨の濃淡・紙目・エッジ掠れ・合成。
3. Standardの滲みはFragment shaderのping-pong RenderTextureで試す。最初からComputeShader必須にはしない。
4. より高度な演算は`SystemInfo.supportsComputeShaders`および端末検証を前提とした任意機能にする。
5. 履歴の古い筆跡をバッチ化／RTへベイクし、持続的なゲームプレイでDraw Callとメモリが増え続けないようにする。
6. シェーダーやRendererFeatureはUnityバージョンで破壊的に変わり得るため、URP依存コードはAdapterに閉じ込め、Coreには含めない。

**当面の品質予算（仮目標）**:
- 試作端末: Android 1機種 + iOS 1機種で検証
- 描画: 60fpsを目標、ロジック: 固定30Hz
- Node/Enemy: 元GDDのノード64・先端12・敵96を初期上限として継承
- Standard wet canvas: 512×512起点、約10Hz
- 全画面の高解像度乾燥・拡散毎フレーム処理はしない
- RenderTextureやGPUバッファは使い回し、頻繁なallocateを避ける
- 画面やリザルトでPNGを出す場合のみ明示的な描画待ち・非同期Readback対応を評価

## 7. BOKUMYAKU連携イベント

```text
InkNodeGrowth(parent, child, curve, ink_pressure)
  -> InkGameplayBridge
  -> StrokeCommand(style=Vein, pressure=map(ink_pressure))
  -> SumiCanvas.Draw(command)

InkTipAttack(tip, target, damage, speed)
  -> StrokeCommand(style=Slash, width=map(damage), duration=map(speed))

EnemyDefeated(position, bleed_level)
  -> Stamp / InkDepositEvent(style=Bloom)

BossDefeated(position)
  -> StrokeCommand(style=Seal, inkColor=vermillion)
```

- **当たり判定は既存のゲームロジックが決定**する。見た目の輪郭が揺れても、戦闘結果は変わらない。
- 経路の位置・枝ごとの乱数seedは固定で保存。画質変更してもWave・ダメージ・勝敗に影響しない。
- レンダラ遅延時は描画イベントを統合／間引き可能にする。ゲームイベントを削除して難易度が変わることは許さない。

## 8. Unity Packageフォルダー構成

```text
Packages/com.bokumyaku.sumi/
  package.json
  README.md
  CHANGELOG.md
  Runtime/
    Sumi.Core.asmdef
    Core/BrushPoint.cs
    Core/StrokeCommand.cs
    Core/BrushDynamics.cs
    Core/StrokeSampler.cs
    Core/StrokeGenerator.cs
    Core/StrokeLog.cs
    Unity/SumiCanvas.cs
    Unity/SumiBrushPreset.cs
    Unity/SumiTextureExporter.cs
    Rendering/InkStroke.shader
    Rendering/DryBrush.hlsl
    Rendering/PaperComposite.shader
    Rendering/WetBleed.shader
  Editor/
    Sumi.Editor.asmdef
    SumiBrushLabWindow.cs
  Tests/
    Runtime/
    Editor/
  Samples~/
    BrushLab/
    BokumyakuBridge/
```

**テスト用シーン Brush Lab**:
- 筆型選択（Vein / Slash / Bloom / Seal）
- スライダー: `Pressure`, `Speed`, `Pigment`, `Water`, `Paper Roughness`
- `Straight / Arc / Circle / Zigzag / Handdrawn`デモ軌跡ボタン
- `Same Seed` と `Different Seed` ボタン
- Lite / Standard表示切替、フレームタイム表示、PNG保存
- 2つの描画結果の比較（GPU差異許容範囲を定義）

## 9. ゲート方式の開発と合格条件

### Gate E0 — 独立した描画命令
- Pure CoreでPoint列 → 弧長サンプリング → 不均一幅の輪郭データを生成
- seed一定でストローク幾何データが一致
- Brush Labに白紙と黒い一画を表示（まだ滲みなし）

### Gate E1 — 毛筆特有の表情
- 始筆・中筆・終筆の形態が分かる
- 筆圧スライダーで幅が明確に変わる
- 低墨量で移動方向に沿う掠れが現れる
- 紙質を変えるとエッジ・内部マスクが変化
- 連続100ストロークでメモリが継続増大しない

### Gate E2 — 滲み・乾燥
- Liteの疑似滲みが動作
- Standardは別オプション。実機で過剰負荷がない場合のみ採用
- 色合成は白紙上で黒が濃く、繰り返し筆跡が薄くならない
- 表示結果の保存と再描画（ログ）を確認

### Gate G0 — BOKUMYAKUへ接続
- 既存GDDのWave・敵・木グラフ・戦闘を純C#で実装
- `InkGameplayBridge`経由で枝伸長/攻撃/撃破を描画
- ゲームロジックのseed再現性・恒久強化・セーブを確認
- 描画品質を変えても戦闘ログが一致

### Gate G1 — スマホ動作検証
- Android/iOSの実機上でパフォーマンス計測
- 画面回転、安全領域、発熱、長時間セッションで問題がない
- 墨線が多くても敵の識別・危険方向の視認性を保持
- 60fpsに届かない場合、最初にwet layer更新頻度と描画密度を下げる

## 10. 外部サービスへの転用方針

| ターゲット | そのまま再利用 | 別実装が必要 |
|---|---|---|
| Unity製ゲーム | Unity Package全体 | ほぼ不要（シーンアダプターは別） |
| Unity製描画アプリ | CoreとUnity描画 | ペンUI・レイヤーUI・保存UI |
| PC向けアート制作ツール | Core + PNG Exporter | UI・編集機能 |
| Web SaaS（例：POP/ポスター生成） | Brush Profile JSON、stroke log、生成データ仕様 | Web描画、サーバーレンダラ、API・フォント処理 |
| 動画・モーショングラフィック | StrokeLog再生 | 録画・動画エクスポート |

**初期の資産化戦略**: Unity Packageを独立リポジトリにし、BOKUMYAKU側はGit/ローカルPackage参照。描画命令とSeed・Preset定義をバージョン管理する。ゲームとエンジンのライセンス・公開範囲はそれぞれ個別に決定する。

## 11. 作らないもの（Scope外）

- 書道家の手元や実筆跡を完全物理再現すること
- GPU ComputeShaderしか動かない設計
- 墨流のゲーム数式と湿潤顔料セルを同一クラスにすること
- 初期版で本格的な紙の毛細管モデル、3D筆毛シミュレーション、オンライン同期を導入すること
- 先に大量のアセットを制作し、見た目の仮説を検証しないこと

## 12. Codexへの実装依頼（コピー可能）

> Unity 6.3 LTS / URP / C# の新規プロジェクトで、独立再利用できる毛筆描画パッケージ `Packages/com.bokumyaku.sumi` を作成してください。添付 `SUMI_ENGINE_Technical_Spec_v0.1.md` と `BOKUMYAKU_GDD_v0.2_Unity.md` を基準とし、Gate E0 → E1 → E2 → G0 → G1の順で進めてください。まずPure C# CoreのStrokeCommand、Seed固定サンプリング、筆圧に応じて幅が変化するストロークジオメトリ、紙目付きのURP Unlit描画、Brush Labを実装してください。見た目のための水分・顔料演算とゲーム通貨や攻撃力の墨圧を混同しないでください。初期版の滲みは軽量な疑似表現とし、Standardの反応拡散は実機プロファイル後に導入してください。BOKUMYAKUとの接続はGame→InkGameplayBridge→SUMI ENGINEの一方向依存とし、戦闘結果を描画に依存させないでください。既存のGodotコードを自動生成せず、C#の新規実装にしてください。各Gateでコンパイル、実機／Editor確認、テスト、性能計測、README更新を行い、完了条件を満たすまで次へ進めないでください。The Towerのゲームコード、アセット、数値テーブル、画面配置は使用しないでください。ストア公開・課金・クラウドデプロイは指示があるまで行わないでください。

## 参考（Unity公式）

- Unity 6.3 LTS: https://unity.com/releases/unity-6
- URP 2D: https://docs.unity3d.com/ja/current/Manual/2d-urp-landing.html
- Shader Graph: https://docs.unity3d.com/jp/current/Manual/shader-graph.html
- Custom packages: https://docs.unity3d.com/ja/current/Manual/CustomPackages.html
- Package layout: https://docs.unity3d.com/ja/current/Manual/cus-layout.html
- URP RenderGraph blit: https://docs.unity3d.com/ja/current/Manual/urp/render-graph-blit.html
- Multi-platform Compute Shader constraints: https://docs.unity3d.com/ja/current/Manual/class-ComputeShader-crossplatform.html
