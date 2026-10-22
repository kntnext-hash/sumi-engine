## 2026-10-22 開発方針（既存記述より優先）
- 開始時はAGENTS.md、docs/STATE.md、TASKS.mdの該当ゲートだけ読む。GDD係数は初期値であり唯一の基準ではない。
- TheTowerSDK (https://github.com/TmRxJD/TheTowerSDK) とTowerstatsの数値を成長の形・伸び率・比率の参考として使用可。表・コード・画像をコピーせず独自係数を決める。初回敗北5〜8分、恒久研究2〜3周に1回。1日目/1週間のWave目標は参照比率と仮定を明記して提案する。候補最大3案、目標内で採用し終了。
- 優先順位は①スマホ縦戦場/UI ②グラフィック/演出 ③音 ④最初の1週間のバランス。10万Waveまでの長期曲線は凍結、既存コードと上限を保持。
- 敵・攻撃・枝・核・撃破をSUMI ENGINEの筆描画で手続き生成する。シンプルな形と掠れ/滲みを優先。ゲーム固有形状と再利用描画ヘルパーを分離し、将来の墨絵ノベルでも使える構造にする。
- ゲーム試作は動く画面と簡単な確認手順で進める。数値調整中は関連EditModeと短い該当シミュレーションのみ。全テスト/画面/独立QA/Windowsビルド/複数seedのフル検証はマイルストーン時のみ。実施区分をSTATEに1行記録。Runtime/エンジン変更は従来の厳格な規約を維持。
- STATEは現状・直近結果・次の一手のみ、目安3KB以内。古い記録はdocs/archiveへ。WORK_LOG/QA_AND_EVIDENCE本文は直近3版のみ。毎回STATEと実際に変更した内容の資料だけ更新する。
- 墨脈別リポジトリへの分割は対象・UPM Git URL参照・移行手順を計画として提示。新リポジトリ作成/移行はユーザー判断まで実行しない。
# AGENTS.md — SUMI ENGINE / Codex 常設開発規約

最初に `AGENTS.md`、`docs/STATE.md`、`docs/TASKS.md` の該当ゲートだけ読む。実リポジトリの現在状態を優先し、古い文書を正本と誤認しない。

## Mission
- リポジトリ: `kntnext-hash/sumi-engine`。再利用可能な Unity 毛筆・墨汁描画パッケージ **SUMI ENGINE** を実用状態まで育てる。
- 初めての利用先: 『墨脈 — BOKUMYAKU』。毛筆エンジンとゲームの経済・敵AI・Wave・戦闘判定を分離する。
- Unity 6.3 LTS 系 / C# / 2D を基準。エディタの正確なパッチ番号は `ProjectSettings/ProjectVersion.txt` と導入済み実行ファイルに合わせて確認・固定する。勝手に Godot へ変更しない。
- `kntnext-hash/nyanstar` は **開発手順・テスト台帳・作業継続方法の参考専用**。非公開リポジトリであり、参照権限がある場合だけ読む。ソース/絵/素材/ゲームシステムをコピーしない。Godotコードの流用も不要。

## Repository constraints
- このリポジトリの **ルートが Unity Package** (`package.json`,`Runtime/`,`Editor/`,`Tests/`)。`UnitySandbox/` は検証用のUnityプロジェクト（必要なら作る）。
- `Runtime/Core/` は UnityEngine に依存しない純 C#。`Runtime/Unity/` は Unity アダプタ。`Editor/` はエディタ限定。
- Coreには敵AI、Wave、ゲーム内墨資源、課金、ゲームUIを入れない。ゲーム専用機能は `UnitySandbox/Assets/BokumyakuPrototype/` で試作し、商用ゲーム本体は将来別リポジトリとする。
- 既存 `Runtime`/`Tests` の v0.1 は **ソース初版・Unity未コンパイル/実機未検証**。存在だけで動作品と判断しない。
- 実測のない速度/画質/モバイル互換/課金等の主張は禁止。ダミー成功記録禁止。
- 商用利用を想定。GPLコード、ライセンス未確認の筆画像、他ゲームのゲームアセットの直接混入禁止。OSSの参考文献と直接取り込みを区別し `docs/OSS_AND_PROVENANCE.md` に記録。
- 秘密鍵・個人情報・トークン・Unityログイン情報・巨大生成キャッシュをコミットしない。

## Autonomous execution contract
1. 各開始時に `git status -sb`, `git log -1`, ソース/テスト/ドキュメント/利用可能なUnity CLIを確認。未コミット変更を保護。無断で `reset --hard` / `clean -fd` しない。
2. `docs/TASKS.md` の最初の未達ゲートから着手。タスクを仕様→実装→テスト→結果記録→コミットの単位で閉じる。ドキュメントだけ増やして完了と言わない。
3. ゲームの順序は冒頭の2026-10-22方針に従う。エンジン担当は従来の描画ゲートを維持。
4. 必要なら subagents を **Core / Rendering / Demo / Independent QA** に分割。ただし同じファイルを並行修正せず、統合担当が最終確認・コミットする。
5. 利用可能なコマンドを実行し、実行できないものは `BLOCKED` と記録。ソース検査と Unity Editor コンパイル、PC画面確認、実機計測を混同しない。
6. 頻繁な確認質問は不要。普通の実装判断は自分で行う。課金/外部サービス契約/リポジトリ公開範囲変更/他リポジトリ変更/ストア申請/重要な設計転換では立ち止まる。
7. 毎回 `docs/STATE.md` と実際に変えた内容に対応する資料だけ更新する。
8. 無限ループする「完成まで永遠に走れ」は禁止。今回のタスク受入条件が満たされた時点、または実行環境の真の阻害要因が出た時点で区切る。独立してできる作業は完了する。
9. コード変更時は小さなコミット。push/PRは接続・権限が使える場合だけ行う。コミットできない場合は差分とコマンドを提示する。

## Definition of Done
- 起動可能な **UnitySandbox Brush Lab**、入力による毛筆描画、PNG出力、複数Seedの再現性、Editor Tests、Unity Consoleのエラーなし、少なくとも手動確認手順、成果物とコミットSHA。
- モバイル実機は実機でテストするまで未合格。墨脈プロトタイプはWave/枝供給/敗北/周回の主要ゲームループがプレイできるまで未合格。

## ローカルUnity操作環境
- 構築と有限リトライ手順は `docs/CODEX_UNITY_SETUP.md`。公式Plugin/CLI/MCPを使い、Editor接続とMCP起動成功を区別する。
- 接続前に `unity status` / `unity pipeline list`、操作前に `unity list` で実コマンドを発見する。対象はUnitySandboxの絶対パスで指定する。
- Scene/PrefabはEditor APIで編集し、既存の未保存Sceneを保護する。操作試験用Assetは `Assets/CodexAutomation/` に隔離する。
- 修正は最大3回。実コンパイル、テストXML、フレーム進行、Console、画面確認を別々に記録する。管理者操作/認証で停止し、合格を捏造しない。
