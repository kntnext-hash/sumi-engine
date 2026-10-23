using System;
using System.Collections.Generic;
using Bokumyaku.Simulation;
using Bokumyaku.Persistence;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Bokumyaku.Presentation
{
    /// <summary>Retained, safe-area menu UI. Purchases and persistence remain owned by BokumyakuGame.</summary>
    public sealed class BokumyakuMenuHud : MonoBehaviour
    {
        readonly Color ink = new Color(.055f,.045f,.03f), gold = new Color(.78f,.62f,.32f), ivory = new Color(.96f,.93f,.84f);
        readonly List<Sprite> sprites = new List<Sprite>();
        readonly List<Action> bindings = new List<Action>();
        readonly List<TextMeshProUGUI> historyLabels = new List<TextMeshProUGUI>();
        BokumyakuGame game; TMP_FontAsset font; Canvas canvas; RectTransform safe;
        Sprite paperSprite, frameSprite, landscapeSprite, buttonFrame; GameObject ownedEventSystem, confirmation;
        readonly GameObject[] pages = new GameObject[3]; readonly ScrollRect[] scrolls = new ScrollRect[3];
        RectTransform recordsContent; TextMeshProUGUI summary, notice, warning, recordsSummary;
        CampaignProgress snapshot; BokumyakuGame.GamePage lastPage = (BokumyakuGame.GamePage)(-1);
        Rect lastSafe; int lastWidth, lastHeight; float refreshAt;
        static readonly string[] Weapons = {"点墨","貫筆","滲墨","払い","円相","共鳴"};
        static readonly string[] WeaponNotes = {"単体へ素早く連射。狙った敵を一体ずつ削る。","直線上の最大3体を貫く払い。攻撃間隔は長い。","紙面に残る範囲攻撃。速い敵は範囲から抜けやすい。","前方へ扇状に払う。まとまった敵を迎え撃つ。","末端を囲む環状攻撃。至近距離には当たらない。","近くの敵へ連鎖する墨撃。敵の集まりを狙う。"};
        static readonly string[] Builds = {"手動","採墨・周回","供給・記録"};
        static readonly string[] Policies = {"均衡","迎撃","集中"};
        static readonly string[] MetaNames = {"祖墨","深根","生脈","豊穣"};
        static readonly string[] MetaNotes = {"基礎攻撃力を永続強化","基礎流墨を永続強化","枝の成長を早める","撃破で得る墨を増やす"};

        public void Initialize(BokumyakuGame owner, Texture2D paper, Texture2D ring, Texture2D landscape)
        {
            if(game != null) return; game=owner;
            font=Resources.Load<TMP_FontAsset>("BokumyakuFonts/BattleJapanese");
            if(font==null) throw new InvalidOperationException("Bundled Japanese font is missing.");
            canvas=gameObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=110;
            var scaler=gameObject.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(440,960);scaler.matchWidthOrHeight=0;
            gameObject.AddComponent<GraphicRaycaster>();
            if(FindFirstObjectByType<EventSystem>()==null) ownedEventSystem=new GameObject("BokumyakuMenuEventSystem",typeof(EventSystem),typeof(StandaloneInputModule));
            paperSprite=SpriteFor(paper,false);frameSprite=SpriteFor(ring,true);landscapeSprite=SpriteFor(landscape,false);
            buttonFrame=Resources.Load<Sprite>("BokumyakuArt/button_ink_frame_03");
            var background=BokumyakuUiParts.Panel(transform,"MenuPaper",Vector2.zero,Vector2.one,ivory);background.sprite=paperSprite;
            safe=BokumyakuUiParts.Rect(transform,"MenuSafeArea",Vector2.zero,Vector2.one);
            var header=Box(safe,"MenuHeader",0,0,1,100);
            var title=Text(header,"Title",0,0,1,46,24,"墨 脈 — BOKUMYAKU");title.alignment=TextAlignmentOptions.Center;
            summary=Text(header,"Summary",0,48,1,38,15,"");summary.alignment=TextAlignmentOptions.Center;
            string[] tabs={"ホーム","研究","記録"};
            for(int i=0;i<3;i++){int n=i;var b=Button(safe,"MenuTab"+i,i/3f,108,(i+1)/3f,48,tabs[i],()=>game.SetPage(n==0?BokumyakuGame.GamePage.Home:n==1?BokumyakuGame.GamePage.Research:BokumyakuGame.GamePage.Records));bindings.Add(()=>Tint(b,PageIndex()==n));}
            for(int i=0;i<3;i++) CreateScroll(i);
            BuildHome();BuildResearch();BuildRecords();
            var footer=BokumyakuUiParts.Panel(safe,"MenuFooter",new Vector2(0,0),new Vector2(1,0),ink);footer.rectTransform.pivot=new Vector2(.5f,0);footer.rectTransform.sizeDelta=new Vector2(0,100);
            notice=BokumyakuUiParts.Label(footer.transform,"Notice",font,new Vector2(.03f,.49f),new Vector2(.97f,.98f),13,ivory);notice.textWrappingMode=TextWrappingModes.Normal;
            warning=BokumyakuUiParts.Label(footer.transform,"Warning",font,new Vector2(.03f,.02f),new Vector2(.7f,.49f),12,gold);warning.textWrappingMode=TextWrappingModes.Normal;
            var retry=BokumyakuUiParts.Button(footer.transform,"MenuSaveRetry",font,new Vector2(.72f,.02f),new Vector2(.99f,.48f),"再試行",()=>Act(()=>{if(game.MenuLoadBlocked)game.TryReloadProgress();else game.SaveProgress();}),out var retryText);
            bindings.Add(()=>{retry.gameObject.SetActive(!string.IsNullOrEmpty(game.MenuSaveWarning));retryText.text=game.MenuLoadBlocked?"読込再試行":"保存再試行";});
            BuildConfirmation();game.ProgressChanged+=Refresh;Refresh();
        }
        int PageIndex()=>game.Page==BokumyakuGame.GamePage.Research?1:game.Page==BokumyakuGame.GamePage.Records?2:0;
        void CreateScroll(int index)
        {
            var root=BokumyakuUiParts.Rect(safe,"MenuPage"+index,Vector2.zero,Vector2.one);root.offsetMin=new Vector2(12,108);root.offsetMax=new Vector2(-12,-166);pages[index]=root.gameObject;
            var scroll=root.gameObject.AddComponent<ScrollRect>();scroll.horizontal=false;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=35;scrolls[index]=scroll;
            var view=BokumyakuUiParts.Rect(root,"Viewport",Vector2.zero,Vector2.one);view.gameObject.AddComponent<RectMask2D>();var catchDrag=view.gameObject.AddComponent<Image>();catchDrag.color=new Color(0,0,0,.001f);
            var content=BokumyakuUiParts.Rect(view,"Content",new Vector2(0,1),Vector2.one);content.pivot=new Vector2(.5f,1);content.sizeDelta=new Vector2(0,100);scroll.viewport=view;scroll.content=content;
            view.offsetMax=new Vector2(-16,0);
            var track=BokumyakuUiParts.Panel(root,"ScrollTrack",new Vector2(1,0),Vector2.one,new Color(.12f,.09f,.04f,.14f));
            track.rectTransform.offsetMin=new Vector2(-12,0);track.rectTransform.offsetMax=Vector2.zero;track.raycastTarget=true;
            var handle=BokumyakuUiParts.Panel(track.transform,"ScrollHandle",Vector2.zero,Vector2.one,gold);
            handle.raycastTarget=true;
            var scrollbar=track.gameObject.AddComponent<Scrollbar>();scrollbar.direction=Scrollbar.Direction.BottomToTop;
            scrollbar.handleRect=handle.rectTransform;scrollbar.targetGraphic=handle;
            scroll.verticalScrollbar=scrollbar;scroll.verticalScrollbarVisibility=ScrollRect.ScrollbarVisibility.AutoHide;
        }
        RectTransform At(Transform parent,string name,float left,float top,float right,float height)
        {
            var r=BokumyakuUiParts.Rect(parent,name,new Vector2(left,1),new Vector2(right,1));r.pivot=new Vector2(.5f,1);r.offsetMin=new Vector2(4,-top-height);r.offsetMax=new Vector2(-4,-top);return r;
        }
        RectTransform Box(Transform parent,string name,float left,float top,float right,float height)
        {
            var r=At(parent,name,left,top,right,height);var image=r.gameObject.AddComponent<Image>();image.sprite=paperSprite;image.color=ivory;image.raycastTarget=false;
            var frame=BokumyakuUiParts.Panel(r,"BrushFrame",Vector2.zero,Vector2.one,ink);frame.sprite=frameSprite;frame.type=Image.Type.Sliced;frame.pixelsPerUnitMultiplier=3.5f;return r;
        }
        TextMeshProUGUI Text(Transform parent,string name,float left,float top,float right,float height,float size,string text)
        {
            var r=At(parent,name,left,top,right,height);var label=BokumyakuUiParts.Label(r,"Text",font,Vector2.zero,Vector2.one,size,ink);label.text=text;label.alignment=TextAlignmentOptions.MidlineLeft;label.textWrappingMode=TextWrappingModes.Normal;label.enableAutoSizing=true;label.fontSizeMin=size*.85f;label.fontSizeMax=size;return label;
        }
        Button Button(Transform parent,string name,float left,float top,float right,float height,string caption,Action action)
        {
            var r=At(parent,name+"Layout",left,top,right,height);var b=BokumyakuUiParts.Button(r,name,font,Vector2.zero,Vector2.one,caption,()=>Act(action),out var label);label.fontSize=17;label.fontSizeMin=14.45f;label.fontSizeMax=17;
            ((Image)b.targetGraphic).sprite=paperSprite;((Image)b.targetGraphic).color=ivory;label.color=ink;
            var edge=BokumyakuUiParts.Panel(b.transform,"BrushFrame",Vector2.zero,Vector2.one,Color.white);edge.sprite=buttonFrame??frameSprite;edge.type=Image.Type.Sliced;edge.fillCenter=false;edge.pixelsPerUnitMultiplier=24f;edge.transform.SetAsFirstSibling();return b;
        }
        void Act(Action action){game.MenuClick();action();Refresh();}
        void Tint(Button button,bool selected){((Image)button.targetGraphic).color=selected?new Color(.82f,.66f,.36f):ivory;}
        void BuildHome()
        {
            var p=scrolls[0].content;float y=0;
            var hero=Box(p,"HomeHero",0,y,1,168);var scenery=BokumyakuUiParts.Panel(hero,"Landscape",Vector2.zero,Vector2.one,new Color(1,1,1,.16f));scenery.sprite=landscapeSprite;scenery.preserveAspect=true;Text(hero,"HomeTagline",.04f,8,.96f,42,22,"一滴から、命が芽吹く。");
            var start=Button(hero,"MenuStart",.04f,60,.96f,56,"出撃する",()=>{if(game.ResumeAvailable)game.ResumeRun();else game.TryStartRun(game.AwaitingStart||game.Run==null?game.InitialSeed:unchecked(game.Run.Seed+1));});
            var startCaption=start.GetComponentInChildren<TextMeshProUGUI>();bindings.Add(()=>{startCaption.text=game.ResumeAvailable?"戦場へ戻る":"出撃する";start.interactable=!game.BattleStorageBlocked;});
            var abandon=Button(hero,"MenuAbandon",.04f,119,.96f,44,"途中終了・帰還",()=>confirmation.SetActive(true));bindings.Add(()=>abandon.gameObject.SetActive(game.ResumeAvailable));y+=180;
            Text(p,"PlayStyleTitle",0,y,1,34,21,"今回の目的");y+=40;
            for(int i=0;i<2;i++)
            {
                bool record=i==1;
                var b=Button(p,record?"MenuRecordStyle":"MenuFarmStyle",0,y,1,68,
                    record?"記録に挑む  ―  円相・供給\n最高Waveと次の段位を目指す":"周回して育てる  ―  点墨・採墨\n結晶を集めて恒久研究へ",()=>SelectPlayStyle(record));
                b.GetComponentInChildren<TextMeshProUGUI>().fontSize=17;
                bindings.Add(()=>{b.interactable=snapshot!=null&&!game.ResumeAvailable&&!game.BattleStorageBlocked;
                    Tint(b,game.SelectedWeapon==(record?WeaponKind.Enso:WeaponKind.Point)&&game.SelectedBuild==(record?BuildPreset.Record:BuildPreset.Farming));});y+=76;
            }
            Text(p,"PlayStyleNote",0,y,1,62,14,"武器と投資方針をまとめて選択。\n枝の成長方針や自動周回は下で調整できます。");y+=70;
            Text(p,"TierTitle",0,y,1,34,21,"挑む段位");y+=40;
            for(int i=1;i<=TierBalance.Count;i++)
            {
                int tier=i;var b=Button(p,"MenuTier"+tier,0,y,1,76,"",()=>game.SelectTier(tier));
                var caption=b.GetComponentInChildren<TextMeshProUGUI>();caption.fontSize=18;
                bindings.Add(()=>{bool unlocked=snapshot!=null&&TierBalance.IsUnlocked(tier,snapshot.BestWaveByTier);var d=TierBalance.Get(tier);
                    caption.text=d.DisplayName+"  結晶 ×"+d.CrystalMultiplier.ToString("0.0")+"\n"+(unlocked?"最高 "+snapshot.BestWaveByTier[tier-1]+" 波":"前の段位で100波突破すると解放");
                    Tint(b,game.SelectedTier==tier);b.interactable=unlocked&&!snapshot.HasActiveRun&&!game.BattleStorageBlocked;});y+=84;
            }
            Text(p,"TierEnemies",0,y,1,78,15,"二段以降：吸生種は核に取りついて生命を吸う。\n護輪種は金の輪が薄れる間が攻撃の好機。");y+=86;
            Text(p,"WeaponsTitle",0,y,1,34,21,"六つの武器");y+=40;
            for(int i=0;i<6;i++){int n=i;var b=Button(p,"MenuWeapon"+i,(i%3)/3f,y+(i/3)*62,(i%3+1)/3f,58,Weapons[i],()=>game.SetWeapon((WeaponKind)n));bindings.Add(()=>{Tint(b,(int)game.SelectedWeapon==n);b.interactable=!game.BattleStorageBlocked;});}y+=128;
            var weaponNote=Text(p,"WeaponDescription",0,y,1,66,16,"");bindings.Add(()=>weaponNote.text=WeaponNotes[(int)game.SelectedWeapon]);y+=76;
            Text(p,"BuildTitle",0,y,1,34,21,"墨の投資方針");y+=40;
            for(int i=0;i<3;i++){int n=i;var b=Button(p,"MenuBuild"+i,i/3f,y,(i+1)/3f,54,Builds[i],()=>game.SetBuild((BuildPreset)n));bindings.Add(()=>{Tint(b,(int)game.SelectedBuild==n);b.interactable=!game.BattleStorageBlocked;});}y+=62;
            var buildNote=Text(p,"BuildDescription",0,y,1,82,16,"");bindings.Add(()=>buildNote.text=game.SelectedBuildDescription);y+=90;
            Text(p,"PolicyTitle",0,y,1,34,21,"枝の成長方針");y+=40;
            for(int i=0;i<3;i++){int n=i;var b=Button(p,"MenuPolicy"+i,i/3f,y,(i+1)/3f,54,Policies[i],()=>game.SetPolicy((GrowthPolicy)n));bindings.Add(()=>{Tint(b,(int)game.SelectedPolicy==n);b.interactable=!game.BattleStorageBlocked;});}y+=66;
            Text(p,"SlotsTitle",0,y,1,34,21,"構成を保存・呼出");y+=40;
            for(int i=0;i<3;i++){int n=i;var row=Box(p,"Slot"+i,0,y,1,105);var label=Text(row,"SlotDescription",.03f,5,.97f,39,15,"");bindings.Add(()=>{var s=snapshot==null?null:snapshot.Slots[n];label.text="構成"+(n+1)+(s==null?"": "  "+Weapons[(int)s.Weapon]+" / "+Builds[(int)s.Build]+" / "+Policies[(int)s.Policy]);});var save=Button(row,"MenuSlotSave"+i,.03f,47,.49f,50,"保存",()=>game.SaveBuildSlot(n));var load=Button(row,"MenuSlotLoad"+i,.51f,47,.97f,50,"呼出",()=>game.LoadBuildSlot(n));bindings.Add(()=>{save.interactable=load.interactable=snapshot!=null&&!game.BattleStorageBlocked;});y+=113;}
            var repeat=Button(p,"MenuAutoRepeat",0,y,1,54,"",()=>game.SetAutoRepeat(!game.AutoRepeatEnabled));var repeatLabel=repeat.GetComponentInChildren<TextMeshProUGUI>();bindings.Add(()=>repeatLabel.text=game.AutoRepeatEnabled?"自動周回 ON":"自動周回 OFF");y+=66;
            var speed=Button(p,"MenuSpeed",0,y,1,54,"",()=>game.SetSpeed(game.Speed==1?2:1));var speedLabel=speed.GetComponentInChildren<TextMeshProUGUI>();bindings.Add(()=>speedLabel.text="速度 "+game.Speed+"倍");y+=62;
            var session=Text(p,"SessionSummary",0,y,1,54,15,"");bindings.Add(()=>session.text="この起動中 "+game.CompletedRuns+" 周 / "+game.SessionCrystals+" 結晶");y+=62;
            Text(p,"AudioTitle",0,y,1,34,21,"音と演出の設定");y+=40;
            for(int i=0;i<4;i++){int n=i;var b=Button(p,"MenuAudio"+i,0,y,1,54,"",()=>ChangeAudio(n));var label=b.GetComponentInChildren<TextMeshProUGUI>();bindings.Add(()=>{var a=game.AudioSettings;label.text=n==0?(a.Muted?"消音中 · 音を戻す":"音あり · 消音する"):(n==1?"全体 ":n==2?"BGM ":"効果音 ")+Mathf.RoundToInt((n==1?a.Master:n==2?a.Music:a.Effects)*100)+"%";});y+=62;}
            var motion=Button(p,"MenuMotion",0,y,1,54,"",()=>game.SetReducedMotion(!game.ReducedMotion));var ml=motion.GetComponentInChildren<TextMeshProUGUI>();bindings.Add(()=>ml.text=game.ReducedMotion?"演出軽減 ON":"演出軽減 OFF");y+=62;
            var aw=Text(p,"AudioWarning",0,y,1,64,14,"");bindings.Add(()=>aw.text=game.AudioSettingsWarning);y+=68;
            Button(p,"MenuSave",0,y,1,54,"保存する",()=>game.SaveProgress());y+=66;p.sizeDelta=new Vector2(0,y);
        }
        void ChangeAudio(int channel){var a=game.AudioSettings;float Next(float v)=>v>=.99f?0:Mathf.Min(1,(Mathf.Floor(v*4)+1)/4);game.SetAudioSettings(channel==1?Next(a.Master):a.Master,channel==2?Next(a.Music):a.Music,channel==3?Next(a.Effects):a.Effects,channel==0?!a.Muted:a.Muted);}
        void SelectPlayStyle(bool record)
        {
            if(game.ResumeAvailable||game.BattleStorageBlocked)return;
            game.SetWeapon(record?WeaponKind.Enso:WeaponKind.Point);
            game.SetBuild(record?BuildPreset.Record:BuildPreset.Farming);
        }
        void BuildResearch()
        {
            var p=scrolls[1].content;Text(p,"ResearchTitle",0,0,1,42,25,"命をつなぐ研究");var note=Text(p,"ResearchNote",0,48,1,70,16,"");bindings.Add(()=>note.text=game.ResumeAvailable?"途中の周回があります。研究は周回を終えてから購入できます。":"周回の合間に購入。次の出撃から永続的に働く。");
            for(int i=0;i<4;i++){int n=i;var row=Box(p,"ResearchCard"+i,0,130+i*170,1,158);var title=Text(row,"Name",.04f,8,.96f,35,22,"");var detail=Text(row,"Description",.04f,47,.96f,40,15,"");var b=Button(row,"MenuResearch"+i,.04f,96,.96f,52,"",()=>game.BuyMeta((MetaUpgrade)n));var price=b.GetComponentInChildren<TextMeshProUGUI>();bindings.Add(()=>{var k=(MetaUpgrade)n;bool capped=game.Meta.Level(k)>=MetaProgress.MaxLevel(k);title.text=MetaNames[n]+"  Lv."+game.Meta.Level(k)+" / "+MetaProgress.MaxLevel(k);detail.text=n==0?"攻撃 ×"+game.Meta.DamageMultiplier.ToString("0.00")+(capped?"":" → ×"+new MetaProgress{Damage=game.Meta.Damage+1}.DamageMultiplier.ToString("0.00")):MetaNotes[n];price.text=capped?"MAX":game.Meta.Cost(k)+" 結晶";b.interactable=snapshot!=null&&!snapshot.HasActiveRun&&!game.BattleStorageBlocked&&!capped&&game.Meta.Crystals>=game.Meta.Cost(k);});}p.sizeDelta=new Vector2(0,820);
        }
        void BuildRecords(){recordsContent=scrolls[2].content;Text(recordsContent,"RecordsTitle",0,0,1,42,25,"これまでの墨脈");recordsSummary=Text(recordsContent,"RecordsSummary",0,48,1,190,17,"");}
        public void Refresh()
        {
            if(game==null)return;snapshot=game.Campaign;summary.text="結晶 "+game.Meta.Crystals+"   ／   最高 "+game.BestWave+" 波";
            notice.text=game.BattleNotice;warning.text=game.MenuSaveWarning;foreach(var bind in bindings)bind();
            var history=snapshot?.History;int count=history==null?0:history.Count;
            recordsSummary.text="累計 "+(snapshot?.TotalRuns??0)+" 周 / "+(snapshot?.TotalCrystals??0)+" 結晶";
            for(int tier=1;tier<=TierBalance.Count;tier++)recordsSummary.text+="\n"+TierBalance.Get(tier).DisplayName+"：最高 "+(snapshot?.BestWaveByTier[tier-1]??0)+" 波";
            for(int i=historyLabels.Count;i<count;i++){var box=Box(recordsContent,"HistoryCard"+i,0,252+i*112,1,104);historyLabels.Add(Text(box,"Result",.03f,4,.97f,96,15,""));}
            for(int i=0;i<historyLabels.Count;i++){historyLabels[i].transform.parent.parent.gameObject.SetActive(i<count);if(i>=count)continue;var r=history[count-1-i];historyLabels[i].text=TierBalance.Get(r.Tier).DisplayName+" / "+r.CompletedWave+" 波 / "+Weapons[(int)r.Weapon]+" / "+Builds[(int)r.Build]+"\n結晶 "+r.Crystals+" + 初達成 "+r.MilestoneCrystals+"\n"+(r.Ticks/1800)+":"+((r.Ticks/30)%60).ToString("00")+" / Seed "+r.Seed+" / 規則"+r.RulesVersion;}
            recordsContent.sizeDelta=new Vector2(0,258+count*112);RefreshPage();
        }
        void RefreshPage(){bool visible=game.Page!=BokumyakuGame.GamePage.Battle;canvas.enabled=visible;int page=PageIndex();for(int i=0;i<pages.Length;i++)pages[i].SetActive(visible&&i==page);if(lastPage!=game.Page){confirmation.SetActive(false);if(visible){scrolls[page].StopMovement();scrolls[page].content.anchoredPosition=Vector2.zero;scrolls[page].verticalNormalizedPosition=1;}lastPage=game.Page;}}
        void BuildConfirmation(){var veil=BokumyakuUiParts.Panel(safe,"AbandonConfirmation",Vector2.zero,Vector2.one,new Color(0,0,0,.88f));veil.raycastTarget=true;confirmation=veil.gameObject;var panel=BokumyakuUiParts.Panel(veil.transform,"Paper",new Vector2(.04f,.32f),new Vector2(.96f,.69f),ivory);panel.sprite=paperSprite;var text=BokumyakuUiParts.Label(panel.transform,"Question",font,new Vector2(.06f,.48f),new Vector2(.94f,.94f),22,ink);text.text="途中の周回を終了しますか？\n報酬はありません。";text.textWrappingMode=TextWrappingModes.Normal;BokumyakuUiParts.Button(panel.transform,"ConfirmAbandon",font,new Vector2(.04f,.08f),new Vector2(.49f,.38f),"終了する",()=>Act(()=>{game.AbandonRun();confirmation.SetActive(false);}),out _);BokumyakuUiParts.Button(panel.transform,"CancelAbandon",font,new Vector2(.51f,.08f),new Vector2(.96f,.38f),"戻る",()=>confirmation.SetActive(false),out _);confirmation.SetActive(false);}
        void Update(){if(game==null)return;if(lastSafe!=Screen.safeArea||lastWidth!=Screen.width||lastHeight!=Screen.height){lastSafe=Screen.safeArea;lastWidth=Screen.width;lastHeight=Screen.height;safe.anchorMin=new Vector2(lastSafe.xMin/Screen.width,lastSafe.yMin/Screen.height);safe.anchorMax=new Vector2(lastSafe.xMax/Screen.width,lastSafe.yMax/Screen.height);}if(lastPage!=game.Page)Refresh();if(game.Page!=BokumyakuGame.GamePage.Battle&&Time.unscaledTime>=refreshAt){refreshAt=Time.unscaledTime+.2f;notice.text=game.BattleNotice;warning.text=game.MenuSaveWarning;foreach(var bind in bindings)bind();}}
        Sprite SpriteFor(Texture2D texture,bool sliced){if(texture==null)return null;var s=Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect,sliced?new Vector4(texture.width*.2f,texture.height*.2f,texture.width*.2f,texture.height*.2f):Vector4.zero);sprites.Add(s);return s;}
        void OnDestroy(){if(game!=null)game.ProgressChanged-=Refresh;foreach(var s in sprites)if(s!=null)Destroy(s);if(ownedEventSystem!=null)Destroy(ownedEventSystem);}
    }
}



