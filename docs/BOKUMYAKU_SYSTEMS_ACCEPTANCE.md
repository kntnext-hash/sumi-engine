# 墨脈 — 一人用システム完成チェック

最新は末尾のv0.9長期基盤の検証状況。v0.8までのPCα受入は履歴として保持し、v0.9のUnity/Player最終判定は未確定。初期役割比較G04は既定seed合格を維持し、追加seedでは未達。初回尺G05、育成後の記録構成、長期経済・美術/実機は未合格。

2026-10-22 JST。ユーザーの「システム面完成まで」を受け、40bb883/v0.4から実装を継続。今回の対象はGDDの一人用MVPと既存の武器/放置ビルド案。状態は実検証後に更新する。

|工程|今回の完了条件|開始時|
|---|---|---|
|戦闘|通常/高速/重装/分裂/ボスの5敵、出現・接触・撃破・分裂上限、核修復|3敵、修復なし|
|武器/ビルド|点墨/貫筆/滲墨/払い/円相/共鳴の6系統、6周回強化、3成長方針、手動/2自動投資、3構成保存枠|3武器、構成は起動中のみ|
|経済/目標|固定報酬による短命周回偏重を避けた通常報酬、自己最高の節目報酬は一度だけ、統計と履歴|報酬曲線の役割差が未解決|
|恒久育成|4恒久強化、保存・購入・次周回への反映、3周連続で成立|単一ランの保存のみ確認|
|永続化|戦闘checkpointの同tick復元、操作設定保存、復帰時停止、旧セーブ保護、正常bak、壊れた入力の拒否、報酬二重付与なし|終了時のMeta保存のみ|
|操作フロー|出撃準備→戦闘/停止/修復→中断保存→再起動復帰→結果→恒久強化→再出撃・自動周回|出撃/敗北/再出撃のみ|
|検証|新旧NUnit、独立C#、10seed×3成長×4戦略、武器比較、Unity実操作、Windows実行|v0.4の67NUnit等|

オンライン大会/課金/ストア、実時間研究、オフライン戦闘、Tierは既存GDDの後続項目であり、このチェックの完成に含めない。端末性能/長時間運用/人による面白さ評価/完成美術は別の品質工程。未合格を合格へ読み替えず記録する。

今回のスマホ参考画像「墨絵インクバトルWAVE37.png」は、戦場を大きく取り上部に情報・下部に操作を置く完成美術の方向として保存する。画像内の通貨・Lv・技の数値は実仕様ではない。まず機能を通し、グラフィック工程で戦闘構図を改める。

所有: Core=Simulationと戦闘テスト/数値比較、Persistence=Campaignと保存テスト、Presentation=操作/復帰接続、統合=独立レビュー/Unity/Player/共有台帳/Git。EngineとBrush Labは変更しない。

## 判定

**一人用の主要システムは、PCシステムαとして受入。ゲーム全体の完成ではない。**

|確認|結果|証拠|
|---|---|---|
|戦闘/武器/修復/終端/保存|Unity102/102、独立C#12/12|evidence/bokumyaku-editmode-v5.xml、standalone-v5.txt|
|保存精度/破損検出|6武器の保存後の未来が厳密一致、精度/異常計12検査|evidence/bokumyaku-snapshot-v5.json|
|画面と3周の主要ループ|31/31、研究/履歴/slot/中断復帰/保存失敗再試行|evidence/bokumyaku-play-v5.json|
|縦画面|440×1060で選択/スクロール合格|evidence/bokumyaku-portrait-v5.json|
|Windows|6武器各900tick、実再出撃、別プロセス復帰後600tick一致、両exit0|evidence/bokumyaku-player-v5.json、player-resume-v5.json、player-launch-v5.json|
|ビルド|6000.3.26f1、Windows64、error/warning0|evidence/bokumyaku-build-v5.json、artifact-v5.json|
|数値比較|310条件と310再走一致。役割分担と尺の安定は未合格|evidence/bokumyaku-systems-v5.json|

