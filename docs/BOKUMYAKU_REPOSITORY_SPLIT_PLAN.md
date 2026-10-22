# 墨脈リポジトリ分割計画

2026-10-22。**計画のみ。ファイル移動、新リポジトリ作成、公開範囲・ライセンスの変更は未実行。** 調査時HEADは `888c75f8c1ce26db7dd6de5d44bd944d9fe244ad`。実行時には未コミット差分と最新の受入証跡を再確認する。

## 分割単位と依存

|対象|ゲーム側へ引き継ぐもの|SUMI側に維持するもの|
|---|---|---|
|ソース|`UnitySandbox/Assets/BokumyakuPrototype/` 全体と全 `.meta`。Simulation、Persistence、Presentation、Editor、Diagnostics、Tests/Fixtures、Scenes、Resourcesを一緒に移す|ルート `package.json`、`Runtime/`、`Editor/`、`Tests/`、`Documentation~/` と各 `.meta`。敵・経済・保存を入れない|
|Unityホスト|現 `UnitySandbox/Packages/`、`ProjectSettings/` を新ゲームrepo直下へコピー。ゲームAssetsの相対位置は `Assets/BokumyakuPrototype/` のまま|Brush Lab検証用ホストと `Assets/BrushLab/`、`Assets/CodexAutomation/` のSUMI試験。ゲーム分離後も描画回帰を実行できるようにする|
|素材|`ArtSource~/Bokumyaku/`、`docs/art/`、ゲームResourcesの6PNG/import設定、ゲーム用出所記録|汎用描画見本とエンジン独自テスト素材。ゲーム背景や武器絵はパッケージへ入れない|
|仕様・証跡|`docs/BOKUMYAKU*`、ゲーム調査、該当 `docs/evidence/`、対応する作業履歴・出所記録を選別してコピー|エンジン仕様・Brush Lab証跡。元の混在台帳は歴史資料として保存し、新repoにはゲーム専用の継続台帳を作る|
|検証スクリプト|`scripts/*Bokumyaku*` とゲームが使う `Test-GameBalance.ps1` 等を内容確認して引き継ぐ|`Test-Core.ps1`、Brush Lab/Unity接続・セットアップ用途。共有ツールは依存を明記して必要最小限だけコピー|

現asmdef依存は `Simulation`（`noEngineReferences:true`）←`Persistence`、両者＋`ConnectPiece.SumiEngine`←`Presentation`。`Diagnostics` はこの4つを参照し、`Bokumyaku.Editor` はPresentationを参照する。ゲームの `Bokumyaku.Tests` はSimulation/Persistence参照で、表示試験の一部はReflectionでPresentationを読む。名前とGUIDを初回移行時に変更しない。SUMI runtimeの実assembly名は `ConnectPiece.SumiEngine` であり、フォルダCore/Unityを別assemblyと誤認しない。

`Bokumyaku.unity` と `GameSceneSetup.ScenePath` は `Assets/BokumyakuPrototype/Scenes/Bokumyaku.unity`。現在の `EditorBuildSettings.asset` はシーン一覧が空なので、コピーだけでビルドできたと判断せず、既存のゲームビルド経路と実際のシーン指定を確認する。

## パッケージ供給

現manifestは `file:../LocalPackages~/com.connectpiece.sumi-engine`。`Setup-UnitySandbox.ps1` が配布対象だけのミラーを作り、ルートpackage内にホストを再帰インポートすることを防いでいる。**現HEADを無検査でGit依存へ置き換えない。** ルートに `UnitySandbox/` が同居しているため、Gitパッケージには不要なホスト・ゲームが混入し得る。

最初の推奨案は、SUMIの配布専用branch/commitを作る工程を別途設け、rootにpackage.json/Runtime/Editor/Tests/Documentation等の必要ファイルだけを置くこと。元ソースcommitと配布commitの対応・ファイルhashを記録し、ホスト/ゲーム/証跡/秘密情報が入っていないことを確認する。将来、開発ホストの配置を変更してmainをそのまま配布可能にする案は別の変更として扱う。

検証済み配布commitができた後のmanifest形式は以下。プレースホルダーを実在する40桁SHAに置換する。branch名や浮動tagには固定しない。

```json
"com.connectpiece.sumi-engine": "https://github.com/kntnext-hash/sumi-engine.git#<verified-package-commit-40hex>"
```

