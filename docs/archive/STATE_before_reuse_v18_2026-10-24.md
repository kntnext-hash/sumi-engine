# 現状 — 墨脈
更新: 2026-10-24

## 現状
- Unity6000.3.26f1 / URP17.3.0 2D。戦場SpriteRenderer、全UI uGUI+TMP。Brush Lab互換確認済み。
- 初墨/幽墨/朱天の3段位、段位別記録/HP/結晶倍率/解放。二段から吸生種/護輪種。
- rules9 / snapshot3 / campaign4 / 保存v11 / 研究価格3。旧保存と進行中rulesを保持。Runtime未変更。
- 山水delivery-v5、敵の紙色細縁、周回/記録の構成ボタンを統合。商品完成は未達。

## 直近の結果
- 周回Clearを透明GPUコピーに変更し墨テクスチャ/Spriteを再利用。対象EditMode3/3（GPU全画素消去/再描画）、隔離Playの3再開始でidentity保持・初期化1回。ink-reset-reuse-*。今回軽量検証、Windows時間は未再計測。
- Windows stroke-v17: ビルド警告/エラー0、実寸540×960目視、6武器/自動周回/別プロセス復帰合格。根区間最大46.54ms、フレーム最大536.30msで性能未合格。通常Startとprobeの同フレーム複数Resetを切り分ける。詳細WINDOWS_RANK_CANDIDATE.md。
- AndroidReadinessCheck: Android/SDK/NDK/JDK未導入、APK未作成。ゲーム起動時のみスマホ縦向き固定を追加、コンパイル合格。実機未検証。MOBILE_READINESS.md。端末種別を質問中。公式モジュール一覧取得は長時間無応答で照会プロセスのみ中止。インストール未開始。
- 吸生に墨管6区間＋朱粒3個。1080×2400で目視、移動/軽減静止/撃破消去/リセット返却/snapshot不変を隔離Play確認。drain-flow.json/png。今回コンパイル＋対象Playのみ、実機負荷未検証。
- 初期根35ストローク、通常枝1本/フレームへ分散。隔離Playで同フレーム予算と後続全描画、snapshot/全面初期化不変を確認。root-stroke-budget/branch-budget.json。seed42/900tickで変更前後の全画素墨量ハッシュ一致。root-pigment-before/after.json。細分化後の時間は未計測。
- HUD墨パネルdelivery-v6を統合。360×640戦闘/ホーム目視、TMP overflowなし、Pause/Homeイベント・snapshot保持合格（hud-panel/home）。今回軽量Editor検証。音8素材統合済み、聴感/実機未検証。
- 以前の詳細: archive/STATE_before_root_v16_2026-10-24.md。

## 次の一手
1. 墨テクスチャ再利用後のWindows再計測。通常Start/複数Resetを区別しストレス試験も保持。
2. 敵造形と演出の美術改善、HUDは1080×2400でも目視済み。スマホ実機タッチ/性能、音の聴感、二段以降収益/分も残件。
3. 別リポジトリ化は計画のみ、実行はユーザー判断待ち。
