# 戦闘描画刷新 2026-10-23

## 実装
Unity 6000.3.26f1 / URP 17.3.0 の2D RendererをGraphicsSettingsと全Qualityへ設定。戦場はSpriteRenderer、戦闘表示はuGUI/TMPへ移行。既存のホーム・研究・記録はIMGUI。uGUIを選んだ理由は、スプライトの筆枠と再利用するボタン・数値・パネルを同じCanvasに組みやすいため。

戦場はsafe areaの幅100%・高さ68%。従来は30Hzの現在位置を直接描いていた。現在は前/current tickの位置を描画側で補間する。敵5種の墨シルエット、被弾の滲み/白、消失、6武器の軌跡・範囲、45msの表示ヒットストップ（連打制限あり）を追加。演出軽減は揺れとフラッシュを停止する。シミュレーションの時間を止めない。

SUMIで枝・撃破跡を描き、32pxタイル単位でCPU変換とGPU CopyTextureを行う。通常描画で全面Applyを行わない。初期化と新周回のClearは全面初期化。Runtime本体・シミュレーション・保存は未変更。

## 検証
- Brush Lab前後の表示を確認。出力PNGのSHA256は両方 `uL2m0kMyQHHoRhb84P0xY8EfG6nYvEssSL+8T+kMWZ4=`。evidence/brushlab-urp-{before,after}*。
- シミュレーション等211件 + dirty tileのGPU読戻し2件、213/213合格。evidence/render-editmode-tests.json。
- uGUIの速度・停止・自動・演出軽減・保存・ホームのraycast操作とviewport/EventSystem、8/8合格。evidence/render-ugui-controls.json。
- 前後画像はseed42、点墨/Farming、6000tickのWave17。前後の3分計測はevidence/render-{before,after}-timing.json。Editor/360×640/60fps目標であり、スマホ性能の保証ではない。
- 確認手順: Bokumyakuシーンで開始し、速度/停止/保存を操作。撃破跡の蓄積と枝追加、演出軽減を切り替える。Brush Labでは筆描画後PNG出力を確認。

## 参考画像へ近づける次工程
2026-10-23のユーザー添付画像を美術目標とする。現時点では参考の品質に未達。太くうねる広い樹形、赤い敵核と金の味方核、厚みのある紙札・筆枠、敵の判別しやすい輪郭を順に改善する。敵数や資源値を見た目のために改変しない。参考画像自体は素材として同梱しない。

## 残る制約
初回シェーダー準備の待機、実機のCopyTexture対応・日本語フォントは未確認。撃破跡は周回内の表示で保存データに追加しない。同tickで発生・消失した敵は既存シミュレーションにイベントがなく検出できない場合がある。メニューの移行、完成美術、スマホ実機QAは次工程。研究価格変更はstashに退避して保持。

### 最終計測結果
前: 平均16.8316ms / 最大112.8685ms。後: 平均16.7216ms / 最大53.0458ms。後は304撃破/304跡、全面再構築0、サンプル内全面初期化0。初回準備とEditor操作が混在した最大393msの試行は比較不採用。最大53msは残るため完全な無停止とは扱わない。

### 枝の美術第一段階
幹半径を14→4.5の遠近勾配にし、枝方向に垂直な湾曲と先細りを追加。装飾小枝を3→5へ増やした。seed42/Wave17のbranch-art-wave17.pngで確認、全面再構築0。攻撃位置/敵数/経済は未変更。描画移行時の性能値はこの変更後の再測定ではない。参考の広い樹形には未達。

### 紙札のコントラスト
無効時も和紙を暗くしすぎず、筆記号を拡大、黒い価格欄と明るい数値を分離。BattleCardCheck.Prepare→次フレーム後Runで隔離試験残高50000/0を使用し、5札購入と不足拒否6/6。実機タップの代用ではない。card-art-wave17.pngを確認。価格欄の文字サイズ、筆縁と発光は継続。

### 金の核と筆縁
核に白い中心/二重の金輪、枝先に金輪を追加。札は既存円相から9-slice筆縁と金の外輪を構成し価格を17ptへ。Wave17画面を更新、追加装飾のraycast遮蔽なし（購入/不足6項目合格）。コンパイル/画面/操作の限定検証。画面構図の余白と参考の樹形の差は残る。

