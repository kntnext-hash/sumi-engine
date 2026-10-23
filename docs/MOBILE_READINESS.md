# スマホ検証の準備
更新2026-10-24。商品完成/ストア提出/実機合格ではない。

## 確認済みの環境
AndroidReadinessCheckをUnity6000.3.26f1のEditorで実行。BuildPipeline.IsBuildTargetSupported(Android)=false、同梱AndroidPlayer/SDK/NDK/OpenJDKなし。現在はWindows64ターゲット。証拠evidence/android-readiness.json。
プロジェクト共通設定はAutoRotation、左右横向き許可。Android仮識別子はcom.DefaultCompany.SUMIBrushLab、ARM64/IL2CPP。製品のアプリ識別子とストア署名はまだ確定していない。
ゲームのStartでApplication.isMobilePlatformの場合だけScreen.orientation=Portraitを設定。Brush LabやPCの向きを変更しない。Editorコンパイル合格、Console groundTruth compilationFailed=false/errors0。実機回転の確認は未実施。

## 次の手順
1. 使用端末がAndroid/iPhone/両方のどれかを確認。Androidなら同じUnityパッチへ公式Android Build SupportとSDK/NDK/OpenJDKを追加する。管理者操作/認証はユーザー対応が必要。
2. 一覧取得には unity editor module refresh 6000.3.26f1 を使用。現在の処理を追跡し、応答待ちを成功扱いしない。再インストールやEditor更新は行っていない。
3. モジュール導入後、AndroidReadinessCheckを再実行。Unityでビルドターゲット切替と必要アセット再インポート。検証用識別子/デバッグ署名でAPKを作り、配布用署名と区別する。
4. 端末で起動、縦向き維持、セーフエリア、ホーム/研究/記録のタッチ、6武器、バックグラウンド復帰、保存/再起動、音量/消音を確認。
5. 密集戦闘と開始直後の平均/最大フレーム時間、メモリ、発熱を端末名/OS/解像度と一緒に記録。Editor/Windowsの数値を代用しない。
6. iPhone向けはmacOS/Xcodeのビルド・署名環境も確認する。Windowsの検証だけでiOS合格にしない。

実機環境が整うまではPCで描画/操作/保存の改善を続ける。新リポジトリ作成やストア提出は行わない。
