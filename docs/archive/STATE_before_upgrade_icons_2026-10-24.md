# 現状 — 墨脈
更新: 2026-10-24

## 現状
- Unity6000.3.26f1 / URP17.3.0 2D。戦場SpriteRenderer、全UI uGUI+TMP。Brush Lab互換確認済み。
- 初墨/幽墨/朱天の3段位と段位別記録/倍率/解放。二段から吸生種/護輪種。
- rules9 / snapshot3 / campaign4 / 保存v11 / 研究価格3。旧保存保持。Runtime未変更。
- 山水v5、HUD墨枠v6、白和紙カードv7、音8素材＋6武器SEを統合。商品完成は未達。

## 直近の結果
- burstへSUMIで濃い中心を追加。人工7種80体/8点墨/88effects、renderer374維持とsnapshot不変を確認。1080×2400前後画像保存。動的密集性能は未検証。
- 点墨へ方向性のある5滴の着弾飛沫を追加。Editor1080×2400で移動/reduced静止表示/128上限/snapshot不変を確認。まだ小さく淡く、迫力と密集時性能は継続。point-splash-check.json/png。
- 攻撃枝を核からの距離で細くし、小枝を親曲線へ正確に接続。1080×2400/seed42/Wave17/22枝の前後画像を確認。描画中snapshot不変。root-silhouette-mature-before/after.png/json。
- 装飾根を非対称曲線と接続分岐へ更新。細すぎた初案は修正。1080×2400/seed42/Wave3前後画像、35stroke/7枝完了と描画中snapshot不変を確認。root-silhouette-*.png/json。
- Windowsの500ms級停止を既定描画でFinishFrameRendering、Direct描画でTexture2D.Apply内まで絞った。製品の描画スレッド設定は保持。GPU/ドライバー等の根本原因は未確定。
- 白和紙カード360×640/1080×2400で表示/零墨無効/購入callback/TMP overflow確認。v23でWindows表示も確認済み。
- 初期根35ストローク・枝1本/フレーム。GPU宛先とSpriteの再利用を保持。根CPU最大約42msと密集性能も残件。
- 検証区分: 今回対象Editor画面確認。全EditMode再実行なし。性能/実機/主観的試聴/全体受入は未合格。
- Android/SDK/NDK/JDK未導入、APK未作成。端末種別を質問中。ゲームのみ縦固定。MOBILE_READINESS.md。
- 詳細履歴: archive/STATE_before_dense_impact_2026-10-24.md。

## 次の一手
1. 素材室01a16134-e305-7543-954d-eb9c2c8c17c7でdelivery-v8（5強化アイコン）制作中。納品を確認し実UIへ統合。待機cursor末尾:23。
2. 参考画像へ敵造形/演出/UIを改善。実機タッチ/性能、音試聴、二段以降収益/分も残件。
3. 別リポジトリ化は計画のみ、実行はユーザー判断待ち。
