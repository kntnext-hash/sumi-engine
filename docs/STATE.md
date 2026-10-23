# 現状 — 墨脈
更新: 2026-10-24

## 現状
- Unity6000.3.26f1 / URP17.3.0 2D。戦場SpriteRenderer、全ゲームUI uGUI+TMP。Brush Lab互換は確認済み。
- Waveと段位を分離。初墨・一段/幽墨・二段/朱天・三段。HP・通常結晶倍率、段位別最高Wave/初達成、ホーム選択と戦闘HUD/記録を接続。
- rules7 / snapshot schema3 / campaign schema4 / 保存ファイルv9。旧v8は一段へ移行し元データを保持、旧rules1〜6の途中戦闘は維持。研究価格版2、価格500案はstash保護。Runtime未変更。

## 直近の結果
- 検証区分: 関連EditModeと隔離Editor Playの軽量検証。Bokumyaku.Tests 230/230合格（tier-editmode.json）。初回226/230から旧期待値/初敵待機fixtureを修正。
- 1080×2400で段位選択・ロック・二段出撃・保存復帰・旧音設定引継ぎ・文字glyph/overflow合格。tier-ui.json、tier-home.png、tier-battle.pngを確認。解放には人工100波fixtureを使用、実際の到達バランスは未評価。復帰UI確認はtick0、非ゼロtickの同一継続はEditModeで検証。
- 新段位に対するWindowsビルド/実機確認は未実施。従来v8 Playerは6武器/自動周回/別プロセス復帰確認済み。
- 仮倍率は通常結晶1/1.6/2.5、初達成報酬は従来の5波ごと5結晶。高段位の結晶/分が必ず上がるとは未確認。
- 既存描画3分試験は平均16.81ms/最大123.86ms、全面更新0。音8種類のイベント再生確認済み、聴感/実機は未検証。詳細はarchive/STATE_before_tier_integration_2026-10-24.md。

## 次の一手
1. 段位ごとの昼/夜/朱の背景、二段の吸生種/護輪種を実装。TIER_PROGRESSION.md参照。
2. 段位別に初週進行と結晶/分、研究購入頻度を調整。従来の7時間Point50波診断では育成不足、段位追加だけで解消した扱いにしない。
3. 参考画像へ描画を磨く。分裂子の同tick生成/撃破演出欠落、Windows統合再検証、スマホ実機、音の聴感が残る。商品完成は未達。
4. 別リポジトリ化は計画のみ、実行はユーザー判断待ち。
