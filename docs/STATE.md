# 現状 — 墨脈
更新: 2026-10-24

## 現状
- Unity6000.3.26f1 / URP17.3.0 2D。戦場SpriteRenderer、全UI uGUI+TMP。Brush Lab互換確認済み。
- 初墨/幽墨/朱天の3段位と段位別記録/倍率/解放。二段から吸生種/護輪種。
- rules9 / snapshot3 / campaign4 / 保存v11 / 研究価格3。旧保存保持。Runtime未変更。
- 山水v5、HUD墨枠v6、白和紙カードv7、音8素材＋6武器SEを統合。商品完成は未達。

## 直近の結果
- Windowsの500ms級停止を既定描画でFinishFrameRendering、Direct描画でTexture2D.Apply内まで絞った。製品の描画スレッド設定は保持。GPU/ドライバー等の根本原因は未確定。
- staging-direct-v24（タイル別転送元192枚）は最大561.31ms、Apply517.37ms。直前v23と同程度で改善なし。6武器/SE/周回/復帰合格、欠落0。メモリ増加に見合わず候補不採用。WINDOWS_RANK_CANDIDATE.md。
- 転送元を従来1枚へ戻し、API別計測と連続Flushテストを保持。関連EditMode4/4合格（5.32秒）。GPU alpha±1、部分更新/端コピー/Clear再利用を確認。staging-rejection-editmode.json。
- 白和紙カード360×640/1080×2400で表示/零墨無効/購入callback/TMP overflow確認。v23でWindows表示も確認済み。
- 初期根35ストローク・枝1本/フレーム。GPU宛先とSpriteの再利用を保持。根CPU最大約42msと密集性能も残件。
- 検証区分: 今回対象Windows比較＋関連EditMode。性能/実機/主観的試聴/全体受入は未合格。
- Android/SDK/NDK/JDK未導入、APK未作成。端末種別を質問中。ゲームのみ縦固定。MOBILE_READINESS.md。
- 詳細履歴: archive/STATE_before_staging_v24_2026-10-24.md。

## 次の一手
1. 固定小サイズの転送アトラスにdirty tileをまとめ、Apply回数を減らす候補を検証。宛先部分更新を維持。
2. 参考画像へ敵造形/演出/UIを改善。実機タッチ/性能、音試聴、二段以降収益/分も残件。
3. 別リポジトリ化は計画のみ、実行はユーザー判断待ち。