UPM解決後の `packages-lock.json` も記録する。Unityは現行の **6000.3.26f1**、Test Framework **1.6.0** を維持し、Pipeline **0.9.0-exp.1** は現在の自動試験依存として初回移行では維持する。`testables` のSUMI指定も初回は残し、ゲーム回帰とpackage回帰を分けて報告する。未使用モジュール削減は移行成功後の別変更。認証情報をURL/manifestへ埋め込まない。README/出所台帳では公開ライセンス未決定であり、分割に伴う勝手なMIT化・公開化を行わない。

## 実行手順と受入

1. 最新ソース・未コミット差分・通常セーブ・旧Playerを保護し、移行元commitと比較用テスト/画面/ビルド証跡を確定する。最初はコピーで作業し、元repoから削除しない。新repoの場所・権限・公開範囲は実行時の合意に従う。
2. 新repo直下をUnityプロジェクトにする。Assets/Packages/ProjectSettingsを上記単位でコピーし、全metaを保持する。Library/Temp/Logs/Obj/UserSettings/LocalPackages~のミラー、Builds、個人セーブは持ち込まない。`.gitignore` の `UnitySandbox/` 接頭辞を新配置に合わせる。
3. SUMIの検証済み配布SHAへ切り替え、Editorを新プロジェクトの絶対パスで開く。GUID欠落、重複assembly、BokumyakuとBrush Labの混入がないことを確認する。旧セットアップscriptはmanifestをローカルミラーへ戻すため、新ゲームrepoでそのまま実行しない。
4. scriptsの `UnitySandbox/Assets/...`、ビルド出力、fixtureパスを新配置へ適応する。過去比較scriptの `git show <旧SHA>:UnitySandbox/...` は新repoの履歴に存在しない可能性がある。元repoを読み取り専用の比較元引数として指定するか、出所SHA/hash付きfixtureへ置き換える。比較不能を合格扱いしない。
5. クリーンなUPM解決/Editor compile、ゲームEditMode、SUMI package tests、旧fixtureの未来・終端、全世代セーブ移行、価格/戦闘の独立C#試験を実施する。素材6点のResource読込と見た目、枝overlay、ホーム→研究→戦闘→中断復帰→結果→自動再出撃、横/縦UIを確認する。新Windowsビルドと別プロセス復帰も検証する。テスト件数は実行結果から記録する。
6. QAは `SavePathOverride` と専用保存先を必ず使う。`persistentDataPath` はcompany/product設定に影響されるため、初回に設定を変えず実パスを確認し、通常保存を触らない。製品名変更時は既存v8〜旧Metaを見失う問題を別の保存移行として扱う。
7. 両repoのcommit/SUMI配布SHA/移動対応表/受入証跡を記録して切り替える。新ゲームrepoで再現できるまで、元ゲームコード・素材・履歴を削除しない。SUMI側のゲーム除去は独立した後続commitとする。

## 戻し方

切替失敗時は新プロジェクトを閉じ、保護した元repo/元Editor/元Playerで作業を再開する。配布SHA更新の問題なら新ゲームrepoのmanifestとlockを直前の検証済み組へ戻す。未コミット差分を破棄する `reset --hard` や通常セーブの上書きは行わない。新旧Playerの保存互換を想定せず、QAの隔離保存だけで復旧確認する。コピー方式なら元データを逆移動する必要はない。

## ノベル等への再利用案

現ゲームの透明枝overlayは `BokumyakuGame.RebuildBranchOverlay` が `canvas.Core.Paper.PigmentAt` からalphaを作り、枝追加時だけ更新している。背景合成・透明墨の表示は他作品でも使える候補だが、枝座標、攻撃演出、UI、報酬には依存させない。

将来の独立変更で、SUMI Unityアダプタ側に「紙の顔料→透明Texture2D」「更新通知時だけ再生成」「Texture破棄とサイズ変更」の小さな表示ヘルパーを設ける案がある。紙色/墨色/密度/透過を明示パラメータにし、既存RenderRgbaとの差、alpha/質量の扱い、キャッシュ無効化をテストする。ノベル側はこれを文章出現、挿絵マスク、筆跡遷移に組み合わせる。シナリオ進行・文字組み・台詞保存・ゲーム固有素材は利用側が所有する。今回の分割でこの抽出まで混ぜず、最低2利用例で必要性を確認してからAPIを確定する。
