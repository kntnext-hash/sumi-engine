# 現状 — 墨脈
更新: 2026-10-24

## 現状
- Unity6000.3.26f1 / URP17.3.0 2D。戦場SpriteRenderer、全UI uGUI+TMP。Brush Lab互換確認済み。
- 初墨/幽墨/朱天の3段位と段位別記録/倍率/解放。二段から吸生種/護輪種。
- rules9 / snapshot3 / campaign4 / 保存v11 / 研究価格3。旧保存保持。Runtime未変更。
- 山水v5、HUD墨枠v6、白和紙カードv7・強化アイコンv8、音8素材＋6武器SEを統合。商品完成は未達。

## 直近の結果
- 上部HUDをWave円/資源欄/段位帯へ分離。360×640/1080×2400で通常値とWave100000/資源int.MaxValueの全文字、停止/再開raycast、fixture復元snapshot一致を確認。header-*.json/png。縁/帯の質感は未完成。
- 墨核を円形HP/残量リングへ変更、修復費用を分離。360×640/1080×2400で満タン/墨不足/上限の無効化、HP3→5/墨25消費、文字を確認。1080で操作パネル17入力も再確認。core-gauge-*.json/png。
- 戦場上の説明/追加強化を操作パネルへ移動。常設は墨核/速度/自動/操作。360×640と1080×2400で各17回のraycast入力、保存/設定/ホーム復帰、TMP表示、snapshot不変を確認。battle-tools-*.json/png。
- 5強化アイコンv8を統合。512px/非readableで取り込み、360×640と1080×2400で5種判別・零墨無効・購入callback・TMP overflowなしを確認。upgrade-icons-*.json/png。戦場の完成評価ではない。
- 攻撃枝を核からの距離で細くし、小枝を親曲線へ正確に接続。1080×2400/seed42/Wave17/22枝の前後画像を確認。描画中snapshot不変。root-silhouette-mature-before/after.png/json。
- Windowsの500ms級停止を既定描画でFinishFrameRendering、Direct描画でTexture2D.Apply内まで絞った。製品の描画スレッド設定は保持。GPU/ドライバー等の根本原因は未確定。
- 初期根35ストローク・枝1本/フレーム。GPU宛先とSpriteの再利用を保持。根CPU最大約42msと密集性能も残件。
- 検証区分: 今回対象Editor画面確認。全EditMode再実行なし。性能/実機/主観的試聴/全体受入は未合格。
- Android/SDK/NDK/JDK未導入、APK未作成。端末種別を質問中。ゲームのみ縦固定。MOBILE_READINESS.md。
- 詳細履歴: archive/STATE_before_header_2026-10-24.md。

## 次の一手
1. 素材室01a16134-e305-7543-954d-eb9c2c8c17c7へ円相枠delivery-v9を依頼。納品後に円形HUDの縁を改善。現状の縁/帯は単調。
2. 参考画像へ敵造形/演出/UIを改善。実機タッチ/性能、音試聴、二段以降収益/分も残件。
3. 別リポジトリ化は計画のみ、実行はユーザー判断待ち。
