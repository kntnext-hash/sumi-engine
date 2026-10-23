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
        public static RectTransform RoundPlate(Transform parent,string name,Vector2 min,Vector2 max,Sprite rim,Color rimColor)
        {
            var area=Rect(parent,name+"Area",min,max);
            var plate=Panel(area,name,Vector2.zero,Vector2.one,Color.clear);
            var aspect=plate.gameObject.AddComponent<UnityEngine.UI.AspectRatioFitter>();
            aspect.aspectMode=UnityEngine.UI.AspectRatioFitter.AspectMode.FitInParent;aspect.aspectRatio=1;
            var disc=Rect(plate.transform,"InkBacking",new Vector2(.09f,.09f),new Vector2(.91f,.91f)).gameObject.AddComponent<Drawing.UiDiscGraphic>();
            disc.color=new Color(.025f,.023f,.02f,1);disc.raycastTarget=false;
            var brush=Panel(plate.transform,"BrushRim",Vector2.zero,Vector2.one,rimColor);
            brush.sprite=rim;brush.preserveAspect=true;
            return plate.rectTransform;
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
        GameObject toolsPanel;
        bool toolsOpen;
        TextMeshProUGUI repairStatus;
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
            var top = BokumyakuUiParts.Rect(safe,"Header",new Vector2(0,.92f),Vector2.one); var bottom = Panel(safe,"Footer",0,0,1,.30f);
            var ringSprite = SpriteFor(ring); var paperSprite = SpriteFor(paper);
            var brushFrameSprite = FrameSpriteFor(ring);
            var cardFrameSprite = Resources.Load<Sprite>("BokumyakuArt/card_ink_gold_04");
            var filledCardSprite = Resources.Load<Sprite>("BokumyakuArt/washi_card_07");
            var wavePlate=BokumyakuUiParts.RoundPlate(top,"WaveRing",new Vector2(0,-.35f),new Vector2(.24f,1),ringSprite,ivory);
            Label(wavePlate,"WaveTitle",.16f,.68f,.84f,.90f,11,ivory).text="WAVE";
            wave=Label(wavePlate,"Wave",.10f,.16f,.90f,.72f,34,new Color(.85f,.14f,.09f));wave.fontSizeMin=12;
            var resources=Panel(top,"ResourcePlate",.25f,.38f,.85f,.98f);
            BokumyakuUiParts.Panel(resources.transform,"InkBacking",Vector2.zero,Vector2.one,black,1);
            var inkTitle=Label(resources.transform,"InkTitle",.02f,.60f,.49f,.98f,10,gold);inkTitle.text="墨";
            var crystalTitle=Label(resources.transform,"CrystalTitle",.51f,.60f,.98f,.98f,10,gold);crystalTitle.text="結晶";
            inkTitle.rectTransform.offsetMin=inkTitle.rectTransform.offsetMax=Vector2.zero;
            crystalTitle.rectTransform.offsetMin=crystalTitle.rectTransform.offsetMax=Vector2.zero;
            ink=Label(resources.transform,"Ink",.02f,.03f,.49f,.67f,21,ivory);ink.fontSizeMin=10;
            crystals=Label(resources.transform,"Crystals",.51f,.03f,.98f,.67f,21,ivory);crystals.fontSizeMin=10;
            var rankPlate=Panel(top,"RankPlate",.25f,.01f,.85f,.37f);
            BokumyakuUiParts.Panel(rankPlate.transform,"InkBacking",Vector2.zero,Vector2.one,black,1);
            rank=Label(rankPlate.transform,"Brand",.02f,0,.98f,1,12,gold);
            var pausePlate=BokumyakuUiParts.RoundPlate(top,"Pause",new Vector2(.86f,.06f),new Vector2(1,.98f),ringSprite,ivory);
            var pauseImage=pausePlate.GetComponent<UnityEngine.UI.Image>();pauseImage.raycastTarget=true;
            var pauseButton=pausePlate.gameObject.AddComponent<UnityEngine.UI.Button>();pauseButton.targetGraphic=pausePlate.Find("BrushRim").GetComponent<UnityEngine.UI.Image>();
            pauseButton.onClick.AddListener(()=>game.SetPaused(!game.IsPaused));
            pause=Label(pausePlate,"Caption",.14f,.12f,.86f,.88f,24,ivory);pause.text="Ⅱ";
            TextMeshProUGUI ignored;
            var gaugeArea=BokumyakuUiParts.Rect(bottom.transform,"CoreGaugeArea",new Vector2(0,.74f),new Vector2(.28f,1.16f));
            repair=Button(gaugeArea,"Repair",0,0,1,1,"",()=>game.TryRepair(),out ignored);ignored.gameObject.SetActive(false);
            var aspect=repair.gameObject.AddComponent<UnityEngine.UI.AspectRatioFitter>();
            aspect.aspectMode=UnityEngine.UI.AspectRatioFitter.AspectMode.FitInParent;aspect.aspectRatio=1;
            var gaugeFace=(UnityEngine.UI.Image)repair.targetGraphic;
            gaugeFace.sprite=ringSprite;gaugeFace.type=UnityEngine.UI.Image.Type.Simple;gaugeFace.preserveAspect=true;gaugeFace.color=new Color(.32f,.25f,.14f);
            var disc=BokumyakuUiParts.Rect(repair.transform,"InkBacking",new Vector2(.09f,.09f),new Vector2(.91f,.91f)).gameObject.AddComponent<Drawing.UiDiscGraphic>();
            disc.color=black;disc.raycastTarget=false;
            hpFill=BokumyakuUiParts.Panel(repair.transform,"CoreRingFill",Vector2.zero,Vector2.one,gold);
            hpFill.sprite=ringSprite;hpFill.type=UnityEngine.UI.Image.Type.Filled;hpFill.fillMethod=UnityEngine.UI.Image.FillMethod.Radial360;
            hpFill.fillOrigin=(int)UnityEngine.UI.Image.Origin360.Top;hpFill.fillClockwise=true;hpFill.preserveAspect=true;
            Label(repair.transform,"CoreTitle",.18f,.74f,.82f,.92f,12,ivory).text="墨核";
            hp=Label(repair.transform,"CoreValue",.14f,.24f,.86f,.76f,34,ivory);
            Label(repair.transform,"CoreMaximum",.22f,.08f,.78f,.28f,13,ivory).text="/ 10";
            repairStatus=Label(bottom.transform,"RepairStatus",.30f,.74f,.99f,.85f,12,ivory);
            Button(bottom.transform,"Speed",.30f,.87f,.51f,1.10f,"×1",()=>game.SetSpeed(game.Speed==1?2:1),out speed);
            Button(bottom.transform,"Auto",.51f,.87f,.77f,1.10f,"自動",()=>game.SetAutoRepeat(!game.AutoRepeatEnabled),out auto);
            Button(bottom.transform,"BattleTools",.77f,.87f,1,1.10f,"操作",()=>{bool open=!(toolsOpen||helpOpen||extraOpen);CloseTools();toolsOpen=open;Refresh();},out ignored);
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
            // Secondary actions are hidden until requested, leaving the battlefield unobstructed.
            var tools=Panel(safe,"BattleToolsPanel",.06f,.32f,.94f,.68f);toolsPanel=tools.gameObject;tools.raycastTarget=true;
            Button(tools.transform,"Save",.04f,.69f,.49f,.96f,"保存",()=>game.SaveProgress(),out ignored);
            Button(tools.transform,"Home",.51f,.69f,.96f,.96f,"ホーム",()=>{CloseTools();game.SetPage(BokumyakuGame.GamePage.Home);},out ignored);
            Button(tools.transform,"ReducedMotion",.04f,.37f,.49f,.64f,"演出",()=>game.SetReducedMotion(!game.ReducedMotion),out motion);
            Button(tools.transform,"UpgradeHelp",.51f,.37f,.96f,.64f,"強化説明",()=>{CloseTools();helpOpen=true;Refresh();},out helpCaption);
            extraToggle=Button(tools.transform,"ExtraUpgrades",.04f,.05f,.62f,.32f,"滲み・採墨",()=>{CloseTools();extraOpen=true;Refresh();},out extraToggleLabel);
            Button(tools.transform,"CloseTools",.64f,.05f,.96f,.32f,"閉じる",()=>{CloseTools();Refresh();},out ignored);
            var extra=Panel(safe,"ExtraUpgradePanel",.06f,.32f,.94f,.68f); extraPanel=extra.gameObject;
            extra.raycastTarget=true;
            for(int i=0;i<ExtraKinds.Length;i++)
            {
                int slot=i;
                float y=i==0?.65f:.34f;
                extraCards[i]=Button(extra.transform,"Upgrade"+ExtraKinds[i],.03f,y,.97f,y+.30f,"",()=>{game.BuyUpgrade(ExtraKinds[slot]);Refresh();},out extraLabels[i]);
            }
            Button(extra.transform,"BackToTools",.03f,.04f,.97f,.30f,"戻る",()=>{CloseTools();toolsOpen=true;Refresh();},out ignored);
            extraPanel.SetActive(false);
            var help=Panel(safe,"UpgradeHelpPanel",.03f,.32f,.97f,.85f);
            helpPanel=help.gameObject; help.raycastTarget=true;
            for(int i=0;i<7;i++)
            {
                float y=1-(i+1)*.85f/7f;
                var description=Label(help.transform,"UpgradeDescription"+i,.03f,y,.97f,y+.85f/7f,17,ivory);
                description.text=BokumyakuGame.UpgradeDescription((UpgradeKind)i);
                description.textWrappingMode=TextWrappingModes.Normal;
            }
            helpPanel.SetActive(false);
            Button(help.transform,"BackToTools",.03f,.02f,.97f,.14f,"戻る",()=>{CloseTools();toolsOpen=true;Refresh();},out ignored);
            var veil=BokumyakuUiParts.Panel(safe,"ResultVeil",new Vector2(.06f,.42f),new Vector2(.94f,.69f),black); modal=veil.gameObject;
            result=Label(veil.transform,"Result",.04f,.43f,.96f,.96f,22,ivory);
            retry=Button(veil.transform,"Retry",.05f,.07f,.62f,.4f,"再出撃",()=>game.TryStartRun(unchecked(game.Run.Seed+1)),out retryLabel);
            Button(veil.transform,"ResultHome",.65f,.07f,.95f,.4f,"ホーム",()=>game.SetPage(BokumyakuGame.GamePage.Home),out ignored);
            RefreshSafeArea(); Refresh();
        }
        void CloseTools(){toolsOpen=false;helpOpen=false;extraOpen=false;}
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
            if(!canvas.enabled){CloseTools();toolsPanel.SetActive(false);extraPanel.SetActive(false);helpPanel.SetActive(false);}
            if(!canvas.enabled && extraOpen){extraOpen=false;extraPanel.SetActive(false);}
            if(!canvas.enabled && helpOpen){helpOpen=false;helpPanel.SetActive(false);}
            if(canvas.enabled && Time.unscaledTime>=nextRefresh){nextRefresh=Time.unscaledTime+.1f;Refresh();}
        }
        static void Text(TextMeshProUGUI target,string value){if(target.text!=value)target.text=value;}
        void Refresh()
        {
            var run=game.Run;if(run==null)return;Text(rank,TierBalance.Get(run.Tier).DisplayName);
            Text(wave,run.Wave.ToString());Text(ink,run.RunInk.ToString("N0"));Text(crystals,game.Meta.Crystals.ToString("N0"));Text(hp,run.CoreHp.ToString());
            Text(repairStatus,run.CoreHp>=10?"墨核 満タン":run.RepairCount>=5?"修復上限":"核を押して修復  墨 "+run.RepairCost);
            repair.interactable=!game.BattleStorageBlocked&&!game.AwaitingStart&&run.State==RunStatus.Running&&run.CoreHp<10&&run.RepairCount<5&&run.RunInk>=run.RepairCost;
            hpFill.fillAmount=Mathf.Clamp01(run.CoreHp/10f);
            hp.color=run.CoreHp<=3?new Color(.95f,.36f,.24f):ivory;
            hpFill.color=run.CoreHp<=3?new Color(.83f,.25f,.12f):gold;
            repairStatus.color=repair.interactable?gold:ivory;
            Text(pause,game.IsPaused?"▶":"Ⅱ");Text(speed,"×"+game.Speed);Text(auto,game.AutoRepeatEnabled?"自動 入":"自動 切");Text(motion,game.ReducedMotion?"演出 弱":"演出 通常");Text(notice,game.BattleNotice);
            for(int i=0;i<5;i++)
            {
                var kind=(UpgradeKind)i;int level=run.UpgradeLevels[i],cost=run.UpgradeCost(kind);bool max=level>=run.UpgradeMaxLevel(kind);
                Text(cardLevels[i],"Lv."+level);Text(cardCosts[i],max?"MAX":"<size=70%>墨</size> "+cost.ToString("N0"));
                cardCosts[i].color=run.RunInk>=cost?ivory:new Color(.84f,.79f,.68f);
                cards[i].interactable=!game.BattleStorageBlocked&&!game.AwaitingStart&&run.State==RunStatus.Running&&!max&&run.RunInk>=cost;
            }
            bool ended=run.State!=RunStatus.Running;
            if(ended||game.AwaitingStart||run!=extraRun)CloseTools();
            toolsPanel.SetActive(toolsOpen);
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