残るシステム品質ゲートはG-V2-04/05（放置/記録の優位差と5〜8分の安定）、長時間/強制終了/実機。12sector成長、Tier/装備解放/実時間研究/オフライン/オンライン/課金は後続の機能範囲。今回の受入でP3/G5全体を完了へ変更しない。新規アートは作らず既存6素材を維持。参考スマホ画像の完成美術への移行は次工程。

## v0.6 増分判定（履歴）

有償の第7強化「採墨」、供給先行の記録構成、旧ルール継続とv3保存移行をPCαとして受入。

|確認|結果|証拠|
|---|---|---|
|回帰/ルール/移行|124/124 NUnit、独立12/12|editmode-v6.xml、standalone-v6.txt|
|旧実データ継続|旧DLL fixture900tick後/終端一致、新ルール12構成の900tick毎tick JSON一致|balance-v6-legacy-fixture/rules-checks.json|
|画面/保存/周回|37/37、採墨購入/原本保護移行/旧採墨無効/次周新ルール|play-v6.json|
|縦画面|440×1060の選択/スクロール|portrait-v6.jsonとPNG|
|Windows|6武器/各checkpoint/自動再出撃、別プロセス復帰600tick一致、両exit0|player/player-resume/player-launch-v6.json|
|ビルド|95,022,537bytes、error/warning0、旧版保持|build/artifact/console-v6.json|
|G-V2-04役割比較|深度54/60・効率51/60、各武器6/10以上|balance-v6-selection.json|
|G-V2-05初回尺|26/30が5〜8分、未合格|balance-v6-harvest-2.json|

証拠パスはdocs/evidence配下。採墨の最大16倍は長期経済の完成値ではない。次の最初のゲームゲートはG-V2-05、並行課題は恒久育成速度。参考スマホ美術・音・実機・長時間・後続オンライン機能は上記PCα受入に含めない。

## v0.7 増分判定（履歴）

- 基点397b666。3案の有限比較から分岐角±0.65を採用。ボスHPと修復案は不採用。新規rules3、途中rules1/2は旧角度±0.35を保持。
- G04: 初期meta0、60組で記録深度56勝/周回効率55勝、各武器は深度8/10以上・効率7/10以上。G05: seed1..30で28/30、固定後31..50で17/20、合計45/50。25/26/31/40/49は約244秒で未達。追加調整やseed選別なし。
- 旧rules2の80周回測定と20条件の同meta比較を保存。Farming20周で恒久購入54回、Record29回。確定rules3の同固定meta14/13/14/12・20条件では記録1勝2同点7敗、平均−1.4Wave。育成後の役割差は未合格（v0.7連続20周育成の結果ではない）。
- campaign-v4へ原本保護コピー、JSON構造schema3/configv3継続。結果画面は通常結晶/分に初達成を含めず、侵入合計と最多種を表示。死因判定ではない。
- Unity141/141（44.708秒）、純C#12/12、画面41/41、縦440×1060の選択/スクロール合格。実v0.6 DLL由来fixtureの未来/終端一致、旧v0.5互換、rules3の12構成900tick毎tick復帰一致。仮案と本体160条件のtick/Wave/報酬/状態/修復/強化Lv一致。
- 仕様・診断: BOKUMYAKU_PACING_V7.md。証拠: evidence/bokumyaku-*-v7.* と bokumyaku-pacing-v7-*.json。Builds/Bokumyaku-v0.7を新規作成、旧バイナリ/通常セーブ/Scene/描画基盤は保護。
- 次: G05と育成後記録構成。供給条件を維持した記録分岐上限6→9を有限比較する候補（未実装・未測定）。初期G04の退行、同meta役割、初回尺を別々に判定。長期経済/美術/音/実機/長時間/物理入力は未合格。

