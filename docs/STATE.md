# 現状 — 墨脈
更新: 2026-10-24

## 現状
- Unity6000.3.26f1 / URP17.3.0 2D。戦場SpriteRenderer、全UI uGUI+TMP。Brush Lab互換確認済み。
- 初墨/幽墨/朱天の3段位と段位別記録/倍率/解放。二段から吸生種/護輪種。
- rules9 / snapshot3 / campaign4 / 保存v11 / 研究価格3。旧保存保持。Runtime未変更。
- 山水v5、HUD墨枠v6、白和紙カードv7、音8素材＋6武器SEを統合。商品完成は未達。

## 直近の結果
- Windows postlate-v22: 警告/エラー0。6武器/SE/周回/別プロセス復帰合格。平均16.80〜17.39ms、最大544.09ms。Enso開始直後、focused前後true・GC差0・ログ欠落0。
- 停止frameのFinishFrameRendering495.93ms、PostLateUpdate496.05ms、Update15.82ms、InkFlush1.09ms。描画完了処理へ局在したがGPU/描画スレッド/ドライバー原因は未特定。WINDOWS_RANK_CANDIDATE.md。性能未合格。
- QA専用PlayerLoop計測は元の順序を保持。Editor60frameで構造/デリゲート復元、二重Install/Restore、ログ上限/欠落/無効動作を確認。phase-timing-check.json。
- 白和紙カードは360×640/1080×2400で目視。5枚/二重枠なし/零墨無効/購入callback/TMP overflowなしを確認。paper-card-360/1080。通常保存不使用。v22ビルドは差替え前なので新カード未収録。
- 同一v22を単一スレッド描画で比較（ログDirect確認、EXE/DLL一致）。最大559ms、今回はInkFlush522.93msに局在、欠落0。6武器/SE/周回/復帰合格。スレッド無効化でも停止は残る。direct-v22証拠。
- 検証区分: 今回同一Windows比較。実機/主観的試聴/全体受入は未実施。
- GPUテクスチャ再利用/部分更新は対象EditMode3/3、隔離Play確認済み。初期根35ストローク・枝1本/フレーム。根CPU最大約42msも残件。
- Android/SDK/NDK/JDK未導入、APK未作成。端末種別を質問中。ゲームのみ縦固定。MOBILE_READINESS.md。
- 詳細履歴: archive/STATE_before_postlate_v22_2026-10-24.md。

## 次の一手
1. InkFlush内SetPixels32/Apply/CopyTextureを分離し待機箇所を特定。製品のスレッド設定は保持。根CPU/密集性能も改善。
2. 参考画像へ敵造形/演出/UIを改善。白和紙のWindows収録、実機タッチ/性能、音試聴、二段以降収益/分も残件。
3. 別リポジトリ化は計画のみ、実行はユーザー判断待ち。
