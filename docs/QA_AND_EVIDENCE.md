# QA・証拠管理

にゃんスターの製品レビュー運用を参考に、検証の強さを区別する。

|等級|意味|何を保証しないか|
|---|---|---|
|S|静的なソース・JSON・構文・依存関係チェック|Unity実動|
|A|Unity EditMode/PlayMode自動テスト（正確な実行環境明記）|人間の画面評価、モバイル性能|
|R|Unity Editor/PC実画面・マウス/タッチ操作観測（画像/動画と実行日時）|実端末での60fps|
|D|Android/iOS実機、機種/OS/ビルド/負荷を明記|他の全機種|
|H|ユーザーが見て気持ち良いかの主観評価を記録|客観的市場成功|

P0合格に必要: S + A + R。Unity EditorがなくSのみならP0全体はBLOCKED/未合格。`docs/STATE.md`とここに証拠場所を記録する。

毎回の記録例:
```
- SHA: <commit>
- Unity: <exact version, OS>
- Source import: PASS/FAIL/NOT RUN
- EditMode: X passed / Y failed / NOT RUN
- PlayMode: X passed / Y failed / NOT RUN
- Brush Lab screenshot: <path> / NOT CAPTURED
- Device: Android/iOS model + FPS + memory / NOT RUN
- Outstanding: <repro and next action>
```

WindowsのUnity実行例（Unityの具体パスを導入済みのものに置換）:
```powershell
& 'C:\Program Files\Unity\Hub\Editor\<version>\Editor\Unity.exe' -batchmode -nographics -quit -projectPath (Resolve-Path .\UnitySandbox) -logFile .\logs\unity-import.log
& 'C:\Program Files\Unity\Hub\Editor\<version>\Editor\Unity.exe' -batchmode -nographics -runTests -projectPath (Resolve-Path .\UnitySandbox) -testPlatform EditMode -testResults .\logs\editmode.xml -logFile .\logs\unity-tests.log
```
注: `-nographics` が適切でない描画テストでは外し、R用はGUI Unity Editorでテスト。ホストUnitySandboxが作成されていない状態でのコマンド成功を捏造しない。