### 紙面へ張る根
ResetInk時に一度だけ7本の先細りの根をSUMI描画。攻撃枝/判定/経済への追加なし、装飾根には金の攻撃点なし。初稿の紙座標上下の誤りを実画面で修正し、下方向へ配置。Wave17画像を更新、全面再構築0。性能再計測ではない。

### 敵のシルエット改良
通常/重装/ボスの円形輪郭に放射状の墨ストローク、突進型に内部の筆塗りを追加。分裂型の双核を保持。Editorで5種のマスクが空/全面不透明でないことを確認し一覧画像を保存。Wave17でも確認。敵AI/HP/数/保存は未変更。赤い核の強調と密集時の識別は継続。

### 敵核と突進方向
敵核を32px共有スプライトで白中心/赤い滲みに変更し破棄管理を追加。突進型の描画回転を投影後の移動方向に合わせる。Wave17の1体で向き一致、enemy-facing.json/pngを記録。最初の判定はヒットストップ静止を誤検出、QAで静止を除外して再確認。AI/経済/保存は未変更。

### 攻撃の金光
発射点/着弾点と直線系3武器へ短い金の光を追加。既存の墨の形を保持、同時128エフェクト上限内。演出軽減時は生成せず、既存閃光も即非表示。6武器の短時間Play記録を更新し貫筆/円相画面を目視。ReducedFlashCheckで6閃光すべて非表示を確認。実機性能や全武器の美術完成の合格ではない。

### 修復操作の移行漏れ修正
旧詳細画面のTryRepair操作が新HUDから欠けていたため、HP欄を修復ボタンへ変更。既存費用/上限/保存処理を使用。隔離試験データで4条件（購入、満タン、墨不足、5回上限）をraycast経由で確認。repair-hud-check.json、repair-hud.png。実機タッチと保存再読込の追加検証ではない。

### 独立QAとフォント修正
独立read-only QAがOSフォント不在端末の起動例外と滲み/採墨導線不足を検出。前者を同梱Noto Serif CJK JP静的TMPへ修正。28ラベルの同梱資産参照/全表示文字収録を確認。OSフォント検索なし。実機起動は未検証。後者は次工程。実行時targetFrameRate60を設定し、Play終了時の元値-1復元を確認。60fps達成の保証ではない。

### ユーザー敵案の反映
分裂種の双輪を3子核の輪郭へ、ボスの星形を9本の分岐触手へ変更。提供画像は形状参考のみで切抜き/素材同梱なし。画像内の行動案は新ルールとして採用せず既存シミュレーション維持。EnemySilhouetteCheckで5種生成/透明領域を確認、一覧画像更新。赤い副核と実戦の判読性は残件。

### 副核と追加強化
分裂種/核樹に3副核を重ね、死亡/Resetで本体と共にプール返却。人工配置3体9個を確認（自然出現/死亡反復の試験ではない）。右上に滲み/採墨パネル追加。2購入と不足拒否、静的TMP31ラベル確認。初回同フレームraycastは未描画状態のため不採用。次フレーム確認で合格。同seed再出撃の閉じ忘れを周回参照比較で修正、上限/開閉の追加検証は残件。

### 追加強化と副核の残件確認
ExtraUpgradeEdgeCheckで描画フレームごとにraycast操作。上限2種/閉じる/再開/同seed再出撃/Homeの6項目合格。EnemyPoolCheckで副核付き人工配置をReset→Render30回、Renderer79→79。長時間撃破/メモリプロファイルの代替ではない。Play停止・一時設定復元済み。

### 実ダメージ表示
前後tickの敵HP差を表示し、過剰ダメージは残HPで制限。同一敵への連続命中を0.65秒内で集約、24表示まで再利用。演出軽減では上昇させない。隔離Playで4表示の合計4.514327253895011が実HP減少と一致、damage-number-check.json / damage-numbers.png。上限到達/再利用の負荷検証は未実施。シミュレーション/経済/保存は未変更。
敵案の狙眼・吸生・護輪は将来候補。現在の5種の描画改善を優先し、新AIは今回追加しない。

### ダメージ文字の上限と再利用
DamagePoolCheckで人工100体×30サイクル、各2+3ダメージの表示要求。24個上限、表示値5への集約、Reset後全非表示、同じ24個の再利用を確認。damage-pool-check.json。自然戦闘の長時間負荷や実機性能を示す試験ではない。

