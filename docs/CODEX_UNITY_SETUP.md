# Codex × Unity ローカル構築

更新: 2026-10-21〜22 JST。状態: **公式ツール・Editor接続・操作試験完了**。描画P0の実マウス入力/preset比較は別途未合格。

## 最新の実測と通常操作

- Editor実体: `C:\Program Files\Unity 6000.3.26f1\Editor\Unity.exe`。ProjectVersion.txt: 6000.3.26f1 (de8167e7440c)。CLIへ登録済み。併存6000.6.7f1は本プロジェクトでは使わない。
- UnitySandbox生成済み。Test Framework 1.6.0、Pipeline 0.9.0-exp.1。Editorでライセンス解決、CLIのCloudサインインとは区別する。
- CLI ready、MCP 166ツール公開とEditor版/Scene読取成功。MCPの短いツール名には重複があるので、操作はCLIのグループ形式を優先する。
- `unity command script eval` のようにグループと動詞を別引数にする。`"script eval"`を1引数にするとCommand Not Foundになる。旧スキルの`editor_play`ではなく、現版は`editor.playmode start`。
- 実試験: EditMode 18 PASS、PlayMode 1 PASS。CS1519を2件検出→一時ソース除去→再コンパイルerror/warning 0。Consoleの現在件数0、過去の診断/意図的エラー履歴は証拠に保存。
- Scene生成で未保存初期Sceneの追加作成例外を修正。バッチ時は初期Sceneをユニーク名で保存し保護する。対話時は未保存Sceneの保存を要求する。

```powershell
$u = 'C:\Users\che22\AppData\Local\Microsoft\WindowsApps\unity.exe'
$p = 'C:\Users\che22\Desktop\SUMI_ENGINE_Codex_Starter\UnitySandbox'
& $u status --project-path $p --format json
& $u command test run --caller plugin --skill unity-cli --project-path $p --format json -- --mode editor --filter ConnectPiece.SumiEngine.Tests --timeout 60
& $u command test run --caller plugin --skill unity-cli --project-path $p --format json -- --mode playmode --filter Sumi.Automation.Tests --async_tests true --timeout 60
& $u status --until-ready --timeout 60 --project-path $p --format json
& $u command test status --caller plugin --skill unity-cli --project-path $p --format json
& $u command console status --caller plugin --skill unity-cli --project-path $p --format json
node scripts/Test-UnityMcp.cjs $u --editor-probe
```

画像は`docs/evidence/automation-cube.png`、`automation-brush-lab-view.png`、`brush-lab.png`。検証JSONも同ディレクトリ。PNG生成はAPI駆動であり、実マウス入力合格とはしない。
初回失敗後はTemp/UnityLockfileだけ残った。Editorプロセス不存在とファイル排他オープンを確認した後、当該ファイルのみ削除して再試行した。実行中Editorのロックを削除しない。

以下は初回構築時の経緯と、別PC向け手順。

## 実測した環境

|項目|結果|
|---|---|
|OS|Windows build 26200 / 25H2。レジストリProductNameはWindows 10 Homeと報告|
|Codex CLI|0.162.0-alpha.17.2。plugin marketplace/add/list対応|
|Unity公式Plugin|unity@unity-agent-plugin 0.1.8-beta、installed/enabled確認|
|Unity CLI|1.0.0-beta.13、winget Unity.CLI 1.0.0.20013から新規導入|
|Hub / Editor|既存インストール未検出。`unity editors --installed`は空配列|
|Editor候補|6000.3.26f1。dry-runで未導入・4,206,721,368 bytesを確認。実在バージョン固定は導入後|
|MCP|stdio設定済み。initialize / tools/list成功、ツール0件|
|Pipeline|接続0。installはUnitySandbox未生成のため失敗|
|ライセンス|LICENSING_CLIENT_UNAVAILABLE。認証状態は未確認|

`unity editors`の引数なし表示にはダウンロード候補も出る。導入確認には必ず`--installed`を使う。

