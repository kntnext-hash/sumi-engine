# SUMI ENGINE → Codex 実装引継ぎマスター v1.0

更新: 2026-10-21 JST。GitHub: https://github.com/kntnext-hash/sumi-engine

## A. 何を作るのか
- **SUMI ENGINE**: 筆跡・筆圧・筆速・筆先の開閉・墨の残量・掠れ・滲み・乾燥・紙質を演算する再利用可能なUnityパッケージ。目的は見た目だけの画像フィルタでなく、**入力経路と状態から毛筆らしい形が生まれる**こと。
- **Brush Lab**: 設定の差を画面で比較する操作可能な検証アプリ。デモが実際に動くことを工程の合格条件にする。
- **BOKUMYAKU / 墨脈**: 筆跡に生命的な枝分岐、敵Wave、迎撃、流墨、周回育成が加わる縦画面Idleゲーム。ゲーム設計資料は `docs/specs/BOKUMYAKU_GDD_v0.2_Unity.md`。エンジンの品質が安定してから縦切りで実装する。
- **非目標**: The Towerの著作物、APK、固有能力値テーブル、アセット、演出そのもののコピー。Noita級の完全な流体/粒子物理の初期実装。初版で課金やストア公開。

## B. 引き継ぐもの、真実の優先順位
1. GitHubの最新コミットとテスト・実コードが正本。必ず `git status` を見る。
2. v0.1 初版コードは元ZIP `SUMI_ENGINE_v0.1_UnityPackage.zip` から取得した。今回の引き継ぎパックではUnity Packageの `package.json`, `Runtime/`, `Editor/`, `Tests/` を**レポジトリルート**へ配置している。
3. `docs/specs/SUMI_ENGINE_Technical_Spec_v0.1.md` は設計案。**未実装のAPIやURP Shaderが書かれている可能性がある**。実装済みと誤認しない。
4. `docs/specs/BOKUMYAKU_GDD_v0.2_Unity.md` はゲーム企画案。具体数値は検証前の初期パラメータ。性能実測ではない。
5. `docs/START_HERE_ORIGINAL_v0.1.md` は旧ZIP向け導入方法で、旧フォルダ階層を前提とする。今はルートUPM方式に変更したので、必ず `docs/INSTALL_WINDOWS.md` を優先する。
6. どの資料に何が書かれていても、Unity Editor未検証の機能を「動作確認済み」とは記録しない。

## C. にゃんスターから取り込むのは「製作手順」
参照リポジトリ: https://github.com/kntnext-hash/nyanstar （アクセス可能な場合のみ。private）
調査済みの参考ファイル：`AGENTS.md`, `docs/development/PRODUCT_FINISH_PROMPT.md`, `docs/development/TEAM_WORKFLOW.md`, `docs/development/WORK_LOG.md`, `docs/STATE.md`, `docs/TASKS.md`, `docs/reviews/PRODUCT_RELEASE_GATES.md`。
転用する運用構造:
- 企画・Core・描画・UI・QAの責務所有、統合担当による最終コミット
- 固定されたデータ契約、変化しない設計の核、反復して動くテスト
- 自動テスト=A、実画面/R、実機/Dを分けた証拠管理
- 進捗状態/作業ログ/不具合/再利用ガイドの毎回更新
- 1回で具体的な受入条件を閉じる短い反復サイクル
**禁止**: private Nyanstarのソース/画材/ゲーム設定をSUMI ENGINEの公開リポジトリへ転載すること。

## D. パッケージ境界
```
sumi-engine/                  # Unity Package: git URLで他ゲームへ再利用
  package.json
  Runtime/Core/              # Pure C#, Unity非依存
  Runtime/Unity/             # MonoBehaviour/Texture2Dアダプタ
  Editor/                    # Editorメニュー/インスペクタ
  Tests/Editor/              # NUnit EditMode tests
  Documentation~/            # パッケージ利用者向けドキュメント
  UnitySandbox/              # ホストUnityプロジェクト（新規作成/運用）
    Assets/BrushLab/         # 動作検証
    Assets/BokumyakuPrototype/ # ゲーム縦切りのみ。Coreへ混入禁止
    Packages/
    ProjectSettings/
  docs/specs/                # GDD/技術仕様
  docs/STATE.md, TASKS.md, WORK_LOG.md, QA_AND_EVIDENCE.md
  scripts/                   # QA補助、セットアップ
```
- UnitySandboxをUPMパッケージ内のソースと二重管理しない。UPM依存はローカル編集が即座に反映される形式で設定するか、Editorがpackage本体を見られるホスト構成を採用して記録。
- UnitySandboxが未生成ならCodexはUnity Hub/CLIを検出して生成。Unity Editorが無い環境では有効なUnityプロジェクト生成を捏造せず、テンプレートと手順のみ用意。
- コアは「ゲーム内墨圧(Q)」と「紙面顔料/水分」を混同しない。ゲームのHPと戦闘計算はエンジン非依存。