### 根の分岐と筆圧
装飾根7本の半径13→18、圧力を二次減衰へ変更。各3本、計21本の側根をSUMIで追加。Reset時のみ。隔離Play seed42/6000tick/Wave17の画面を確認、全面再構築0、dirty upload313。card-art-wave17.png更新。攻撃判定/枝ノード/経済は変更なし。墨の面の濃さは依然弱く改善継続。性能計測ではない。

### 根の濃い芯
Fiberの広い掠れ層を残し、同じ曲線へ半径10のPool層を追加。細線の束だった根を黒い帯へ改善。SUMIの既存描画機能のみ使用、Runtime変更なし。seed42/6000tick/Wave17で画像確認、dirty upload313/全面再構築0。性能再計測ではない。

### スマホ操作域の拡大
下部HUD24→30%、戦場68→62%へ調整（幅100%、高さ60%以上を維持）。360×640で操作列44.7px高、札68.7×104.2px。主要10ボタンの中心raycast到達と画面を確認。hud-size-check.json/png。初回QAはRectのJSON循環参照で出力失敗、数値出力へ直して再実施。実機タッチやノッチ端末の検証ではない。

### Windowsビルドと旧検証の移行
899b702をWindowsビルド、成功。旧probeは旧SumiCanvasのpigment=0で不合格（render-player-legacy-probe.json）。GPUテクスチャ読み戻しを明示QAのみ追加したv2ビルドは135720813bytes/error0/warning0。6武器/保存再読込/自動再出撃/墨領域は通過。全体は素材数期待6/実際7で不合格。LoadArt呼出7件を確認し期待7へ修正、再ビルド検証は残す。render-player-v2.jsonを失敗のまま保存。Unity生成のURP設定も保持。

### v3 Player再検証
素材7個判定版をビルド。135720813bytes/error0/warning1。6武器900tick、攻撃/購入/保存再読込/GPU墨領域、自動再出撃が全体passed=true。別プロセスから600tick復帰一致もtrue。非表示Player試験で画質/性能は主張しない。警告は未コンパイル変更によるインポート処理等の未反映注意。配布確定前に警告なしで再ビルドが必要。証拠render-build-messages.json / render-player-v3.json / render-player-resume-v3.json。

### v4クリーンビルド
事前Refreshとコンパイル/インポート中拒否、CleanBuildCacheを使用。76.11秒/135720813bytes/error0/warning0。BuildReport警告内容も自動保存。配布対象173ファイル中171はv3同一、boot.config/resources.assets差分。v2の未更新チェックポイントのコピーから別プロセス600tick復帰一致。初回はv3の復帰試験後に更新された保存と旧期待値を混ぜ不一致だったため不採用、製品不具合と判断しない。証拠render-clean-resume.json / render-clean-artifacts.json。v4の全武器再試験や目視は未実施。

### 全EditModeと独立描画QA
7adaf88時点の全EditMode255/255、41.79秒（render-all-editmode.json）。独立read-only QAで同tick分裂子の演出欠落（残件）と一時停止中の_Time揺らぎを検出。後者を_InkTimeへ変更し表示専用時計をMaterialに設定。実行中に進行後、一時停止した値25.04041が複数フレーム後も一致（ink-clock-check.json）。シミュレーション未変更。修正後は限定Play検証で、v4配布ビルドへの反映は未実施。

### 密集人工配置と同tick撃破の調査
80体/5種を隔離状態で格子配置し、描画20呼び出しでRenderer316→316。自然Wave・経済・性能の証拠ではない。画面下側の座標圧縮で輪郭重なりが強いため改善対象。dense-battle-check.json/png。AttackEventはフィールド撃破の一覧を持たず、同tick消失子を描画だけで正確に復元できない。シミュレーション変更禁止を維持し、演出欠落は未解決とする。

### 下側の描画間隔
核位置を戦場上から72→65%へ変更し、下側の投影係数.56→.70（間隔25%増）。上側/下側の墨スプライト配置も共通CoreScreenDepthから計算。人工80体画像を比較、Renderer316固定。15点で墨面/Project一致、最大誤差2.38e-7 world units。dense-battle-before-spacing.pngとdense-battle-check.png、ink-projection-check.json。シミュレーション/射程/保存未変更。密集の完全な判読性や実機性能の合格ではない。

