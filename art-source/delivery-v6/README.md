# delivery-v6 — 黒墨HUDパネル

2026-10-24 JST。新規1回、修正0回、有限1候補。hud_ink_panel_06.pngが原本。既存画像は未変更。

## 由来・原本
- imagegenスキル / built-in image_gen.imagegen、transparent_background=true。モデル番号非公開。PROMPT.txtに実プロンプト。
- 生成元: $CODEX_HOME/generated_images/01a16134-e305-7543-954d-eb9c2c8c17c7/exec-5a5ef096-800d-4b8f-92e7-249839127d64.png。原本を画素変更せずコピー。
- docs/evidence/player-battle-budget-v14.pngとC:/Users/che22/Desktop/墨絵インクバトルWAVE37.pngを目視比較。矩形HUDを、暗い読み取り面・掠れた外周・細い鈍金線へ寄せる新規構図。参考ファイルの画像入力/切抜き/文字・アイコン転用なし。
- PNG RGBA、1774×887（2:1）、1,508,727 bytes。目標1536×768とは寸法不一致、比率一致。
- SHA256: BFA3D1E60B8B986B0B5BC0246DA31C50D897C90DDDD62FD686529A8966EE50D7

## 検査
Inspect-Asset.ps1を実行、inspection-final.jsonへ全画素走査を保存。完全透明546,343画素、半透明1,027,014、不透明181。中央セルx260..1513/y230..656は535,458画素すべてalpha>0、alpha252〜254/255、RGB各成分の最大26/255。中心alpha253。内側はほぼ不透明な暗い墨で、完全不透明とは異なる。

a>0境界x0..1759/y0..886。微小alphaが原本端へ達し、指示した外周の完全透明余白は厳密には未達。主な筆形の見える端切れは認めず。必要なら統合時にClamp/Atlasの余白を考慮する。原本をトリミング/背景除去していない。

preview-360.pngは360px幅キャンバスへSystem.Drawingで九分割表示した検査画像。344×40の数値帯、160×48の短い表示、344×144の通知面で目視。中央は黒墨、金縁は細く、辺のつながりは維持。細かな掠れは小サイズで弱まる。文字・アイコン・和紙背景は原本に含まない。検査画像の英語注記/背景色は原本外。

## 九分割の初期値
slice-settings.json参照。原本pxでL260/B230/R260/T230、Image.Type=Sliced、**Fill Center=true**。白tint/alpha1で黒墨と金の色を保持。今回の中央は塗り面なので、過去の枠専用素材と違い中央非描画にはしない。

プレビューでは隅14×12px固定。金縁と文字を離す初期目安は左右18/上下12px。これは字体・サイズごとの文字収まり保証ではない。40px高の帯は小さい一行用、複数行は高さを増やす。import縮小時はBorder値も同率換算し、Canvas/PPUに応じて隅の表示尺度を調整する。

## 限界
Unity実9-slice/文字を重ねた実画面/入力/実機/性能は未検証。黒いパネルなので明るい文字色が前提。ごく薄い透過があるため背後の高輝度効果上で読めるか確認する。9-slice伸長では中央の微細な質感が伸びる。候補納品、最終採用は統合担当。商用権利の法的保証はしていない。

書込みはart-source/delivery-v6/とdocs/ART_DELIVERY.md追記だけ。Unity/ゲームコード/Resources/共通STATE/Git操作なし。
