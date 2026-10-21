# Windows環境の準備とCodex移行

## ダウンロード対象（必須）
1. **Unity Hub**（公式ダウンロード: https://unity.com/download）へログイン。Unity Editorの **6.3 LTS系** をUnity Hubからインストール。厳密なパッチ番号は現時点のHubのLTS推奨版を選び、`ProjectVersion.txt`で固定。Unity 6.3 LTSは2027年12月までサポート予定。
2. **Git for Windows**（公式: https://git-scm.com/install/windows ）と **Codexの利用環境**（既に使える場合は再インストール不要）。Codexはリポジトリ作業フォルダを開く。Unity Editorへのアクセス権限があるローカルCodexがビルド確認に有利。
3. 最初はWindowsデスクトップ/Editor検証。**Android Build Support + Android SDK & NDK Tools + OpenJDK** はAndroid実機ビルド時にUnity Hubのモジュール追加画面で導入。別途個別ダウンロードしなくてよい。
4. iOSへのビルドは通常 macOS/Xcodeが必要。WindowsだけでiPhoneのネイティブ実機ビルドを完了したと見なさない。

## このZIPの投入（最短）
このスターターZIPには初版パッケージ、AGENTS.md、Codex用指示、設計書が一体化している。**既にGitHubにREADME等が存在するときは履歴を消さずに同期**。

### 手作業で安全に入れる場合
```powershell
# 作業したいフォルダで実行
 git clone https://github.com/kntnext-hash/sumi-engine.git
# ダウンロードしたSUMI_ENGINE_Codex_Starter.zipを展開し、展開フォルダの中身をsumi-engine直下へコピー（.gitは上書きしない）。
 cd .\sumi-engine
 git status -sb
 git add .
 git commit -m "Bootstrap SUMI ENGINE v0.1 with Codex handoff"
 git push -u origin main
```
- GitHub認証でログインが必要なら表示された案内に従う。公開範囲は変更しない。
- pushできない場合でもCodexのローカル作業はできる。`git status` と変更差分を保管する。
- ZIP内 `package.json` が **sumi-engine直下** にあること。`com.connectpiece.sumi-engine/com.connectpiece.sumi-engine/package.json` のような二重階層は不可。

### Unityについて
Codexは `UnitySandbox` のUnityプロジェクトを自動生成/構成し、同じRepoルートのUPMパッケージを参照させる。必要ならUnity HubでBlank 2Dの `UnitySandbox` を先に作る。**Unity Sandboxを別Gitリポジトリにしない**。実験シーンとEditor Testの両方が使えることを確認する。

## GitHub URLから他プロジェクトへ導入する将来の形
Unity Package Manager `+` → `Install package from git URL` →
```
https://github.com/kntnext-hash/sumi-engine.git
```
レポジトリ直下に `package.json` があるため `?path=` は不要。バージョンをロックする際はGitタグまたはコミットSHAで指定。初版をpushするまでGit URLインストールは機能しない。

## 最初にやること
1. スターターZIPの内容をGitHubに同期。
2. Codexで `kntnext-hash/sumi-engine` を開き `START_CODEX.md` の文面を貼る。
3. CodexにUnity検出/コンパイル/テスト/BrushLab実描画まで実装させる。
4. 次の機能は `docs/TASKS.md` のゲート順に進行し、`docs/STATE.md` で継続する。
