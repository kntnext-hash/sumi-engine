# SUMI ENGINE v0.1 — スタートガイド

これは「墨脈」のために制作した **Unity Packageのソースコード初版** です。既存のOSSの概念・構造を研究参考にしていますが、外部ライブラリ・既存ゲーム素材は一切同梱していません。

## 最初の確認（Unity 6 / 6.3）

1. ZIPを展開する。
2. Unity 6の空の2Dプロジェクトを開く。
3. `Window > Package Manager > + > Add package from disk` を選ぶ。
4. 展開した `com.connectpiece.sumi-engine/package.json` を指定する。
5. メニューの `Tools > SUMI Engine > Create Brush Lab in Scene` を選ぶ。
6. ▶ Play → 白いエリアをドラッグして毛筆描画を試す。
7. 別の空のSceneで `Tools > SUMI Engine > Create Branch Growth Demo in Scene` → ▶ Playを押すと、墨脈風の枝が自動で増殖する。

## v0.1 の実装範囲

- **Core:** 筆圧・速度、毛束ごとの付着、掠れ、墨残量、確定乱数
- **Paper:** 水分拡散、顔料拡散、乾燥・定着（物理忠実な流体演算ではない）
- **Unity:** CPUテクスチャ表示、15Hz更新、10Hz拡散、PNG保存
- **Demo:** Brush Lab、枝成長デモ
- **Tests:** インク付着、数値安定性、決定性、範囲外座標、ログの再生

## 未完了・注意

- この作業環境にはUnity Editor/C#コンパイラがなく、**Unity 6上のビルドと画面表示は未検証**。
- Android / iOS のGPU・CPU負荷は未計測。スマートフォン向けには、タイル差分更新かGPU描画への移行が必要になる可能性がある。
- サンプルはマウス操作。モバイルのタッチUI・ペン入力はこれから。
- 枝成長デモは描画の連携テストであり、ゲーム本体のWave・敵AI・コスト演算は実装していない。
- `.myb`ブラシ互換やMoXiのLBM流体演算も、直接の統合はしていない。
- プロジェクト全体の公開ライセンスは未決定。OSSを参照した事実とOSSコードの同梱は別である。

`com.connectpiece.sumi-engine/README.md` に英語版API・仕様があります。
