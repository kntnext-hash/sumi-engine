# 再利用設計 — SUMI ENGINEの資産性

- package.json / UPMを保ち、BOKUMYAKUプロジェクトに依存しない。
- Pure C#を描画とUnityEngineから分離する。コアデータは座標、時間、筆圧、Preset、Seed、墨量、水分だけで再生できる形にする。
- 同じStrokeLogを将来Web Canvas / WASM / GPUレンダラーで再現するには別Adapterが必要。**Unityで動くこと=Webで動くことではない**。
- 予定の転用例: 墨脈の枝伸長、和風アクションの斬撃軌跡、書道アプリ、動画タイトルアニメーション、デザインSaaS用墨素材の生成。いずれもゲーム/アプリ固有の実装はPackage外へ。
- 品質の収束手順: ①BrushLabの単筆描画 ②紙質3preset ③CPU/GPU性能計測 ④移植先のAdapter作成。最初からフル流体/全部入りを狙わない。

## 現行CPU入力契約
座標は紙面pixel・左下原点、時間は秒。点座標±8192、時間±1e9、格納筆圧0..1、筆半径256以下、1区間4096px以下。非有限値は描画・記録前に拒否。Session v1は点列とseedを保持し、拡散Step時系列は保存しない。完全画像比較ではStep条件も一致させる。
