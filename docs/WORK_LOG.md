# 直近3版の記録（v0.10〜v0.12）
過去全文はarchive/に保管。現行方針はAGENTS/STATEを優先。

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

## 2026-10-22 v0.11 研究価格と成長テンポ
- 基点94606b8/main同期。研究価格のみ変更、戦闘rules6・倍率・上限は維持。購入前Lv0〜10は従来費用、11〜20は丸め済みLv10費用×1.22指数、祖墨20超は新Lv20費用から二次増加。祖墨20→21は20,339→2,952結晶。
- 旧版＋3候補の304自然周回を比較し1.22を固定。Enso Farmingの次購入は9→2周。初期meta0から20周は18/18/18/17へ成長。育成meta20からの試験と初期育成を区別する。
- 固定後seed11〜13の36自然周回で全6武器Farmingの次購入は2〜3周。Recordは3周以内購入なし。製品76周は採用候補の全結果・購入・残高と完全一致。比較は今回で終了、長期経済全体の合格ではない。
- 保存先campaign-v8へ原本保護移行。所持Lv/残高/途中状態を保持、遡及返金なし。研究画面に購入前→購入後倍率を表示。
- 独立検証: 全164価格点・購入境界PASS。旧版との固定meta12組で状態完全一致、10組自然敗北、最大meta Record2組はWave2000観測打切りRunning。Unity EditMode211/211（61.0585128秒）、基礎独立12/12合格。
- Unityドメイン再読込時にDeleting invalid font reference警告1件を観測、コンパイル失敗/Console errorなし。画面/PC最終結果は下段へ追記する。
- 次: 未達の初回尺G-V2-05と育成後の武器・構成差を有限条件で評価。数万Wave自然攻略、実時間放置・モバイル実機、完成美術・音は未達。研究価格の候補探索を無制限に繰り返さない。

### v0.11 Unity画面・Windowsビルド
- Unity211/211、基礎独立12/12、画面50/50合格。祖墨20→21の実ボタン購入で2952を減算し、再読込・次周反映・戦闘中購入禁止を確認。研究画面Lv21/100、次費用3255、攻撃×1.58→×1.65を画像で確認。
- build_78c02500b754 / Windows64成功、95,029,193bytes、14,538ms、error/warning0。Builds/Bokumyaku-v0.11/Bokumyaku.exeへ出力。旧ビルド保持、ツール日時は返却値のまま保存。
- 最終Console error0/warning0、compilationFailed=false。Play停止、Scene dirty=false、背景実行false、QAキー空へ復元。画面試験の意図的な保存競合警告は期待された失敗経路。Engine/Scene/BrushLab/ProjectSettingsに変更なし。

### v0.11 最終受入・再開点
- 実装539af7c。Windows6武器各900tick、checkpoint、自動再出撃、別プロセスからの復帰600tick未来一致に合格。両passed=true/exit0、106.538秒と14.183秒。PlayerログException/Error/Failedなし。隔離セーブのみ使用。
- 起動先Builds/Bokumyaku-v0.11/Bokumyaku.exe（Data/UnityPlayer/MonoBleedingEdge同梱のまま）。証拠bokumyaku-player-v11.json、player-resume-v11.json、player-launch-v11.json、artifact-v11.json。独立検証の製品ソースhashも再照合済み。
- 今回の研究価格・表示・保存移行をPCα機能として受入。Unity211/211・画面50/50・基礎独立12/12。これはシステム全体完成、実時間長期放置、スマホ性能、数万Wave自然攻略の合格ではない。
- 次セッション: G-V2-05初回5〜8分の未達5seedと育成後役割差から、有限の評価条件を決めて実装する。価格3候補比較は終了。STATEの現行要約と本記録、TASKSの最新ゲートを優先する。
## 2026-10-22 v0.12 縦画面試作・方針更新
- 方針/STATE軽量化/初週目標案/分割計画を反映。Runtime/戦闘係数/保存形式は不変。
- 検証区分は限定試作。Unityコンパイルと540×960表示、主要6操作を確認。全テスト/独立QA/Windowsビルドは実施せず。
- 詳細/失敗と復旧/保存先の注意はBOKUMYAKU_PORTRAIT_V12.md。スマホ実機と完成グラフィックは未達。