### v0.7 Windows最終結果
- 6武器各900tick、攻撃/購入/checkpoint復帰、自動再出撃42→43を実Playerで確認。別プロセスで同checkpoint復帰＋600tick未来一致。両passed=true/exit0（105.811秒、14.209秒）。通常セーブは使用しない。PlayerログにException/Error/Failedなし。
- 証拠: evidence/bokumyaku-player-v7.json、bokumyaku-player-resume-v7.json、bokumyaku-player-launch-v7.json、bokumyaku-artifact-v7.json。ビルドサイズ95,024,073bytes。エンジン/Scene/ProjectSettingsにコミット差分なし。

## 2026-10-22 v0.8 記録構成の後半供給（履歴）
- 基点03628c2、main clean/同期。Unity6000.3.26f1/Pipeline0.9.0-exp.1。Core比較/QA永続化/Demo照合/親統合で所有分割し、Engine/Scene/BrushLabは変更なし。
- 最大3案を比較。A枝上限のみは供給条件に届かず不変。B後半供給優先を採用。C滲墨を供給へ置換は初期から悪化し不採用。各480走＋B固定後480走＝1920走（反復基準含む、独立1920seedではない）。
- rules4: Recordの枝Lv6以降、Damage条件が足りれば次枝のSupply>=2*nextまで有償供給を優先、枝上限9。既存の毎秒購入枠/費用/敵/報酬は不変。旧rules1〜3は従来処理。保存先v5、schema3/configv3、旧原本/bak保護。
- 育成Point旧Record比平均+2.6Wave、固定後+2.1。全育成120組53勝66同1敗、Bleed seed18は57→56Wave。対Farming育成役割は未合格。Sweep/Ensoは双方60上限の同着で優位を判定できない。
- 初期G04は既定seed深度56/60・効率55/60を維持、追加seedは42/60・58/60で一般化未達（基準版と同じ）。初回Farming50条件は旧版不変、G05は45/50の未合格を保持。
- Unity157/157、独立12/12、採用仮案と本体480条件＋初回50条件一致。初回NUnit156/157の1件は試験で注入した所持墨とincomeEarnedの不整合、試験準備を修正して再実行157/157。失敗XMLも保持。実v7 DLL fixtureの旧未来/終端は初回から合格。
- 詳細BOKUMYAKU_RECORD_V8.md、evidence/record-v8-*.json。次はG05、上限未到達武器の役割、上限で評価不能な条件の整理。今回の候補を増やして再探索しない。完成美術/音/実機/長時間/物理操作は別工程。

### v0.8 Windows最終結果
- 6武器各900tick・checkpoint・自動再出撃、別プロセスで保存復帰後600tick未来一致。2プロセスともpassed=true/exit0、105.917秒/14.320秒。PlayerログException/Error/Failedなし。
- 証拠はbokumyaku-player-v8.json、player-resume-v8.json、player-launch-v8.json、artifact-v8.json。通常セーブは未使用。描画FPS/モバイル/物理入力/長時間放置の証明ではない。
- 起動先Builds/Bokumyaku-v0.8/Bokumyaku.exe。実装bb9886f、今回の有限比較と機能回帰の増分を受入。G05/育成役割/追加seed初期役割の未達は維持。

## 2026-10-22 v0.9 長期進行基盤（最新・確定結果は末尾）

