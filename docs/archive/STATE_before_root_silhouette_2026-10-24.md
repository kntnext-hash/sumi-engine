# 現状 — 墨脈
更新: 2026-10-24

## 現状
- Unity6000.3.26f1 / URP17.3.0 2D。戦場SpriteRenderer、全UI uGUI+TMP。Brush Lab互換確認済み。
- 初墨/幽墨/朱天の3段位と段位別記録/倍率/解放。二段から吸生種/護輪種。
- rules9 / snapshot3 / campaign4 / 保存v11 / 研究価格3。旧保存保持。Runtime未変更。
- 山水v5、HUD墨枠v6、白和紙カードv7、音8素材＋6武器SEを統合。商品完成は未達。

## 直近の結果
- profile-v27: 開発ビルドのProfiler取得に成功（701frame）。初回3frame不足は別保存。戦闘最大52.61msで500ms停止は非再現。製品版との差があり解消とは判定しない。raw/manifest/解析を保存。
- settled-v26: 再読込を省きホーム5秒後Point開始。Direct最大50.62ms、通常描画495.25ms（FinishFrameRendering467.54ms）。各1回、欠落0。連続リセットだけが原因ではない。両方戦闘/同一プロセス保存復帰合格。
- Windowsの500ms級停止を既定描画でFinishFrameRendering、Direct描画でTexture2D.Apply内まで絞った。製品の描画スレッド設定は保持。GPU/ドライバー等の根本原因は未確定。
- batch-direct-v25（8タイル一括転送）は平均16.81〜17.45ms、最大554.57ms、Apply515.40ms。同程度の停止が残り不採用。6武器/SE/周回/復帰合格、欠落0。WINDOWS_RANK_CANDIDATE.md。
- 候補EditMode5/5合格（4.96秒）。17タイルを3転送、GPU alpha±1/部分更新/端コピー/Clear確認。性能改善は未確認のため製品は5f54b4aへ復元。候補差分/ビルド/証拠を保持。
- 白和紙カード360×640/1080×2400で表示/零墨無効/購入callback/TMP overflow確認。v23でWindows表示も確認済み。
- 初期根35ストローク・枝1本/フレーム。GPU宛先とSpriteの再利用を保持。根CPU最大約42msと密集性能も残件。
- 検証区分: 今回対象Windows＋Profiler取得。全EditMode再実行なし。性能/実機/主観的試聴/全体受入は未合格。
- Android/SDK/NDK/JDK未導入、APK未作成。端末種別を質問中。ゲームのみ縦固定。MOBILE_READINESS.md。
- 詳細履歴: archive/STATE_before_profile_v27_2026-10-24.md。

## 次の一手
1. 同じ開発バイナリをProfiler無効で実行し、開発/計測条件の差を確認。問題再現のProfiler captureは未取得。
2. 参考画像へ敵造形/演出/UIを改善。実機タッチ/性能、音試聴、二段以降収益/分も残件。
3. 別リポジトリ化は計画のみ、実行はユーザー判断待ち。