## E. 初版で動いていると期待される機能（実検査前の自己申告範囲）
- `InkBrush`: 毛束ごとの顔料の付着・筆圧と速度依存・墨量消費
- `InkPaper`: 2D配列による顔料/水分の拡散・乾燥
- `SumiEngineCore`: Begin/Append/End/Replayのコマンド境界
- `SumiCanvas`: Texture2Dへの出力
- `SumiBrushLab`: 入力とPNGの出力
- `SumiVeinDemo`: 自動枝生成。**敵/Wave/戦闘/研究は未実装**
- `InkCoreTests`: Coreの一部の自動テスト
この一覧はコードの所在を示すだけ。正しくコンパイル/描画/再現できるかは受入試験で確定。

## F. 工学的に守る設計
- 入力: `StrokeCommand` または同等の点列 `{x,y,time,pressure,tilt,seed,presetId}`。保存/リプレイ可能にする。座標と時間の単位をドキュメントで固定。
- 派生描画: 筆幅・先端向き・穂先広がり・掠れ・墨量。将来 GPU版へ置換してもログ形式を維持する。
- 滲み: v0.1 CPU簡易拡散で十分。画質判断を経て、部分更新またはCompute/URPパスを導入する。スマホ未検証のGPU演算を必須にしない。
- パフォーマンス: 描いた筆跡のすべてを毎フレーム物理再計算しない。ActiveWet/Fixed/Baked/Transientを概念的に分離し、確定した筆跡を圧縮・統合できるようにする。
- 描画: 落ち着いた和紙色、墨の濃淡/掠れ、朱の少量アクセント。ネオン黒背景/純幾何学のThe Tower風を避ける。実アセット著作権を台帳管理。
- 公開リポジトリにつき素材もソースも公開範囲を意識する。第三者のクローズド素材の混入は重大バグ。

## G. 開発フェーズと受入
### P0 — 既存v0.1を**本当に起動**
- `package.json`/`.asmdef`/runtime C#を監査して初期コンパイルを通す。
- Unity 6.3 LTS系 UnitySandboxを設定し、`Tools > SUMI Engine`からBrush Lab / 枝生成デモが見える。
- 画像出力、描画安定性、再生ログ、EditModeテストの実測。
- スクリーンショット、Editorバージョン、Consoleエラー、テスト数、結果ファイルを記録。Editorのない環境は「待ち」でもソースの静的検証とセットアップを進める。
### P1 — 質感
- 入り/止め/払い、筆先割れ、毛束追従、墨量減少、掠れの向き、和紙の繊維ノイズ。
- 同じseedで再現、Presetを3系統に固定（乾筆/濃墨/淡墨）。Brush Labの比較画面で違いが明瞭。
### P2 — 性能・移植
- 解像度384x512/720x1280のCPU/メモリ計測。筆跡多重描画に対するコストを測定。
- 差分更新 / 層固定化 / テクスチャ転送頻度制御。GPUは実測で必要と分かってから。
- Unity向けUPMサンプルと README、日本語導入方法、OSS記録を整備。
### P3 — 墨脈ゲームの縦切り
- 1核、最大12末端、敵3種、Wave 1-20、6周回内強化、敗北、リトライ、簡易永続強化。
- **差別化の中心**: 枝が増えるほど供給Qが分散し先端威力が低下。筆幅/火力/分岐のトレードオフ。
- 画面で枝が伸び、敵が迫り、筆撃で倒される。数値の高速シミュレーションと通常速度の実プレイを別に試験。
- ゲームが成立したら独立 `kntnext-hash/bokumyaku` リポジトリへ切り分ける提案をする。ユーザー許可なく新リポジトリ作成しない。

## H. 成長の数学（現時点では**仮説**）
Wave wの例: HP = `16 * 1.072^(w-1) * (1+0.12*floor((w-1)/10))`、spawn/s = `min(4.8,0.50+0.027*w+0.0007*w*w)`。
枝末端 i への供給: `Qi = (Q0/n) * exp(-0.0013*Li)`; `Pi=clamp(sqrt(Qi/3),0.45,1.65)`。
**必須**: GDDの例示テーブルを無検証でハードコードしない。コードから各Wave表を自動生成し、異常インフレや単一ビルド一択を修正する。

## I. ユーザーへのエスカレーションが必要な事柄
- Unity Editorのログイン/ライセンス、実機とUSB、Android SDK/NDK、Apple Developer契約/署名、課金SDK契約
- 有料サービスの新規購入、一般公開/権限変更、新規リポジトリ作成、ゲーム根本仕様の変更
それ以外の実装・テスト・ローカル素材・通常Gitコミット/既存レポpushは可能な限り自律で進める。

## J. 報告形式
- 実装したもの（ファイル/差分）
- 実行したテストと証拠分類 [Static/Unity EditMode/Unity PlayMode/PC Visual/Android/iOS]
- 未実行（理由）と既知の欠陥
- Commit SHAと次の未達ゲート
- 続きの一文 `docs/STATE.md` に必ず残す。
