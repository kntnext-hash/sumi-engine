# AGENTS.md — SUMI ENGINE / Codex 常設開発規約

最初に `START_CODEX.md` → `docs/HANDOFF_CODEX.md` → `docs/STATE.md` → `docs/TASKS.md` → `docs/QA_AND_EVIDENCE.md` の順に読む。実リポジトリの現在状態を優先し、古い文書を正本と誤認しない。

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
3. 順序: 既存コードコンパイルの修正 → Brush Lab の実描画 → 毛先品質 → 性能改善 → 描画を使用する墨脈戦闘の縦切り試作。
4. 必要なら subagents を **Core / Rendering / Demo / Independent QA** に分割。ただし同じファイルを並行修正せず、統合担当が最終確認・コミットする。
5. 利用可能なコマンドを実行し、実行できないものは `BLOCKED` と記録。ソース検査と Unity Editor コンパイル、PC画面確認、実機計測を混同しない。
6. 頻繁な確認質問は不要。普通の実装判断は自分で行う。課金/外部サービス契約/リポジトリ公開範囲変更/他リポジトリ変更/ストア申請/重要な設計転換では立ち止まる。
7. 毎回 `docs/STATE.md`, `docs/TASKS.md`, `docs/WORK_LOG.md`, `docs/QA_AND_EVIDENCE.md` を更新し、次セッションが再開できる状態にする。
8. 無限ループする「完成まで永遠に走れ」は禁止。今回のタスク受入条件が満たされた時点、または実行環境の真の阻害要因が出た時点で区切る。独立してできる作業は完了する。
9. コード変更時は小さなコミット。push/PRは接続・権限が使える場合だけ行う。コミットできない場合は差分とコマンドを提示する。

## Definition of Done
- 起動可能な **UnitySandbox Brush Lab**、入力による毛筆描画、PNG出力、複数Seedの再現性、Editor Tests、Unity Consoleのエラーなし、少なくとも手動確認手順、成果物とコミットSHA。
- モバイル実機は実機でテストするまで未合格。墨脈プロトタイプはWave/枝供給/敗北/周回の主要ゲームループがプレイできるまで未合格。