## 別PCでの導入

既存版をまず確認し、未導入のものだけ実施する。Codex CLIは`codex plugin --help`で対応を確認する。

```powershell
codex plugin marketplace add Unity-Technologies/unity-agent-plugin
codex plugin add unity@unity-agent-plugin
codex plugin list --marketplace unity-agent-plugin --json
winget install --id Unity.CLI --exact --source winget --accept-source-agreements --accept-package-agreements --silent
unity --version
unity editors --installed --format json
```

PATH反映前は`$env:LOCALAPPDATA\Microsoft\WindowsApps\unity.exe`を明示する。既存EditorがあればProjectVersion.txtと照合し、アップグレードしない。

### 現在、人が必要な操作

標準配置先への次の導入は`ELEVATION_REQUIRED`で終了した。管理者PowerShellで実施するか、人が書き込み可能な導入先を選ぶ。Codex全体を管理者化する必要はない。

```powershell
unity install 6000.3.26f1 --yes --accept-eula --wait-timeout 60
```

Unityへのログイン・ライセンス認証は人が行う。認証情報をチャットやGitに保存しない。今回Hubは導入していない。ログインが必要なら公式CLIの`unity auth --help`、ライセンスは`unity license --help`でその版の手順を確認する。

## Editor導入後の再開

ルートはUPMパッケージ。UnitySandboxのみがホスト。配布ソースは既存セットアップがLocalPackages~へ同期する。生成先を正本として編集しない。

```powershell
unity editors --installed --format json
unity editors path 6000.3.26f1
# 実在するUnity.exeを指定。以下は標準配置例。
pwsh -NoProfile -File scripts/Setup-UnitySandbox.ps1 -UnityEditor 'C:\Program Files\Unity\Hub\Editor\6000.3.26f1\Editor\Unity.exe'
pwsh -NoProfile -File scripts/Install-UnityAutomationProbe.ps1
unity pipeline install --project-path ./UnitySandbox
unity open ./UnitySandbox
unity status --until-ready --timeout 60 --project-path ./UnitySandbox --format json
unity list --project-path ./UnitySandbox --format json
```

`Setup-UnitySandbox.ps1`の既存実装は未Unity検証。特にTest Frameworkの同梱版が見つからない場合は停止するので、Package Managerで対応版を追加して再実行する。生成コードだけでシーン作成成功とはしない。

## MCPとCLIの二経路

公式手順は`unity mcp configure codex --project-path <UnitySandbox絶対パス>`。今回のCLIでは、このコマンドはMCP追加に加えてsandbox network_accessとlocalhost許可設定も追加した。付随したネットワーク設定の完全一致ブロックだけを削除し、MCP登録を保持した。既存MCPエントリを削除・置換していない。再実行前に設定をローカルで退避し、変更を確認する。設定全文は秘密情報を含む可能性があるため公開しない。

現在のunity MCPは絶対パスのCLIを起動し、UnitySandboxへ固定されている。

```powershell
codex mcp get unity
node scripts/Test-UnityMcp.cjs
unity status --project-path ./UnitySandbox --format json
unity pipeline list --format json
```

MCPスモークは20秒で打ち切り、initializeとtools/listのみ確認する。ツール0件でもプロトコルは合格し得るため、Editor操作合格と区別する。現在のチャットにはUnity MCPツールが追加されておらず、実測は子プロセス経由。設定再読込後のチャットでツール公開と実操作を別途確認する。

## 専用操作試験 A–F（現在は未実行）

テストコードは`scripts/UnityAutomation~/`に保管。インストーラは実Unityプロジェクトがない場合、または既存のAssets/CodexAutomationがある場合、上書きせず停止する。

