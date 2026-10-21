# 再利用設計 — SUMI ENGINEの資産性

- package.json / UPMを保ち、BOKUMYAKUプロジェクトに依存しない。
- Pure C#を描画とUnityEngineから分離する。コアデータは座標、時間、筆圧、Preset、Seed、墨量、水分だけで再生できる形にする。
- 同じStrokeLogを将来Web Canvas / WASM / GPUレンダラーで再現するには別Adapterが必要。**Unityで動くこと=Webで動くことではない**。
- 予定の転用例: 墨脈の枝伸長、和風アクションの斬撃軌跡、書道アプリ、動画タイトルアニメーション、デザインSaaS用墨素材の生成。いずれもゲーム/アプリ固有の実装はPackage外へ。
- 品質の収束手順: ①BrushLabの単筆描画 ②紙質3preset ③CPU/GPU性能計測 ④移植先のAdapter作成。最初からフル流体/全部入りを狙わない。
