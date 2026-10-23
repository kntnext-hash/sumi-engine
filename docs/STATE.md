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
- 検証区分: 今回は対象Windowsビルド/実行/画像のみ。全EditMode・実機・主観的試聴は未実施。
- GPUテクスチャ/Spriteの再利用は対象EditMode3/3と隔離Playで検証済み。初期根35ストローク・通常枝1本/フレーム。seed42/900tickの変更前後墨量ハッシュ一致。部分更新を保持。
- 吸生の墨管/朱粒は1080×2400で表示・軽減・撃破/リセット消去確認済み。実機負荷未検証。
- Android/SDK/NDK/JDK未導入、APK未作成。ゲームのみスマホ縦向き固定。端末種別を質問中。公式モジュール照会は無応答で照会プロセスのみ終了、インストール未開始。MOBILE_READINESS.md。
- 以前の詳細: archive/STATE_before_audio_v19_2026-10-24.md。

## 次の一手
1. 開始直後の500ms級フレームの原因切分け、初期根CPU負荷と密集時性能の改善。
2. 参考画像へ敵造形/演出/UIを改善。スマホ実機タッチ/性能、音の聴感、二段以降収益/分も残件。
3. 別リポジトリ化は計画のみ、実行はユーザー判断待ち。