1. A: statusのEditor版・プロジェクト・readyと、Editorから取得した現在Sceneを保存する。
2. B: メニュー`Tools > SUMI Engine > Automation > Generate Connection Test`、または発見したevalで`Sumi.Automation.Editor.AutomationScene.Generate()`を呼ぶ。専用ConnectionTest.unityにCamera / Directional Light / Cube / Emptyを作る。既存シーンを上書きしない。Editorで保存シーンを開き、4ルートとコンポーネントを読み戻す。
3. C: `unity recompile`のその版のhelpを確認し実行。compiler error 0を確認。生成ソースは未コンパイル扱いのまま成功にしない。
4. D: 発見したPlayコマンドを実行し、数秒間隔でCube回転とフレーム数の増加を読む。`AutomationPlayTests.CubeAdvancesAndRotates`も実行。単なるisPlayingだけを合格根拠にしない。Game Viewの画像を別途取得する。
5. E: `unity list`で発見したConsole取得コマンドでerror/warningを取得し分類。警告を消去して成功記録にしない。
6. F: 専用テスト用C#ファイルに一時的な構文エラーを作り、検出→そのファイルを復元→再コンパイル→同じテストの合格を確認する。finally相当で原状回復する。今回この修正ループは未検証。

全`unity command`呼出しに`--caller plugin --skill unity-cli --project-path <絶対パス>`を付ける。Scene/Prefabは接続中のEditor APIで操作し、YAMLを直接編集しない。CoreにUnityEngineやゲーム固有ロジックを入れない。

EditMode/PlayModeのCLIバッチ例（Editorを閉じて実施、各実行は15分を上限に監視）:

```powershell
$editor = 'C:\Program Files\Unity\Hub\Editor\6000.3.26f1\Editor\Unity.exe'
$project = (Resolve-Path ./UnitySandbox).Path
& $editor -batchmode -nographics -projectPath $project -runTests -testPlatform EditMode -testResults "$PWD/logs/editmode.xml" -logFile "$PWD/logs/editmode.log"
& $editor -batchmode -projectPath $project -runTests -testPlatform PlayMode -testResults "$PWD/logs/playmode.xml" -logFile "$PWD/logs/playmode.log"
```

終了コードだけでなくXMLの件数・失敗数を検査する。Test Framework実行に`-quit`は付けない。GUIの画面評価とPNG確認は別に[手動QA](BRUSH_LAB_MANUAL_QA.md)を実施する。

## 有限の開発ループ

要求と受入条件→実装→同期/import→compile→Scene読み戻し→Playとフレーム進行→Console→修正→同じ試験→証拠→小さなcommit。

- 同じ失敗の修正は最大3回。起動待ちは1回60秒、ネットワーク一時障害は追加1回まで。
- 認証・管理者操作・未保存の他作業との競合は停止し、独立した検証のみ続ける。
- 自動修正は実エラーを根拠とし、合格XMLやスクリーンショットを偽造しない。
- 毎回4台帳を読み直して追記。並行作業の未追跡ファイルも保護し、明示したファイルだけstageする。

公式出典（2026-10-21参照）: [Unity公式Plugin](https://github.com/Unity-Technologies/unity-agent-plugin)、[Unity CLI](https://docs.unity.com/en-us/unity-cli/use-unity-cli)、[OpenAI Plugin packaging](https://developers.openai.com/plugins/build/plugins)。導入済みPluginのunity-cliスキルと実CLI helpを併用した。

### INSTALL_FAILED / インストーラ終了コード2の場合
2026-10-21はCLIログでサイレント起動3.4秒後の終了コード2を確認。コードだけで原因を断定しない。キャッシュ済みEXEの公式サイズと署名Valid、C空き約672GBを確認済み。ログ実体はMSIXのLocalCache配下だった。
通常フォルダlogs/installersへコピー済み（Git除外）。人が次を実行し、対話式の具体的エラーを確認する。再ダウンロードやセキュリティ設定の無効化は不要。
```powershell
& 'C:\Users\che22\Desktop\SUMI_ENGINE_Codex_Starter\logs\installers\UnitySetup64-6000.3.26f1.exe'
```
CLI経由でない導入が成功したら、実在するEditorパスを`unity editors add`で登録し、`unity editors --installed`で確認する。
