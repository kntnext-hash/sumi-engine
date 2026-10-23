# Windows統合候補 rank-v12
2026-10-24。商品完成版ではなく、段位/描画/研究を統合した確認用ビルド。

## 起動
Builds/Bokumyaku-rank-v12/Bokumyaku.exe を実行する。Bokumyaku_Data、MonoBleedingEdge、UnityPlayer.dll等の同梱物が必要で、exe単独では配布しない。
通常起動は通常セーブv11を使う。旧v10以下は原本を残して移行する。検証用起動はTest-BokumyakuVisiblePlayer.ps1が固有logs配下へ隔離する。

## 操作
ホームで段位と「周回して育てる」または「記録に挑む」を選び出撃。戦闘中の墨で強化、周回で得た結晶で研究する。途中の戦闘は保存し、次回ホームから戻る。研究は周回の合間に購入する。

## 確認と限界
Unity6000.3.26f1、Windows64 Mono。ビルド110.47秒、150,020,397bytes、警告/エラー0。
表示Playerで6武器各900tick、自動周回、別プロセスで600tick以上の保存継続一致を確認。検証中のプレイは一部人工的な敗北直前への早送りを含み、自然周回を何時間も遊んだ結果ではない。
初回の要求1080×2400はOSにより実寸1080×1080となった。これを縦画面検証とは扱わない。
初回平均フレーム16.66〜17.08ms、最大51.68〜398.99ms。各武器15秒前後のフレーム間壁時計、起動直後を含む。GPU時間/スマホ性能ではなく、一時停止の原因も未特定。性能合格とはしない。

証拠: evidence/rank-build-v12.json、bokumyaku-player-rank-v12.json、bokumyaku-player-resume-rank-v12.json、bokumyaku-artifact-rank-v12.json。
独立静的確認ではrules9/価格3/保存v11、敵縁の消失とプール、同tick撃破通知について具体的不具合指摘なし。実行/聴感/性能の代替ではない。

残件: フレーム停止の原因特定、自然な密集戦闘、敵と攻撃の美術、音の聴感、スマホ実機と配布設定。別repo移行/ストア申請は未実施。