### 成長枝の濃墨層
新しい枝を描く時のみ、既存Fiberストロークに半径52%のPool層を同じ曲線で重ねる。根との黒さを揃え、掠れた外縁を保持。seed42/6000tick/Wave17で金の枝先と画面を確認。dirty upload313、全面再構築0。card-art-wave17.png更新。成長判定/保存/Runtime変更なし。形状の完成美術と性能再計測は未達。

## 2026-10-23 メニューuGUI/TMP移行
ホーム・研究・記録をBokumyakuMenuHudへ移行。BokumyakuUiPartsのボタン/数値/パネルと、戦闘用の紙/筆枠テクスチャ/同梱Noto Serif CJK JPを共有。固定タブ/通知とScrollRectを分離し、安全領域へ配置。本文は指定サイズの85〜100%で調整。旧GameのOnGUI、Portrait/InkHud、GUI補助メソッドを撤去。エンジンBrush LabのIMGUIは対象外。

軽量検証: Unityコンパイル、1080×2400の3画面目視、同梱フォント一致/文字収録/テキスト領域の検査。menu-migration-check.json のpass=true。武器/構成/方針、枠保存・呼出、音の消音切替、研究購入、出撃、終了取消・確定、履歴生成、保存再読込を確認。画面外ボタンはScrollRect位置を移動してからraycast到達とpointerClickを検査。実指によるドラッグや実機検証ではない。画像の大量結晶は隔離QA用付与。

手順: 停止したclean sceneでMenuMigrationCheck.Font（必要時のみ）→Arm→MenuInteractionCheck.Run→evidence/menu-migration-check.jsonの完了確認→Play停止→BokumyakuInkCheck.Cleanup。旧PortraitCheck等のIMGUI reflection手順は今回のメニューには非対応。
初期検証ではタイトル領域不足を修正。QA側もダイアログの描画フレームを待つよう修正した。フル検証・Windows再ビルド・スマホ実機はこの限定結果に含めない。
関連EditModeフィルターBokumyakuは213/213合格（menu-editmode.json）。

## Windowsメニュー統合ビルド v5
source9a60b58。Builds/Bokumyaku-menu-v5/Bokumyaku.exe。成功/135715213bytes/93.88秒。CLIの30秒timeout後も同じEditorが完了したことをmenu-build-v5.jsonとEditor.logで確認し、重複起動しなかった。旧IMGUI用abandonConfirmation fieldのCS0414警告1件をソースから除去。ビルドは除去前、次回ビルドでwarning0を再確認する。
Test-BokumyakuMenuPlayer.ps1による隔離Player試験: 6武器900tick/保存/自動周回passed=true、別プロセス復帰600tick一致passed=true。終了コードともに0、118.75秒/15.63秒。証拠bokumyaku-player[-resume/-launch]-menu-v5.jsonとartifact JSON。非表示起動のため画面目視/フレーム性能の証拠にしない。Editorの1080×2400画像は前節の限定証拠。配布Playerの目視とスマホ実機は引き続き未検証。

## メニュースクロール表示
ホーム/研究/記録の共通ScrollRectに金色のScrollbarを追加。内容が収まる場合はAutoHide、本文と重ならない専用幅を確保。1080×2400の隔離PlayでscrollHandler入力を送信し、normalizedPosition 1→0、研究4番目の購入ボタンがViewport内に入ることを確認。menu-scroll-check.json、menu-scroll-research.pngを目視。軽量検証であり実機タッチ/指ドラッグ・フル検証ではない。手順MenuMigrationCheck.Arm→MenuScrollCheck.Run→停止→BokumyakuInkCheck.Cleanup。

## 2026-10-23 分業の追加納品・Player画像取得
素材室からdelivery-v3/button_ink_frame_03.png（透明横長筆枠）を受領。九分割previewを本室でも確認。未統合。音室のmobile_auditでmono合成と30分40秒のPCM反復を検査し、既存WAVハッシュ不変。BGM全体mono低下0.045dB、聴感/実機は未検証。各納品書参照。
PlayerProbeに明示的 --bokumyaku-capture-screens を追加。通常起動は変わらず、QAセーブでのみホーム/戦闘を撮影。v6ビルドは67.98秒/135716237bytes/error0/warning0。予約delayCallは開始されず、idleを確認して直接Runで実行した。CLIは30秒timeout、Editor側の完成レポートで成功を確認。
非表示Playerのキャプチャは黒一色で不合格。ファイル生成だけで画面表示成功とは扱わない。player-home-v6.png / player-battle-v6.pngは失敗証拠。表示可能な環境での配布画面検証が必要。
v6機能試験は両プロセスexit0/passed=true（116.83秒/15.65秒）。6武器/自動周回/別プロセス復帰は合格、画像は別判定で不合格。

