# 現状 — 墨脈
更新: 2026-10-23

## 現状
- Unity6000.3.26f1 / URP17.3.0の2D Rendererへ移行。Brush Lab表示とPNG互換確認済み。
- 戦場をSpriteRendererへ、戦闘HUDをuGUI/TMPへ移行。uGUIの理由: 筆枠スプライトと再利用ボタン/数値/パネルを同じCanvasで構成できる。メニューはIMGUI維持。
- 縦戦場は幅100%・高さ68%。従来の位置補間はなし（30Hz現在位置を直描画）。前/current tick補間を追加。
- 敵5種、6武器、被弾/消失、45ms表示ヒットストップ、演出軽減、SUMIの枝/撃破跡とdirty tile部分更新を実装。
- Runtime/シミュレーション/経済/保存は未変更。研究価格500案はstashに保護。長期曲線凍結・分割計画のみを維持。

## 直近の結果
- 検証区分: 限定検証（関連EditMode、PC隔離Play、前後計測/画面）。実機・新Windowsビルドは未実施。
- EditMode213/213、uGUI操作等8/8。Brush Lab PNG前後のSHA256一致。詳細: RENDER_REFRESH.md。
- 360×640、seed42/点墨/Farming、Wave17から3分: 変更前平均16.83ms/最大112.87ms、変更後平均16.72ms/最大53.05ms。Editor測定、60fps目標、GPU/実機性能保証ではない。初回準備を含んだ393msの試行は比較不採用。
- 変更後304撃破・304撃破跡、全面再構築0・計測中全面初期化0。画像: evidence/render-before-wave17.png / render-after-wave17.png。計測JSONも同接頭辞。
- Play終了、QA画面設定を復元済み。

## 次の一手
1. 添付WAVE37を美術目標に、太く広いうねる枝、敵の迫力、金/朱の光、紙札と文字を改善。現在の画面は目標に未達。
2. 新uGUIの強化購入、実機タップ/ノッチ、日本語フォント/CopyTexture対応、初回シェーダー待機を確認。撃破跡は周回内表示のみで保存しない。
3. 音の聴感と初週バランスは描画の後。商品完成条件はPRODUCT_COMPLETION.md。
