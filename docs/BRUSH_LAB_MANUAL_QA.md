# Brush Lab / Branch Growth 手動QA

2026-10-21: この手順と生成コードは準備済み。Unity Editorが未検出のため、Unityプロジェクト・保存シーン・描画の実行結果は未取得。以下の操作後に初めてA/R証拠を記録する。

## セットアップ

1. Unity HubでUnity 6.3 LTS（6000.3系）を導入し、必要なライセンスを有効にする。
2. リポジトリ直下のPowerShellで実在するパスを指定する。

```powershell
.\scripts\Setup-UnitySandbox.ps1 -UnityEditor 'C:\Program Files\Unity\Hub\Editor\6000.3.<patch>f1\Editor\Unity.exe'
```

スクリプトは実行ファイルのバージョンを検査し、Unity自身でUnitySandboxを生成する。既存プロジェクトのバージョンが異なる場合、または未検証の同名フォルダがある場合は停止する。Editorを閉じて実行すること。既存シーンは上書きしない。

ルートはUPMパッケージのため、ホストを含むルート全体への参照を避ける。配布用Runtime/Editor/Tests等を `UnitySandbox/LocalPackages~/com.connectpiece.sumi-engine` へ同期し、manifestでローカル参照する。生成コピーは編集せず、ルートソースを編集してスクリプトを再実行する。マーカー付きコピーだけを置換し、UnitySandboxのAssetsやProjectSettingsは削除しない。元manifestは初回に `.sumi-backup` として退避する。Test Frameworkは既存manifestまたはEditor同梱のバージョンを利用する（[公式仕様](https://docs.unity.com/en-us/engine/6000.3/manual/packages-list/packages-all/pack-core/com-unity-test-framework)）。

成功後に `Assets/BrushLab/Scenes/BrushLab.unity` と `BranchGrowth.unity` が生成される。メニュー `Tools > SUMI Engine > Generate Missing Sample Scenes` からも不足分を生成できる。既存の開いているシーンと未保存編集は維持される。

## A: 自動試験

Unityを閉じて正確なEditorパスで実行する。テストXMLの失敗件数とテスト実行件数を確認し、プロセス終了だけで合格としない。

```powershell
& $unity -batchmode -nographics -projectPath "$PWD\UnitySandbox" -runTests -testPlatform EditMode -testResults "$PWD\logs\editmode.xml" -logFile "$PWD\logs\unity-tests.log"
```

`$unity` にはセットアップと同じ実行ファイルパスを設定する。Source import、XML結果、ProjectVersion.txt、packages-lock.json、コミットSHAをQA台帳に記録する。

## R: 実画面

1. UnitySandboxを開く。ConsoleをClearし、BrushLabシーンを開いてPlay。Game viewを800x1000程度に設定。紙と操作UIが表示されることを撮影する。
2. 紙上でクリック、ゆっくりした線、速い線、折れ線、紙外へのドラッグを試す。マウスを離した後に線が続かないこと、UI操作で紙に誤描画しないことを確認。
3. 3プリセットを切り替え同様の線を描く。幅、顔料、水分、掠れ、毛束数のUIと実描画の対応を記録する。半径変更を筆圧検証の代わりにしない。
4. Clearで紙面を初期化。PNGを押し、画面に表示された保存先の画像を開く。向き・色・サイズ・描画内容をGame viewと比較する。保存失敗ならエラー表示を記録。
5. Seed固定リプレイはEditModeの同一入力比較と分ける。自由な手描き2回は入力が異なるのでSeed再現性の証拠にしない。
6. Playを停止しBranchGrowthシーンを開く。Playして枝が成長する様子を開始時・成長後に撮影。戦闘/Waveが実装済みとは記録しない。
7. 各シーン終了時Console error数を記録。Unityバージョン、画面寸法、CPU/GPU、操作時間、ログと画像の所在をQA台帳に残す。

結果はPASS/FAIL/NOT RUNで項目別に記録する。実画面の未確認、モバイル実機、FPS/メモリ測定をこの手順の存在だけで合格にしない。

## 現行UI（2026-10-22）
- GameビューのScaleを1にし、Play。横長画面は右側に操作欄。
- Dense/Dry/SplitとBleed/Pool/Mist/Splatterを選び紙上をドラッグ。Pressure/Radius/Pigment/Water/Roughnessで調整。
- Stroke study / Effects studyは現在の紙を置換。先にPNGで保存する。
- Effects studyの順序は左→右、上→下: 乾筆・割れ・払い／滲み・墨だまり・飛沫／淡墨・煙・円相／枝・垂れ・渦。
- Colors +のInk/Paperで編集対象を切替。RGB、色プリセット、Display densityは既存線にも適用。PNG保存先は上部表示。
- 合成入力で新筆切替・ドラッグ・朱色・PNG保存は合格。人の全項目確認は引き続きこの手順で行う。

## v0.2受入済みの確認方法
- ローカルWindows版: Builds/BrushLab/SumiBrushLab.exe。フォルダー内の関連ファイルも必要。ZIPもBuildsに生成済み。
- 2026-10-22にGameView合成イベントで30項目を確認。これは人の物理操作結果ではない。手動手順は上記を引き続き利用する。
- Mouse taperを有効にしてDense/Dry/Splitを描くと、経路内で始筆・終筆が細くなる。微小な手ぶれはタップとして保持。Pressure0は旧3presetを含め無描画。
- プリセット変更では色/表示濃度を保持する。PNG保存前に現在の筆を終了。Open exported PNG folderで保存先を開く。