## 縦画面の再確認
540×960で実寸も540×960。6武器各900tick、自動周回、別プロセス復帰が再度合格。画像でHUDとカード表示を確認した（物理クリックの全操作確認ではない）。
平均16.68〜17.33ms、最大40.43〜613.28ms。小解像度でも停止が残るため、解像度だけを原因とは判断できない。描画/同期保存/GC等の個別タイミングを次に測る。初回と異なる起動条件の測定であり解像度の因果比較には使わない。
evidence/*rank-v12-portrait.json/png。全テスト再実行・独立QA・実機を含む完成監査は未完了。

## timing-v13 原因切分け
2026-10-24。明示QA起動だけBattleTimingを有効化し、保存/枝描画/戦闘tick/撃破VFX/PresentBattleの10ms以上を最大512件記録。通常起動は無効、戦闘結果/保存形式は変更なし。
Windowsビルド66.995秒、エラー/警告0。540×960の6武器/自動周回/別プロセス復帰合格。
記録されたSaveProgressは22件、最大24.7574ms（10ms未満は記録しないので平均を全保存の平均とはしない）。DrawNewBranches最大86.74ms、PresentBattle最大87.02ms。この2値は同フレームの入れ子で加算しない。
フレーム最大482.52msは計測した区間だけでは説明できない。周回切替時の重い描画が複数回同フレームで発生している。現計測は各武器開始直後のフレームも含み、安定戦闘と切替後の遅延破棄/初回描画を区別していない。GPU待ち/GC原因の確定は未達。
次: フレーム番号付き停止イベントと切替/安定戦闘の分離、枝描画の分散。保存保護を弱める変更は行わない。
証拠evidence/*timing-v13.json/png。コードと実行が通った計測追加であり、停止修正の完了ではない。

## budget-v14 枝分散後の再計測
2026-10-24。ビルド64.06秒、エラー/警告0。表示Player実寸540×960、6武器各900tick・自動周回・別プロセス600tick復帰合格。画像でHUDと戦場を確認。物理タップ/スマホ実機の試験ではない。
開始2秒とそれ以後を分けた壁時計計測を追加。全区間平均16.64〜17.18ms、最大486.89ms。開始2秒以降は平均16.66ms、最大33.26〜39.17ms。大きな停止は今回開始直後に集中。起動後2秒を除けば性能合格という意味ではない。
DrawNewBranches記録最大15.28ms（前回86.74ms）、SaveProgress22.52ms。異なる実行の比較であり、枝変更だけの効果を確定しない。Pointの39.17msフレームにはGC1回が同時記録されたが、全停止の原因とは断定しない。
残る候補: ResetInkの飾り根一括描画、テクスチャ生成/遅延破棄、初回GPU描画。まだ原因未確定。次は初期描画を区間計測する。証拠evidence/*budget-v14.json/png。今回区分は対象Windows検証のみ、全EditMode再実行なし。

## root-v16 初期根分散後（2026-10-24）
root-v15はBuildPlayer成功だが未コンパイル変更警告1件。比較対象に採用せず、Refresh後にroot-v16を別出力で作成。v16は60.50秒、150,023,117bytes、警告/エラー0。
表示Player540×960で6武器各900tick、自動周回、別プロセス復帰が合格。同Wave3の画像で根・HUDを確認。全区間平均16.92〜17.50ms、最大528.56ms。開始2秒以降の最大32.21〜40.39ms。
RootFlourish最大66.62ms、DrawNewBranches最大66.63ms、PresentBattle53.50ms、SaveProgress27.29ms。入れ子なので加算禁止。Sweep528.56msとResonance493.39msは開始直後、GC回数差0、未描画枝4。GC停止と断定できず、初期根の分散で停止解消とは言えない。1組のCPU描画も依然重い。
次は根の処理単位をさらに細かくする方法と初回GPU/リソース処理の切分けを検討。単に計測から開始直後を除いて合格にしない。今回対象Windows検証のみ。evidence/*root-v16.json/png、root-build-v15.jsonは警告記録として保管。

## stroke-v17（2026-10-24）
ビルド112.68秒、156,374,861bytes、警告/エラー0。実寸540×960で墨枠HUDを目視。6武器各900tick/自動周回/別プロセス復帰合格。全区間平均16.83〜17.41ms、最大536.30ms。開始2秒以降最大35.58〜42.46ms。RootFlourish最大46.54ms、DrawNewBranches46.55ms、保存25.74ms。前回根66.62msより低いが別実行のため厳密な因果比較ではない。Sweep536.30ms/Resonance506.65msはいずれも開始直後、GC差0。性能未合格。
独立静的確認: probeは前武器末尾でReadPixels/Save/Reload、次武器先頭でAbandon/Startを描画yieldなしで続ける。ResetInkの同期時間はフレームタイマー開始前だが、GPUテクスチャDestroy/Create/Applyの遅延処理は後続へ残り得る。これは通常の1回開始より重いストレス条件。DirtyInkSurface.Clearは毎回GPUテクスチャを再生成し、InkBattleWorldもテクスチャID変更時に2枚のSpriteを作り直す。資源変動はコードで確認、500msの原因と確定はしない。
次: 既存ストレス試験は保持し、通常Startとリセット回数・時間を別計測。テクスチャIDを保ったクリアを検討し、墨量/保存/部分更新の不変性を確認する。UI Canvasや描画待ちは現計測外。今回対象Windows検証のみ。evidence/*stroke-v17.json/png。

## reuse-v18（2026-10-24）
GPUテクスチャ再利用版。ビルド64.35秒、156,374,861bytes、警告/エラー0。同じ540×960/6武器連続切替試験で6武器・自動周回・別プロセス復帰合格。Wave3の根/墨枠HUDを目視。
全区間平均16.81〜16.87ms、最大47.07ms（直前stroke-v17は536.30ms）。6武器ごとの最大47.07/43.98/46.79/37.51/43.12/41.26ms。開始2秒以降最大31.30〜43.98ms。RootFlourish45.29ms、DrawNewBranches45.30ms、保存27.22ms。500ms級停止は今回再現なし。ストレス条件は変えておらず、資源再生成削減の効果を支持する。ただし単回別実行の比較で、全原因の確定や全状況の性能保証ではない。
残件: 1ストロークの初期根CPU負荷、通常開始の独立計測、密集戦闘、スマホ実機。今回対象Windows検証のみ。証拠evidence/*reuse-v18.json/png。

## audio-v19（2026-10-24）
6武器SE入りWindows候補。ビルド68.39秒、156,540,445bytes、警告/エラー0。実寸540×960でWave3の戦場/HUDを目視。6武器各900tick、自動周回、別プロセス600tick復帰が合格。各武器の実攻撃中に対応するsfx_attack_*のAudioSourceが再生中・volume>0・非muteであることを確認。フィールドheardWeaponSourceはこの機械的観測であり、人間の試聴を意味しない。
平均16.80〜17.43ms、最大543.46ms。Point518.39ms（frame17→18）、Sweep543.46ms（2660→2661）が開始2秒以内に再発。いずれもGC差0、未描画枝4。reuse-v18の47.07msは単回結果で、停止の恒久解消を証明していなかった。新音源追加との因果も未確定。密集/実機性能は未合格。
今回対象Windows検証のみ。証拠evidence/*audio-v19.json/png。次は長いフレームの描画/待機時間を分解し、初期根1ストロークCPU負荷も改善する。通常保存/シミュレーションは変更していない。

## 初期根と転送の分離計測（2026-10-24）
BattleTimingに任意の記録下限を追加し、InkFlushだけ0.01ms以上を記録する。明示QA以外はタイマー/サンプルを作らず、既存の上限512件を維持。墨処理・転送方式・シミュレーションは変更なし。
隔離Editor Playのseed42、進行を停止して35本の初期根を描画。35回のFlushを取得、最大2.5254ms。RootFlourish記録最大41.7762ms。35本完了とシミュレーションsnapshot不変を確認。evidence/ink-flush-timing.json、再現scripts/UnityAutomation~/InkFlushTimingCheck.cs。
この条件では根のCPU描画負荷が転送API呼出し時間より大きい。GPU完了時間は測っておらず、Windows audio-v19の500ms停止の原因を確定するものではない。次はWindowsで同じ分離計測とフレーム内外の待機を調べる。今回軽量Editor検証のみ。

## flush-v20（2026-10-24）
転送分離/フォーカス観測版。ビルド64.32秒、156,540,957bytes、警告/エラー0。540×960/Wave3のHUDと戦場を目視。6武器/実攻撃SE/自動周回/別プロセス復帰合格。
平均16.81〜17.40ms、最大504.74ms（Sweep frame2682→2683、開始直後）。前後ともisFocused=true、GC差0。frame2682のInkFlush1.16ms、RootFlourish15.70ms、DrawNewBranches15.70ms（入れ子、加算不可）、frame2683のInkFlush0.42ms。これらの同期処理だけでは504.74msを説明できない。フォーカス喪失による停止はこの標本では支持されないが、GPU同期/描画待機など原因は未確定。
注意: BattleTimingは512件上限に達し、後半の区間サンプルが欠ける。記録範囲のInkFlush最大4.10ms、RootFlourish最大46.93msを全実行の最大と扱わない。該当504msフレームの記録は上限前に残っている。次は全フレームの処理段階を取得する方式と上限/欠落の明示を追加する。今回対象Windows検証、性能未合格。evidence/*flush-v20.*。

## phase-v21（2026-10-24）
明示QA起動だけでトップレベルPlayerLoopを前後計測。ゲーム進行/通常起動は変更なし。ログ容量8192、欠落件数を明示。Editor隔離60frameで構造/デリゲートの完全復元、重複Install/Restore、8193サンプル投入時8192保持＋欠落1、無効時非記録を検証。phase-timing-check.json。
Windowsビルド69.02秒、156,542,685bytes、警告/エラー0。6武器/SE/周回/別プロセス復帰合格。平均16.80〜17.43ms、最大521.18ms（Point frame17→18）。focused前後true、GC差0、droppedTimingSamples=0。
該当frame17: InkFlush1.15ms、RootFlourish15.84ms、Loop.Update15.97ms、Loop.PostLateUpdate500.93ms。入れ子は加算しない。長い停止は今回PostLateUpdate内に局在した。GPU待機/Present/描画完了など個別原因はまだ未特定。次はこの段階の子処理を計測する。TimeUpdateはframe番号増加境界を含み得るので単純な同番号合算禁止。今回対象Windows＋診断Editor検証、性能未合格。evidence/*phase-v21.*。

## postlate-v22（2026-10-24）
PostLateUpdateの各子処理の前後にQA専用計測を追加。処理順/内容は保持、Editor60frameの復元/上限確認合格。ビルド67.22秒、156,543,197bytes、警告/エラー0。6武器/SE/周回/別プロセス復帰合格。平均16.80〜17.39ms、最大544.09ms、欠落0。
Enso frame3551→3552: focused前後true、GC差0。frame3551のPostLate.FinishFrameRendering495.93ms、PostLateUpdate496.05ms、Update15.82ms、InkFlush1.09ms。長い停止は描画完了処理内へ局在。GPU/描画スレッド/ドライバー等のどれかはまだ確定していない。性能未合格。次はUnity Profiler等でこの処理内の待機を区別する。
検証中に別担当が紙札UIのソース/PNGを作成したが、当該Playerは差替え前にビルド済みの固定バイナリ。Editorの背景処理による外部負荷は統制しておらず、CPU/GPU絶対性能の厳密比較には用いない。紙札はこのWindowsビルドに未収録。evidence/*postlate-v22.*。

## direct-v22 同一バイナリの描画スレッド比較（2026-10-24）
公式Player引数 -force-gfx-direct（単一スレッド描画）でpostlate-v22を再実行。EXEとゲームDLLのSHA-256が対照と一致。ログで両プロセスのkGfxThreadingModeDirectを確認。製品ProjectSettingsは変更なし。参照: https://docs.unity.com/en-us/engine/6000.3/manual/unity-editor/command-line-arguments/player
6武器/SE/周回/別プロセス復帰合格。540×960、平均16.86〜17.46ms、最大559.00ms（Enso開始直後）。focused前後true・GC差0・ログ欠落0。
frame3542→3543の559msに対して、frame3543 InkFlush522.93ms、RootFlourish557.10ms、Update557.26ms。同一コードでもスレッド方式を変えると待機が転送API側に現れた。スレッド無効化は解決策ではない。GPU/ドライバー同期等が候補だが確定はせず、次はFlush内のSetPixels32/Apply/CopyTextureを分離する。直前のFinishFrameRendering495.93msとの単回比較であり厳密な再現率や因果の証明ではない。今回は同一Windowsバイナリの対象比較、性能未合格。evidence/*direct-v22.*。

## tile-direct-v23 / タイル別転送候補（2026-10-24）
白和紙カード入り計測版を67秒未満（66.11秒）、162,852,637bytes、警告/エラー0でビルド。Direct描画をログ確認、6武器/SE/周回/別プロセス復帰合格。540×960で白いカード/戦場を目視。平均16.87〜17.46ms、最大560.94ms、欠落0。InkTile.Applyがframe3544で517.35ms、frame5301で513.10ms。長い同期待機をTexture2D.Apply内に確認。SetPixels/Copyが10ms未満だったことはゼロコストを意味しない。evidence/*tile-direct-v23.*。
これを受け、1枚を同じFlush内で繰返し上書きする転送元を、宛先タイルごとの固定Texture2Dへ変更する比較候補を実装。384×512で192枚、画素容量CPU/GPU各0.75MiB（管理費別）、従来の各4KiBより増加。構築時だけ確保、Clear時再利用、Disposeで全解放。跨フレーム依存は残るため改善保証なし。
対象EditMode4/4（5.15秒）合格。連続Flush間にreadbackを挟まず最後にGPU alphaをCPU基準と±1で比較、端のクリップ/部分更新/リセットも合格。RGBA完全一致やWindows性能の合格ではない。tile-staging-editmode.json。次に同じWindows Direct条件で比較し、悪化/改善不明なら候補を再評価する。Runtime/シミュレーション不変。

## staging-direct-v24: 候補不採用（2026-10-24）
タイル別固定転送元の比較。ビルド62.77秒、162,853,149bytes、警告/エラー0。Direct描画、6武器/SE/周回/別プロセス復帰合格、欠落0。平均16.88〜17.51ms、最大561.31ms。Point frame14のInkTile.Apply517.37ms。直前v23最大560.94ms/Apply517.35msと同程度の停止が残り、メモリ増加に見合う改善を確認できなかった。
タイル別192枚案は不採用として1枚の転送元へ戻した。API別計測とreadbackを挟まない連続Flushテストは保持。戻した状態で関連EditMode4/4（5.32秒）合格。GPU alpha±1、部分更新、端コピー、Clear後source再利用を確認。Runtime/シミュレーション不変。evidence/staging-rejection-editmode.json。
次候補は、固定サイズの小さな転送アトラスへ複数dirty tileを詰め、Apply呼出し回数を減らす方式。宛先は部分コピーを維持し、全キャンバスCPU再生成はしない。まだ未実装。Unity公式はApplyの前に更新をまとめることを推奨するが、500ms問題を解消する保証ではない: https://docs.unity3d.com/jp/2019.1/ScriptReference/Texture2D.Apply.html 。SetPixelData等もApplyが必要で単純な置換では転送待機を回避できない: https://docs.unity3d.com/cn/6000.0/ScriptReference/Texture2D.SetPixelData.html 。
今回対象Windows比較＋関連EditMode。性能未合格。evidence/*staging-direct-v24.*。

## batch-direct-v25: 8タイル一括転送候補を不採用（2026-10-24）
32×256の固定stagingへ最大8タイルを詰め、17タイルを3 Applyで転送。関連EditMode5/5（4.96秒）、GPU alpha±1、部分更新/端コピー/Clear後の未使用スロット非混入を確認。候補ソース差分はevidence/batch-v25-candidate.patch（基点5f54b4a）に保存。
Windowsビルド64.80秒、162,853,149bytes、警告/エラー0。CLIは30秒でタイムアウトしたがEditorの同一ビルドが完了したため再実行なし。6武器/SEソース/自動周回/別プロセス復帰合格。Direct描画、540×960、平均16.81〜17.45ms、最大554.57ms、計測欠落0。Point frame16のInkTile.Apply515.40ms、focused前後true/GC差0。直前v23/v24と同じ約0.5秒の停止が残った。起動前frame1のLoop計測は戦闘フレーム集計外。
呼出し数削減は確認できたが停止解消にはならず、少数タイルでも32KiBを転送する増加分もあるため不採用。製品ソースとテストは5f54b4aの1枚32×32 stagingへ復元。候補ビルド/証拠は保存。今回は対象Windows比較＋関連EditMode、性能未合格。次は通常起動の負荷とGPU待機を切り分ける。タイル転送方式の同種候補を際限なく追加しない。evidence/*batch-direct-v25.* / batch-staging-editmode.json。

## settled-v26: 起動条件の切り分け（2026-10-24）
明示QA引数 --bokumyaku-settled-start を追加。Start直後の追加TryReloadProgressを省き、構成選択後ホームを5秒描画してからPoint/Farming/seed42/速度2を900tickだけ実行。6武器連続リセット・自動周回・別プロセス復帰はこのモードの対象外。通常起動にはprobeを生成せず、保存はQA専用。これは通常起動そのものの性能保証ではない。既存6武器モードは保持。
ビルド61.35秒、162,852,637bytes、警告/エラー0。同一バイナリ540×960でDirectと通常ClientWorkerJobsを各1回測定。Direct平均16.88ms/最大50.62ms/定常最大37.10ms、通常平均17.35ms/最大495.25ms/定常最大36.24ms。両方Point/購入/枝/墨/SEソース/同一プロセス保存復帰合格、欠落0。通常frame298 FinishFrameRendering467.54ms。Directで非再現でも通常側は再現したため、待機追加を製品の解決策として採用しない。リセット集中だけが原因とは言えず、描画完了待機の追跡が必要。
今回対象Windows比較（2回）、全EditModeの再実行なし。製品設定/Runtime/シミュレーション不変。次はUnity Profilerの描画スレッド/GPU待機資料を取得できる環境を確認する。evidence/*settled*-v26.*。ビルド時の一時Pipeline切断は同一Editorのready確認後1回再試行。ビルド命令30秒タイムアウト後は同一ビルドの成功レポートで完了確認、二重起動なし。

## profile-v27: Profiler取得経路（2026-10-24）
開発ビルド（Deep Profilingなし）を79.38秒、227,689,054bytes、警告/エラー0で生成。製品ビルド設定は変更せずBuildOptions.Developmentを当該ビルドだけ指定。
初回-profiler-enable併用はrawを3frameしかロードできず、戦闘停止の証拠として不十分（*-first.jsonに保持）。自動接続引数を外し-profiler-log-file / -profiler-capture-frame-count 700で再実行。ProfilerDriver.LoadProfileで0〜700の701frameを実ロード、Main/Render Threadの40ms以上sampleを抽出（94件、遅いframe8件）。既存Profiler記録はlogsへ退避し、active recording中はロード拒否。raw本体はlogsに保持し、パス/サイズ/SHA256をmanifestに記録。
Point900tick/購入/枝/墨/SEソース/同一プロセス保存復帰合格。2回目の戦闘計測は平均16.87ms/最大52.61msで約500msは非再現。Profiler frame294の開始Coroutine72.79msを記録したが、入れ子sampleは加算しない。前の非開発版495.25msとは構成が異なり、解消・改善・原因確定の根拠にしない。取得経路は確立、問題再現captureは未取得。次は同じ開発バイナリをProfiler無効で実行し、開発/計測条件の差を切り分ける。今回は対象Windows＋Profiler取得、性能未合格。
公式参照: https://docs.unity3d.com/kr/6000.0/ScriptReference/Profiling.Profiler-enableBinaryLog.html / https://docs.unity.cn/6000.1/Documentation/Manual/profiler-command-line-arguments.html / https://docs.unity3d.com/ja/current/ScriptReference/Profiling.RawFrameDataView.html 。証拠: evidence/profile-*-v27.json。rawは巨大生成物扱いでコミット対象外。

## development-unprofiled-v27（2026-10-24）
profile-v27と同一開発バイナリをProfiler引数なし・通常描画・settled-startで1回実行。Point/購入/枝/墨/SEソース/同一プロセス保存復帰合格、平均16.87ms/最大52.58ms/定常最大39.40ms。非開発v26最大495.25msとの差はあるが、開発版ではProfilerなしでも非再現。Profilerによる負荷だけで隠れたとは説明できない。開発/非開発構成差の原因は未特定。性能未合格のまま保留し、次は画面品質改善を進める。証拠evidence/*development-unprofiled-v27.*。

## hud-v28（2026-10-24）
非Development/CleanBuildCacheで150.15秒、168141101bytes、警告/エラー0。ソースb1875d6＋RoundPlate描画順修正。CLI30秒timeout後も同じEditor/beeプロセスの進行を確認し、完了reportを待機。重複buildなし。
明示許可済みの表示Player、隔離保存で6武器各900tick/購入/攻撃/SEソース/同一プロセス保存復帰、自動周回と別プロセス再開が合格。通常109.54秒、再開16.65秒。平均/max ms: Point16.81/50.76、Pierce16.82/49.07、Bleed16.80/47.74、Sweep16.77/43.51、Enso17.35/528.75、Resonance16.77/43.23。Ensoの528.75msは起動期、定常最大34.31ms。約500ms停止は未解決。聴感の評価はしていない。
OSが要求1080×2400を1080×1080へ制限、画像でUI縦領域が縮み一部文字が枠から出る。縦画面合格としない。同一バイナリで450×1000/SettledStartのPointだけ追加確認し、実寸450×1000、表示を目視、平均16.83/max55.37/定常max35.72ms。6武器結果とこの単一武器結果を混同しない。Windowsの横/正方形時レイアウトは残件。実機/全EditMode/独立QAは今回未実施。
証拠evidence/*hud-v28.*、*hud-portrait-v28.*、hud-build-v28.json。実行物Builds/Bokumyaku-hud-v28/Bokumyaku.exe、同梱Data/UnityPlayer/MonoBleedingEdge必須。通常セーブは変更せず、プローブは終了済み。

## viewport-v29（2026-10-24）
833d3bdの中央縦領域修正をWindowsへ反映。BuildOptions.Noneの非Development増分build、21.57秒/168141613bytes/警告エラー0。既存v28は保持。BuildViewportCandidate.cs、viewport-build-v29.json。
1080×1080を明示して表示Playerのsettled-single-pointを1回。実寸も1080角。中央幅607.5pxの縦画面へ戦場/HUDが揃い、v28正方形時の縦潰れを画像で修正確認。Point900tick、2購入、攻撃/枝/SEソース/同一プロセス保存復帰合格。実行37.48秒。平均17.36ms/最大523.13ms/定常最大35.44ms。500ms級停止は残っており性能合格としない。今回は6武器/別プロセス再開を再実行していない（v28結果は別記）。ウィンドウをドラッグしながらの変化/実機safe areaは未検証。
証拠evidence/*viewport-square-v29.*。実行物Builds/Bokumyaku-viewport-v29/Bokumyaku.exeと同梱Data/UnityPlayer/MonoBleedingEdgeを保持。通常セーブ未変更、検証プロセス終了済み。
