# 現状 — 墨脈
更新: 2026-10-24

## 現状
- Unity6000.3.26f1 / URP17.3.0 2D。戦場SpriteRenderer、全UI uGUI+TMP。Brush Lab互換確認済み。
- 初墨/幽墨/朱天の3段位、段位別記録/HP/結晶倍率/解放。二段から吸生種/護輪種。
- rules9 / snapshot3 / campaign4 / 保存v11 / 研究価格3。旧保存と進行中rulesを保持。Runtime未変更。
- 山水delivery-v5、HUD墨パネルdelivery-v6、音8素材＋6武器SEを統合。商品完成は未達。

## 直近の結果
- Windows audio-v19: ビルド警告/エラー0。実寸540×960/Wave3を目視、6武器各900tick/自動周回/別プロセス600tick復帰が合格。6種の攻撃SEを実攻撃中のAudioSourceで確認。人間の試聴ではない。
- 平均16.80〜17.43ms、最大543.46ms。Point/Sweep開始直後に500ms級停止が再発、GC差0。前回reuse-v18の最大47.07msだけで停止解消とは判断できない。音追加との因果は未確定。WINDOWS_RANK_CANDIDATE.md、evidence/*audio-v19.*。
- 転送分離計測を追加。隔離Editorで初期根35本/Flush35回、転送API最大2.53ms・根全体最大41.78ms。snapshot不変。ink-flush-timing.json。Windows停止の原因は未確定。
- Windows flush-v20: 6武器/SE/周回/復帰合格。最大504.74msを再現、前後focused=true・GC差0。該当frameの転送1.16ms/根15.70msでは停止を説明できず。ログ512件上限で後半欠落、該当frameは記録済み。
- phase-v21: 6武器/SE/周回/復帰合格。最大521.18msのうちPostLateUpdate500.93ms、Update15.97ms。欠落0。描画後処理内へ局在、個別原因は未特定。診断の復元/上限もEditor合格。
- 検証区分: 今回対象Windows＋診断Editor。性能未合格、実機・主観的試聴未実施。
- GPUテクスチャ/Spriteの再利用は対象EditMode3/3と隔離Playで検証済み。初期根35ストローク・通常枝1本/フレーム。seed42/900tickの変更前後墨量ハッシュ一致。部分更新を保持。
- 吸生の墨管/朱粒は1080×2400で表示・軽減・撃破/リセット消去確認済み。実機負荷未検証。
- Android/SDK/NDK/JDK未導入、APK未作成。ゲームのみスマホ縦向き固定。端末種別を質問中。公式モジュール照会は無応答で照会プロセスのみ終了、インストール未開始。MOBILE_READINESS.md。
- 以前の詳細: archive/STATE_before_audio_v19_2026-10-24.md。

## 次の一手
1. PostLateUpdate子処理を計測し500ms待機箇所を特定。根CPU負荷/密集性能を改善。
2. 白い紙札delivery-v7納品済み、縮小プレビュー確認・Unity未統合。参考画像へ敵造形/演出/UIを改善。スマホ実機タッチ/性能、音の聴感、二段以降収益/分も残件。
3. 別リポジトリ化は計画のみ、実行はユーザー判断待ち。