- 新規rules5は60Wave終了を外し、100000Wave/36000000tickを有限の安全上限とする。旧rules1〜4は60Wave/21600tickを保持する。UIは各周回の上限を表示し、長時間表示は64bit整数で整形する。これは完成コンテンツ10万面や無限進行の実装を意味しない。
- Wave1〜60のHP/出現率/報酬係数/速度係数は旧式と厳密一致。61以降はHPを多項式、報酬係数を対数、速度を上限制にし、整数カウンタの保守上界を確認する。武器・成長コンテンツの追加や役割差の合格宣言は行わない。
- 保存先はcampaign-v6。v5→v4→v3→v2→旧Metaの順で原本保護移行し、途中周回のrulesを維持する。保存互換とUnity実操作の最終結果はQA台帳へ確定後に記録する。
- 独立C#検証: 全100000Waveの4式が有限・単調、Boss HP<=1e12、高速敵速度<=157.5、1〜60旧式厳密一致。整数の保守上界は公開検証限界とint32範囲内。Wave1000/10000/30000/99999/100000×6武器の30合成境界で、900tick以内の自然終端と毎tick JSON復帰後の未来一致を確認。最終Waveの終端後追加tick不変、旧Wave60直前fixtureを新rulesへ切り替えた継続も確認。
- 自然進行の小標本: seed42、固定meta14/13/14/12、6武器×Farming/Recordの12条件をWave1から実行。全12条件が18731〜28468tickで自然敗北、5条件がWave60を越えた。最高はEnso/Recordの79Wave完了・80Wave敗北。最大同時敵37、墨場48、枝30。これは複数seedのバランス合格、恒久育成を含む長期経済、10万Wave自然到達、Unity描画性能の証明ではない。
- 証拠: `evidence/longrun-v9-independent.json`、再実行: `scripts/Test-BokumyakuLongRunV9.ps1`。高Waveは状態注入による境界検証と自然進行を明確に分ける。Unity/Windowsの最終試験数・合格数は末尾の確定記録を参照。
- 次工程: v0.9のUnity/Player/保存回帰を確定し、その後に上限打切りのない複数seed・同metaの役割比較と長期経済を進める。G05初回尺、追加seed初期役割、育成後役割の未達を維持する。美術・音・実機・長時間放置・物理操作は別の品質工程。
### v0.9 Unity受入確定
- 実装SHA dca9fb9。Unity184/184（57.7711037秒）、独立基礎12/12、長期独立検証PASS。Unity初回/2回目の失敗XMLも保持し、最終合格と区別。
- 実画面44/44。旧v8保存原本保持・新周回rules5・七強化・3周・報酬・保存失敗再試行に加え、Wave60越え、注入Wave30000のlive Update進行/保存/復帰一致。highwave-v9.pngで30000/100000と5999:48表示を目視確認。合成入力/注入試験であり物理入力・自然高Wave到達・長時間実測ではない。
- Windows64 build_6788b70521a9成功、95,027,145bytes、29,898ms、error/warning0。出力Builds/Bokumyaku-v0.9/Bokumyaku.exe。ツールの開始/終了時刻は返却値をそのまま保存し、検証時刻とは混同しない。
- Unity現在Console error0/warning0・compilationFailed=false。過去buffer件数を現在エラー数と扱わない。Play停止、Scene dirty=false、背景実行false、QA保存キー解除。
- Engine/BrushLab/Scene/ProjectSettingsの差分なし。旧Measure-BokumyakuSystems/Weaponsは歴史用で現行検証に使用しない。再現手順はTest-BokumyakuLongRunV9.ps1と今回UnityAutomation/PlayerV9スクリプト。

### v0.9 Windows最終受入・再開点
- Windows実行: 6武器各900tick、checkpoint、自動再出撃に合格。別プロセスから復帰し600tick後の未来一致。両passed=true/exit0、106.373秒と14.078秒。PlayerログのException/Error/Failedなし。
- 証拠: bokumyaku-player-v9.json、player-resume-v9.json、player-launch-v9.json、artifact-v9.json。隔離セーブのみ使用。モバイル/長時間実測/描画FPSの合格ではない。
- 起動先Builds/Bokumyaku-v0.9/Bokumyaku.exe（Data/UnityPlayer/MonoBleedingEdge同梱のまま使用）。実装dca9fb9。今回の長期基盤を受入し、システム全体完成とはしない。
- 次: 強化上限と恒久成長を長期向けに拡張。複数seed・同metaで放置周回の収益と記録構成の到達を比較する。現状は最高79Wave完了の小標本、数万Wave攻略バランスは未達。初回尺G05/育成役割/実機/美術の残件も維持。