## 横長筆枠のゲーム統合
button_ink_frame_03.pngをResources/BokumyakuArtへ導入。Sprite Single、border300/240/300/240、mipmapなし、非圧縮。メニューボタンを紙地・墨文字・選択金へ変更、専用Sliced枠はfillCenter=false。円相の矩形引き伸ばしをボタンから除去。1080×2400のホーム画像を目視し、MenuInteractionCheckの出撃/終了確認/研究/保存復帰等pass=trueを再確認。画像とJSONを更新。隔離Play停止・GameView復元済み。ゲームへの採用初版で、完成美術/実機合格ではない。
ボス識別: 敵本体の背面へ朱の筆輪を追加。点滅/回転なし、撃破時は同時dissolveしpoolへ返す。人工5種80体の360×640画像を目視、20描画でRenderer340固定。dense-battle-check.json/pngを更新。密集時重なりは残り、自然Wave性能/実機/完成美術合格ではない。シミュレーション未変更、隔離Play停止/復元済み。

## 表示Playerの再確認（ユーザー承認あり）
ユーザーの明示的な表示起動許可を受け、v6をWindowStyle Normalで起動。隔離QAセーブを使用。取得したホーム/戦闘画像に実描画があり、本室で目視した。非表示時の黒一色と区別する。
要求1080×2400に対して実PNGは1080×1080だったため、縦スマホ解像度合格とは扱わない。PC正方形表示でのメニュー/戦闘描画確認。v6は新筆枠/ボス朱輪より前のビルド。最新美術を含むビルドと画面に収まる縦寸法での再確認が残る。
表示起動の機能試験/別プロセス復帰もpassed=true、exit0（107.65秒/15.67秒）。自動終了済み。player-visible-dimensions-v6.jsonに実寸を記録。

## v7 縦Windows Player
source62ca6e4、Bokumyaku-portrait-v7、142052557bytes、66.36秒、error0/warning0。ユーザー許可済みの表示起動で540×960を指定し、実PNGも同寸。ホームの新筆枠、戦闘の描画を目視。画像はWave3でありボス朱輪自体のPlayer確認は含まない。上部WAVEと下部レベル表示が窮屈なためUI改善残件。スマホ実機・完成美術合格ではない。VisiblePlayerスクリプトにBuildFolder/Width/Height/EvidenceTag引数を追加し、今後は既存証拠を上書きせず再利用可能。
v7表示Player: 機能試験116.89秒/復帰15.73秒、両exit0/passed=true。ウィンドウ自動終了。実寸JSON: player-visible-dimensions-portrait-v7.json。
HUD余白修正: WAVE見出し/数値、強化レベル/修復/ゲージ/カードの領域を分離。360×640の限定PlayでRect非重複、レベルとゲージ間3.56px、修復ボタン高42.81px、画像を目視。hud-spacing.json/png。InkCheck.Arm→Run→HudSpacingCheck.Run→停止→Cleanup。実機/最新版ビルド確認は含まない。

### 墨核の金脈表示
球状の光を細い金輪/放射状の金脈/白い中心へ変更。96pxテクスチャを初期化時に1回生成しDisposeで解放。シミュレーション/Runtime変更なし。初稿は透明度のSmoothStep引数誤りで四角く表示、修正後Wave1/360×640/演出軽減で目視確認（core-light.json/png、比較card-frame.png）。限定Play確認であり実機性能ではない。

### 点墨/貫筆の先端移動
既存の軌跡/着弾へ120ms/180msの墨先端移動を追加。既存128effect上限とプールを共有。演出軽減で生成/表示を抑制。AttackTravelCheckで2武器の注入イベントを使い位置変化、軽減時非表示、シミュレーションsnapshot不変を確認。360×640画像attack-travel.png。限定描画試験、実戦タイミング/性能計測ではない。

