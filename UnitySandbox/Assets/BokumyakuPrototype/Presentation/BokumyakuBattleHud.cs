using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using Bokumyaku.Simulation;

namespace Bokumyaku.Presentation
{
    /// <summary>Reusable uGUI primitives shared by battle and future menu migration.</summary>
    public static class BokumyakuUiParts
    {
        static Sprite inkPanel;
        public static void ApplyInkPanel(UnityEngine.UI.Image image)
        {
            if (inkPanel == null) inkPanel = Resources.Load<Sprite>("BokumyakuArt/hud_ink_panel_06");
            if (inkPanel == null) return;
            image.sprite = inkPanel;
            image.type = UnityEngine.UI.Image.Type.Sliced;
            image.fillCenter = true;
            image.pixelsPerUnitMultiplier = 22f;
            image.color = Color.white;
        }
        public static RectTransform Rect(Transform parent, string name, Vector2 min, Vector2 max, float inset = 0)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var r = (RectTransform)go.transform; r.SetParent(parent, false);
            r.anchorMin = min; r.anchorMax = max; r.offsetMin = Vector2.one * inset; r.offsetMax = -Vector2.one * inset;
            return r;
        }
        public static UnityEngine.UI.Image Panel(Transform parent, string name, Vector2 min, Vector2 max, Color color, float inset = 0)
        {
            var image = Rect(parent, name, min, max, inset).gameObject.AddComponent<UnityEngine.UI.Image>();
            image.color = color; image.raycastTarget = false; return image;
        }
        public static TextMeshProUGUI Label(Transform parent, string name, TMP_FontAsset font, Vector2 min, Vector2 max, float size, Color color)
        {
            var t = Rect(parent, name, min, max, 2).gameObject.AddComponent<TextMeshProUGUI>();
            t.font = font; t.fontSize = size; t.color = color; t.alignment = TextAlignmentOptions.Center;
            t.enableAutoSizing = true; t.fontSizeMin = size * .72f; t.fontSizeMax = size;
            t.raycastTarget = false; t.textWrappingMode = TextWrappingModes.NoWrap; return t;
        }
        public static UnityEngine.UI.Button Button(Transform parent, string name, TMP_FontAsset font, Vector2 min, Vector2 max, string caption, Action click, out TextMeshProUGUI label)
        {
            var image = Panel(parent, name, min, max, new Color(.06f,.05f,.035f,.98f), 2);
            ApplyInkPanel(image);
            image.raycastTarget = true;
            var b = image.gameObject.AddComponent<UnityEngine.UI.Button>(); b.targetGraphic = image;
            var c = b.colors; c.normalColor = Color.white; c.highlightedColor = new Color(1,.9f,.7f); c.pressedColor = new Color(.65f,.53f,.33f); c.disabledColor = new Color(.5f,.5f,.5f,.7f); b.colors = c;
            label = Label(b.transform, "Caption", font, Vector2.zero, Vector2.one, 15, new Color(.94f,.86f,.68f)); label.text = caption;
            b.onClick.AddListener(() => click()); return b;
        }
    }

    public sealed class BokumyakuBattleHud : MonoBehaviour
    {
        readonly Color gold = new Color(.78f,.62f,.32f), ivory = new Color(.96f,.93f,.84f), black = new Color(.035f,.032f,.026f,.98f);
        readonly List<Sprite> ownedSprites = new List<Sprite>();
        readonly UnityEngine.UI.Button[] cards = new UnityEngine.UI.Button[5];
        readonly TextMeshProUGUI[] cardLevels = new TextMeshProUGUI[5], cardCosts = new TextMeshProUGUI[5];
        BokumyakuGame game; Canvas canvas; RectTransform safe; TMP_FontAsset font; Font sourceFont;
        GameObject ownedEventSystem, modal; TextMeshProUGUI rank, wave, ink, crystals, hp, notice, speed, pause, auto, motion, result, retryLabel;
        UnityEngine.UI.Button retry,repair; UnityEngine.UI.Image hpFill; float nextRefresh; Rect lastSafe; int lastWidth, lastHeight;
        UnityEngine.UI.Button extraToggle;
        readonly UnityEngine.UI.Button[] extraCards = new UnityEngine.UI.Button[2];
        readonly TextMeshProUGUI[] extraLabels = new TextMeshProUGUI[2];
        GameObject extraPanel; TextMeshProUGUI extraToggleLabel; bool extraOpen;
        RunSimulator extraRun;
        GameObject helpPanel;
        TextMeshProUGUI helpCaption;
        bool helpOpen;
        static readonly UpgradeKind[] ExtraKinds = { UpgradeKind.Bleed, UpgradeKind.Harvest };
        static readonly string[] ExtraNames = { "滲み", "採墨" };
        static readonly string[] Names = { "筆撃", "筆速", "流墨", "分岐", "筆幅" };
        static readonly string[] IconNames = { "attack", "speed", "flow", "branch", "width" };

        // Camera rect uses bottom-left normalized coordinates, matching SafeArea anchors.
        public static Rect GetBattleViewport()
        {
            var s = Screen.safeArea;
            return new Rect(s.x / Screen.width, (s.y + s.height * .30f) / Screen.height, s.width / Screen.width, s.height * .62f / Screen.height);
        }
        public void Initialize(BokumyakuGame owner, Texture2D ring, Texture2D[] glyphs, Texture2D paper)
        {
            if (game != null) return; game = owner;
            canvas = gameObject.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 100;
            var scaler = gameObject.AddComponent<UnityEngine.UI.CanvasScaler>(); scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(440,780); scaler.matchWidthOrHeight = 0;
            gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() == null)
                ownedEventSystem = new GameObject("BokumyakuEventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            font=Resources.Load<TMP_FontAsset>("BokumyakuFonts/BattleJapanese");
            if(font==null)throw new InvalidOperationException("Bundled Japanese font is missing.");
            safe = BokumyakuUiParts.Rect(transform,"SafeArea",Vector2.zero,Vector2.one);
            var top = Panel(safe,"Header",0,.92f,1,1); var bottom = Panel(safe,"Footer",0,0,1,.30f);
            var ringSprite = SpriteFor(ring); var paperSprite = SpriteFor(paper);
            var brushFrameSprite = FrameSpriteFor(ring);
            var cardFrameSprite = Resources.Load<Sprite>("BokumyakuArt/card_ink_gold_04");
            var filledCardSprite = Resources.Load<Sprite>("BokumyakuArt/washi_card_07");
            if (ringSprite != null) { var image = BokumyakuUiParts.Panel(top.transform,"WaveRing",new Vector2(0,-.2f),new Vector2(.22f,1),ivory); image.sprite = ringSprite; image.preserveAspect = true; }
            Label(top.transform,"WaveTitle",0,.59f,.22f,.88f,11,ivory).text = "WAVE";
            wave = Label(top.transform,"Wave",0,.02f,.22f,.60f,29,new Color(.8f,.12f,.08f));
            ink = Label(top.transform,"Ink",.23f,.4f,.57f,1,21,ivory);
            crystals = Label(top.transform,"Crystals",.57f,.4f,.85f,1,17,gold);
            rank=Label(top.transform,"Brand",.24f,.02f,.84f,.4f,12,gold);
            Button(top.transform,"Pause",.86f,.1f,1,.9f,"Ⅱ",()=>game.SetPaused(!game.IsPaused),out pause);
            repair=Button(bottom.transform,"Repair",0,.76f,.22f,1,"",()=>game.TryRepair(),out hp);
            var track = BokumyakuUiParts.Panel(bottom.transform,"CoreTrack",new Vector2(.02f,.74f),new Vector2(.2f,.755f),new Color(.2f,.18f,.13f));
            hpFill = BokumyakuUiParts.Panel(track.transform,"Fill",Vector2.zero,Vector2.one,gold);
            Button(bottom.transform,"Speed",.23f,.75f,.38f,1,"×1",()=>game.SetSpeed(game.Speed==1?2:1),out speed);
            Button(bottom.transform,"Auto",.38f,.75f,.54f,1,"自動",()=>game.SetAutoRepeat(!game.AutoRepeatEnabled),out auto);
            TextMeshProUGUI ignored;
            Button(bottom.transform,"Save",.54f,.75f,.69f,1,"保存",()=>game.SaveProgress(),out ignored);
            Button(bottom.transform,"Home",.69f,.75f,.84f,1,"ホーム",()=>game.SetPage(BokumyakuGame.GamePage.Home),out ignored);
            Button(bottom.transform,"ReducedMotion",.84f,.75f,1,1,"演出",()=>game.SetReducedMotion(!game.ReducedMotion),out motion);
            for (int i=0;i<5;i++)
            {
                int slot=i; cards[i]=Button(bottom.transform,"Upgrade"+i,i*.2f,.12f,(i+1)*.2f,.64f,"",()=>game.BuyUpgrade((UpgradeKind)slot),out ignored); ignored.gameObject.SetActive(false);
                var face=(UnityEngine.UI.Image)cards[i].targetGraphic;
                face.sprite=filledCardSprite!=null?filledCardSprite:paperSprite;
                face.color=filledCardSprite!=null?Color.white:ivory;
                if(filledCardSprite!=null)
                {
                    face.type=UnityEngine.UI.Image.Type.Sliced;
                    face.fillCenter=true;
                    // At 360px wide, the 440px reference canvas gives approximately 12px side corners.
                    face.pixelsPerUnitMultiplier=12.25f;
                }
                var cardColors=cards[i].colors; cardColors.disabledColor=new Color(.86f,.85f,.81f,1); cardColors.highlightedColor=new Color(1,1,.94f); cards[i].colors=cardColors;
                // Keep the rectangular button hit area; the non-raycast brush frame is purely visual.
                if(filledCardSprite==null)
                {
                    var edge=BokumyakuUiParts.Panel(cards[i].transform,"BrushEdge",Vector2.zero,Vector2.one,black);
                    edge.sprite=cardFrameSprite!=null?cardFrameSprite:brushFrameSprite;
                    edge.color=cardFrameSprite!=null?Color.white:black;
                    edge.type=UnityEngine.UI.Image.Type.Sliced;
                    edge.fillCenter=false;
                    edge.pixelsPerUnitMultiplier=cardFrameSprite!=null?13f:3.5f;
                }
                var rim=BokumyakuUiParts.Panel(cards[i].transform,"GoldEmblemRim",new Vector2(.015f,.345f),new Vector2(.985f,.99f),new Color(.7f,.52f,.22f,.7f));
                rim.sprite=ringSprite; rim.preserveAspect=true;
                var emblem=BokumyakuUiParts.Panel(cards[i].transform,"Emblem",new Vector2(.04f,.35f),new Vector2(.96f,.98f),black); emblem.sprite=ringSprite; emblem.preserveAspect=true;
                var icon=Resources.Load<Sprite>("BokumyakuArt/upgrade_"+IconNames[i]+"_08");
                if(icon!=null || (glyphs!=null && i<glyphs.Length))
                {
                    var glyph=BokumyakuUiParts.Panel(emblem.transform,"Glyph",new Vector2(.12f,.12f),new Vector2(.88f,.88f),icon!=null?Color.white:black);
                    glyph.sprite=icon!=null?icon:SpriteFor(glyphs[i]);
                    glyph.type=UnityEngine.UI.Image.Type.Simple;
                    glyph.preserveAspect=true;
                }
                Label(cards[i].transform,"Name",0,.205f,1,.40f,17,black).text=Names[i];
                var price=BokumyakuUiParts.Panel(cards[i].transform,"PricePlate",new Vector2(.065f,.035f),new Vector2(.935f,.225f),black);
                cardCosts[i]=Label(price.transform,"Cost",0,0,1,1,17,ivory);
                cardCosts[i].rectTransform.offsetMin=new Vector2(1,0);
                cardCosts[i].rectTransform.offsetMax=new Vector2(-1,0);
                cardCosts[i].fontSizeMin=11;
                cardCosts[i].fontStyle=FontStyles.Bold;
                cardLevels[i]=Label(bottom.transform,"Level"+i,i*.2f,.65f,(i+1)*.2f,.73f,11,gold);
            }
            notice=Label(bottom.transform,"Notice",.015f,0,.985f,.12f,11,ivory);
            // A temporary popover keeps the five primary cards and footer controls reachable.
            extraToggle=Button(safe,"ExtraUpgrades",.66f,.855f,.99f,.915f,"滲み・採墨",()=>{extraOpen=!extraOpen;helpOpen=false;Refresh();},out extraToggleLabel);
            var extra=Panel(safe,"ExtraUpgradePanel",.39f,.675f,.99f,.85f); extraPanel=extra.gameObject;
            extra.raycastTarget=true;
            for(int i=0;i<ExtraKinds.Length;i++)
            {
                int slot=i;
                float y=i==0?.51f:.03f;
                extraCards[i]=Button(extra.transform,"Upgrade"+ExtraKinds[i],.03f,y,.97f,y+.46f,"",()=>{game.BuyUpgrade(ExtraKinds[slot]);Refresh();},out extraLabels[i]);
            }
            extraPanel.SetActive(false);
            Button(safe,"UpgradeHelp",.01f,.855f,.35f,.915f,"強化説明",()=>{helpOpen=!helpOpen;extraOpen=false;Refresh();},out helpCaption);
            var help=Panel(safe,"UpgradeHelpPanel",.03f,.32f,.97f,.85f);
            helpPanel=help.gameObject; help.raycastTarget=true;
            for(int i=0;i<7;i++)
            {
                float y=1-(i+1)/7f;
                var description=Label(help.transform,"UpgradeDescription"+i,.03f,y,.97f,y+1/7f,17,ivory);
                description.text=BokumyakuGame.UpgradeDescription((UpgradeKind)i);
                description.textWrappingMode=TextWrappingModes.Normal;
            }
            helpPanel.SetActive(false);
            var veil=BokumyakuUiParts.Panel(safe,"ResultVeil",new Vector2(.06f,.42f),new Vector2(.94f,.69f),black); modal=veil.gameObject;
            result=Label(veil.transform,"Result",.04f,.43f,.96f,.96f,22,ivory);
            retry=Button(veil.transform,"Retry",.05f,.07f,.62f,.4f,"再出撃",()=>game.TryStartRun(unchecked(game.Run.Seed+1)),out retryLabel);
            Button(veil.transform,"ResultHome",.65f,.07f,.95f,.4f,"ホーム",()=>game.SetPage(BokumyakuGame.GamePage.Home),out ignored);
            RefreshSafeArea(); Refresh();
        }
        Sprite SpriteFor(Texture2D texture)
        {
            if(texture==null)return null; var s=Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),new Vector2(.5f,.5f),100); ownedSprites.Add(s);return s;
        }
        Sprite FrameSpriteFor(Texture2D texture)
        {
            if(texture==null)return null;
            // Nine-slicing reuses the SUMI stroke's broken edge without stretching its corners.
            var border=new Vector4(texture.width*.24f,texture.height*.24f,texture.width*.24f,texture.height*.24f);
            var sprite=Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect,border);
            ownedSprites.Add(sprite);return sprite;
        }
        UnityEngine.UI.Image Panel(Transform p,string n,float x,float y,float xx,float yy)
        {
            var panel=BokumyakuUiParts.Panel(p,n,new Vector2(x,y),new Vector2(xx,yy),black);
            BokumyakuUiParts.ApplyInkPanel(panel);return panel;
        }
        TextMeshProUGUI Label(Transform p,string n,float x,float y,float xx,float yy,float size,Color color) => BokumyakuUiParts.Label(p,n,font,new Vector2(x,y),new Vector2(xx,yy),size,color);
        UnityEngine.UI.Button Button(Transform p,string n,float x,float y,float xx,float yy,string caption,Action click,out TextMeshProUGUI label) => BokumyakuUiParts.Button(p,n,font,new Vector2(x,y),new Vector2(xx,yy),caption,click,out label);
        void RefreshSafeArea()
        {
            lastSafe=Screen.safeArea;lastWidth=Screen.width;lastHeight=Screen.height;
            safe.anchorMin=new Vector2(lastSafe.x/Screen.width,lastSafe.y/Screen.height);safe.anchorMax=new Vector2(lastSafe.xMax/Screen.width,lastSafe.yMax/Screen.height);
        }
        void Update()
        {
            if(game==null)return;
            if(lastSafe!=Screen.safeArea||lastWidth!=Screen.width||lastHeight!=Screen.height)RefreshSafeArea();
            canvas.enabled=game.Page==BokumyakuGame.GamePage.Battle;
            if(!canvas.enabled && extraOpen){extraOpen=false;extraPanel.SetActive(false);}
            if(!canvas.enabled && helpOpen){helpOpen=false;helpPanel.SetActive(false);}
            if(canvas.enabled && Time.unscaledTime>=nextRefresh){nextRefresh=Time.unscaledTime+.1f;Refresh();}
        }
        static void Text(TextMeshProUGUI target,string value){if(target.text!=value)target.text=value;}
        void Refresh()
        {
            var run=game.Run;if(run==null)return;Text(rank,TierBalance.Get(run.Tier).DisplayName);
            Text(wave,run.Wave.ToString());Text(ink,"墨 "+run.RunInk);Text(crystals,"結晶 "+game.Meta.Crystals);Text(hp,"墨核 "+run.CoreHp+" / 10\n"+(run.CoreHp>=10?"満タン":run.RepairCount>=5?"修復上限":"修復 墨 "+run.RepairCost));
            repair.interactable=!game.BattleStorageBlocked&&!game.AwaitingStart&&run.State==RunStatus.Running&&run.CoreHp<10&&run.RepairCount<5&&run.RunInk>=run.RepairCost;
            hpFill.rectTransform.anchorMax=new Vector2(Mathf.Clamp01(run.CoreHp/10f),1);
            Text(pause,game.IsPaused?"▶":"Ⅱ");Text(speed,"×"+game.Speed);Text(auto,game.AutoRepeatEnabled?"自動 入":"自動 切");Text(motion,game.ReducedMotion?"演出 弱":"演出 通常");Text(notice,game.BattleNotice);
            for(int i=0;i<5;i++)
            {
                var kind=(UpgradeKind)i;int level=run.UpgradeLevels[i],cost=run.UpgradeCost(kind);bool max=level>=run.UpgradeMaxLevel(kind);
                Text(cardLevels[i],"Lv."+level);Text(cardCosts[i],max?"MAX":"<size=70%>墨</size> "+cost.ToString("N0"));
                cardCosts[i].color=run.RunInk>=cost?ivory:new Color(.84f,.79f,.68f);
                cards[i].interactable=!game.BattleStorageBlocked&&!game.AwaitingStart&&run.State==RunStatus.Running&&!max&&run.RunInk>=cost;
            }
            bool ended=run.State!=RunStatus.Running;
            if(ended||game.AwaitingStart||run!=extraRun)helpOpen=false;
            helpPanel.SetActive(helpOpen);
            Text(helpCaption,helpOpen?"説明を閉じる":"強化説明");
            if(ended||game.AwaitingStart||run!=extraRun)extraOpen=false;
            extraRun=run;
            extraToggle.interactable=!ended&&!game.AwaitingStart&&!game.BattleStorageBlocked;
            if(!extraToggle.interactable)extraOpen=false;
            Text(extraToggleLabel,extraOpen?"閉じる":"滲み・採墨");
            extraPanel.SetActive(extraOpen);
            for(int i=0;i<ExtraKinds.Length;i++)
            {
                var kind=ExtraKinds[i];int level=run.UpgradeLevels[(int)kind],cost=run.UpgradeCost(kind);
                bool max=level>=run.UpgradeMaxLevel(kind);
                Text(extraLabels[i],ExtraNames[i]+"  Lv."+level+"\n"+(max?"MAX":"墨 "+cost.ToString("N0")));
                extraCards[i].interactable=extraToggle.interactable&&!max&&run.RunInk>=cost;
            }
            modal.SetActive(ended||game.AwaitingStart);
            if(modal.activeSelf)
            {
                Text(result,game.BattleResultText);
                Text(retryLabel,game.AwaitingStart?"出撃":"再出撃");retry.interactable=!game.BattleStorageBlocked;
            }
        }
        void OnDestroy()
        {
            foreach(var s in ownedSprites)if(s!=null)Destroy(s);
            // The bundled static font is shared; its lifetime belongs to Unity Resources.
            if(sourceFont!=null)Destroy(sourceFont);if(ownedEventSystem!=null)Destroy(ownedEventSystem);
        }
    }
}