## 2026-10-22 v0.10 火力の長期育成
- 基点bc04d5b/main clean/同期。Core=有限比較/Simulation、QA=Persistence/移行テスト、Demo=UI/独立有償育成、親=統合/Unity/台帳。Engine/Scene/BrushLabは変更しない。
- 新規rules6は周回筆撃100・恒久祖墨100。他上限/報酬/敵は維持。旧取得範囲の費用・威力を保ち、筆撃30超の指数1.12と二次費用、祖墨20超の二次倍率・費用を採用。旧rules1〜5は従来上限のまま。保存先v7へ原本保護移行。仕様BOKUMYAKU_PROGRESSION_V10.md。
- 候補A(80/1.10)、B(100/1.10)、C(100/1.12)の各12条件を500Wave上限で比較しCを固定。追加4条件は祖墨100/他meta20、PointFは239Wave完了・240Wave自然敗北、PointR/EnsoF/EnsoRは2000Wave観測打切りRunning。固定高metaの試験であり初期から育てて到達した実績ではない。
- 初期/meta20×6武器×2構成×seed1..3=72条件で旧版との敗北時刻/到達/収益一致。EnsoRecord2条件は終端購入配分のみ変化。採用候補と製品76条件のtick/到達/報酬/墨収入・残高/状態/全7Lv一致。候補36走＋追加4走＋旧比較72組、反復を独立seedと数えない。
- 独立C#全101Lv費用/倍率、旧式一致、旧caps超過拒否、9組×900tick JSON未来一致、資源・攻撃上界PASS。最大単発8.13m/場0.507m<1e9、累積保守上界5.16494e16<rules6検証上限6e16（旧1e15維持）。
- 有償育成20周: 初期meta20/20/20/20・結晶0、Enso、seed1..10。Farmingは戦闘時間合計2.3953h、23926結晶、9周目に20339支払い祖墨20→21、残3587。Recordは2.9790h・2586結晶、購入0。初達成を含めない通常結晶のみ。育成済み状態からの試験で、ゼロからの全育成/実時間放置/長期バランス合格ではない。
- Unity200/200（66.4940851秒）初回合格。純C#既存scriptは古い上限20固定で初回11/12、テストを種類別MaxLevel参照へ直して再実行。失敗記録も保持、製品コードの試験回避はなし。
- 次: 恒久研究の費用/購入テンポと武器・構成差を複数seedで調整。今回祖墨20→21でも約2時間相当の戦闘を要し、役割差全武器/数万Wave攻略は未合格。G05/スマホ/長時間/美術/音は別ゲート。

### v0.10 Unity画面・ビルド受入
- 実装ab07f5a、Unity200/200、独立基礎12/12。画面50/50: 旧rules5の上限30・原本保持、祖墨20→21の実ボタン購入、正確な費用20339減算、再読込、次周反映、戦闘中購入禁止を追加。研究画面でLv21/100・倍率1.58・他研究20/20表示を目視確認。
- Windows64 build_549aa28a7436成功、95,028,681bytes、14,556ms、error/warning0。Builds/Bokumyaku-v0.10へ新規出力、旧版保持。ツール日時は返却値のまま保管。
- 現在Console error0/warning0、compilationFailed=false。Play停止/Scene dirty=false/背景実行false/QA保存キー空。Engine/Scene/BrushLab/ProjectSettingsに差分なし。独立検証の全Simulationソースhashを実装後に再照合済み。

### v0.10 Windows最終受入と次の一手
- Windows6武器各900tick、checkpoint、自動再出撃を確認。別プロセスで復帰し600tickの未来一致。両passed=true/exit0、105.742秒/14.409秒。PlayerログException/Error/Failedなし。通常保存を使わずlogs内に隔離。
- bokumyaku-player-v10.json / player-resume-v10.json / player-launch-v10.json / artifact-v10.jsonを保存。実装ab07f5a、起動先Builds/Bokumyaku-v0.10/Bokumyaku.exe。Data/UnityPlayer/MonoBleedingEdgeを同梱のまま使用。
- v0.10の機能受入を完了。ゲーム全体完成ではなく、次は研究費用と成長テンポ、放置周回/記録の武器別比較。祖墨20からの20周実測と最大祖墨100の2000Wave観測を混同しない。数万Wave攻略・長時間/実機性能・美術/音の残件を維持。