### 描画磨き込み後の3分測定とボス停止修正
15a4f40、Editor360×640/seed42/Farming/1倍、6000tickまで事前進行後に実時間180.04秒。Wave17→32、304撃破。平均16.81ms/最大123.86ms、全面再構築0、dirty upload2338回。計測中CLIによる状態取得を含むEditor値であり、最大値の原因は未特定。実機/カクつき解消の合格ではない。evidence/render-polish-15a4f40-timing.json。独立ソースQAでボス赤輪がヒットストップ中に本体からずれる条件を指摘。中心を本体localPositionへ修正し、BossFreezeCheckの注入位置変更で停止中/再開後の一致を確認（boss-freeze.json）。

## 敵と墨枝の輪郭分離（2026-10-24）
InkSpriteに既定0の_PaperEdgeを追加。敵本体だけ.65を設定し、四近傍alpha差から1.25texel幅の淡い紙色の縁を作る。形状の穴/掠れを残し、既存UV揺らぎと消失coverageを共有する。追加SpriteやCPUテクスチャ更新はない。背景/攻撃/核の設定は0を維持。
同じ夜Wave10・7種×3列＋ボス22体で前画像landscape-depth-2-denseと後画像enemy-edge-2-denseを目視比較。太い墨枝に重なる分裂種/ボスの外形が追いやすくなった。1080×2400、停止snapshot不変、シェーダーエラーなし。演出軽減ONで22敵のedge=.65、他Spriteのedge=0を確認（enemy-edge-properties.json）。
検証は静止した人工配置と設定の検査。通常密集戦闘、消失アニメの実画面比較、GPU性能/スマホは未検証。四近傍サンプル追加の負荷は次の統合計測で確認する。シミュレーション/Runtimeは未変更、フルテストは再実行していない。

## 分裂子の同tick撃破通知（2026-10-24）
tick開始前にいた敵の差分だけでは、そのtick中に生成・撃破された分裂子の墨跡/飛沫を拾えなかった。RunSimulatorに保存対象外のDefeatedThisTickを追加し、Hurtで撃破確定時だけ記録、Tick冒頭で消去。描画AfterTickはこの通知を消費する。HP/報酬/乱数/保存形式/ルール版は変更なし。残HPの表示は従来差分方式。
関連EditMode265/265合格。親と子2体の通知、再打撃の二重通知なし、次tick消去、Restoreで通知再生なしを追加。旧rules継続試験も合格。
隔離Editor PlayでBeforeTickの後に分裂親を生成し、production Hurtで親と子2体を撃破。AfterTick後の撃破3/飛沫3、墨跡4096pxの部分更新、全面初期化増加なし。人工操作による接続検査で、自然プレイの遭遇動画や性能計測ではない。evidence/defeat-notifications-editmode.json、split-death-presentation.json、scripts/UnityAutomation~/SplitDeathCheck.cs。

## 枝描画のフレーム分散
2026-10-24。Update/PresentBattleからの重複要求をTime.frameCountでまとめ、1フレーム1本の枝を完成させる。根のスキップは描画本数に含めない。既存2パス/16点/繊維の筆致を変更せず、途中のSUMIストロークを次フレームへ持ち越さない（撃破跡のBeginによる中断を防ぐ）。
隔離Editorで900tickの7ノード構成を準備。フレームガードを検証開始のため-1へ戻し、同フレームでPresentBattleを2回呼ぶとpaintedが0→2→2（根＋1枝）。後続フレームで7/7まで完了。停止中snapshotと全面初期化回数は不変。branch-budget.json / BranchBudgetCheck.cs。
これは分散と完了の接続確認で、Windowsの停止時間改善はまだ未測定。最大規模の復帰では段階的に枝が現れる。1枝自体の処理時間も再測定が必要。

## 初期根の描画分散（2026-10-24）
DrawRootFlourishesの全描画を主根7組→側根7組の14フレームへ分割。元の座標・seed・筆設定・重ね順を維持し、各組でストロークを閉じてdirty tileだけflushする。通常枝と同じフレーム予算を共有し、初期根完了後に枝を描く。60fpsでは約0.23秒かけて根が現れる設計であり、実時間はfpsに依存する。保存復帰時もこの視覚的な描き直しがある。
RootBudgetCheckの隔離Playで同フレーム0→1→1、後続14/14組・枝7/7。paused snapshot不変、全面初期化増加なし。コンパイル＋対象Play検証のみ。Windowsフレーム性能、最終ピクセルの完全一致は未検証。Runtime/シミュレーション/経済/保存形式は変更なし。
