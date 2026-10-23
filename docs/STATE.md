# 現状 — 墨脈
更新: 2026-10-24

## 現状
- Unity6000.3.26f1 / URP17.3.0 2D。戦場SpriteRenderer、全UI uGUI+TMP。Brush Lab互換確認済み。
- 初墨/幽墨/朱天の3段位、段位別記録/HP/結晶倍率/解放。二段から吸生種/護輪種。
- rules9 / snapshot3 / campaign4 / 保存v11 / 研究価格3。旧保存と進行中rulesを保持。Runtime未変更。
- 縦山水delivery-v5採用。敵本体に紙色の細縁。ホームに周回/記録の目的別構成ボタン。

## 直近の結果
- 初期根も14組へ分散。隔離Playで同フレーム0→1→1、後続14/14＋枝7/7完了、snapshot/全面初期化不変。root-budget.json。今回検証はコンパイル＋対象Playのみ。Windows時間の再計測は未実施。
- 枝描画を1フレーム1本へ分散。隔離Playで同フレーム0→2→2（根＋1枝）、後続で7/7完成。snapshot/全面初期化不変。branch-budget.json。Windowsで枝区間最大15.28ms（前回86.74ms）。条件付き測定で因果確定ではない。
- QA専用区間計測を追加、Windows timing-v13で6武器/自動周回/別プロセス復帰合格。通常起動は計測無効。ビルド警告/エラー0。WINDOWS_RANK_CANDIDATE.mdに原因切分けを記録、性能改善の完了ではない。
- Windows budget-v14: ビルド警告/エラー0。実寸540×960を目視、6武器/自動周回/別プロセス復帰合格。開始2秒とそれ以後を分けて計測。性能未合格。詳細WINDOWS_RANK_CANDIDATE.md。
- 検証区分: 関連EditMode＋隔離Editor Play（軽量）。265/265合格。defeat-notifications-editmode.json。
- 同tickで生成/撃破された分裂子の演出欠落を修正。保存対象外の一時撃破通知を描画へ接続、HP/報酬/乱数は変更なし。親＋子2体で通知3/飛沫3/墨跡部分更新4096px、全面初期化増加なし。split-death-presentation.json。人工配置であり自然遭遇試験ではない。
- 夜Wave10の22敵配置で細縁の分離を目視。軽減ONでも敵22体のみedge=.65、他0。enemy-edge-2-dense/properties。昼夜朱の背景はlandscape-depth-1〜3で目視/停止snapshot不変/シェーダーエラーなし。GPU追加負荷/実機未検証。
- 1080×2400で研究500→740/保存再読込/旧原本と音量保持、目的別構成選択と途中戦闘変更拒否を確認。first-week-research-ui、play-style-ui。
- 初週3seed実コード測定: 初回6.11〜7.01分、初研究3周。1時間/日で一日目50〜57、一週85〜89Wave、研究18回。通常点墨/採墨、4周に1回円相/記録。購入54件中51件が2〜3周、二段未解放。RANK_FIRST_WEEK_BALANCE.md。
- 旧記録: archive/STATE_before_split_vfx_2026-10-24.md。

## 次の一手
1. timing-v13で枝再描画最大86.74ms、保存最大24.76ms。フレーム最大482.52msは未説明。枝は1フレーム1本へ分散済み。budget-v14は開始2秒以降の平均16.66ms/最大39.17ms、開始直後最大486.89ms。初期根も分散済み。次はWindows再計測で初期停止の改善有無を確認。
2. 参考画像へ美術改善（吸収線等）、スマホ実機タッチ/性能、音の聴感、二段以降収益/分。商品完成は未達。
3. 別リポジトリ化は計画のみ、実行はユーザー判断待ち。
