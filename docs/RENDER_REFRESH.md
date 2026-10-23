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
