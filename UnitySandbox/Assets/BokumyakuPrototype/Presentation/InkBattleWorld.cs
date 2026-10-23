using System;
using System.Collections.Generic;
using Bokumyaku.Simulation;
using UnityEngine;
namespace Bokumyaku.Presentation
{
 // Presentation owns interpolation and VFX clocks; never writes simulation state.
 public sealed class InkBattleWorld : IDisposable
 {
  sealed class EnemyView { public EnemyState state; public Vector2 previous,current; public double hp; public SpriteRenderer body,eye; public SpriteRenderer[] buds; public float hit,death; public bool dying,drawn; }
  sealed class Effect { public SpriteRenderer renderer; public Vector2 a,b; public float age,life,width; public int type; public Color tint; public bool flash; }
  readonly GameObject root; readonly Camera camera; readonly Material material; readonly MaterialPropertyBlock block=new MaterialPropertyBlock();
  readonly Dictionary<int,EnemyView> enemies=new Dictionary<int,EnemyView>(); readonly List<int> remove=new List<int>();
  readonly HashSet<int> liveIds=new HashSet<int>(); readonly List<SpriteRenderer> fieldViews=new List<SpriteRenderer>();
  readonly List<Effect> effects=new List<Effect>(); readonly Stack<SpriteRenderer> pool=new Stack<SpriteRenderer>();
  readonly List<Sprite> ownedSprites=new List<Sprite>(); readonly List<EnemyState> before=new List<EnemyState>();
  readonly Sprite[] enemySprites=new Sprite[5]; readonly Sprite slash,burst,ring,halo,ember;
  readonly Texture2D emberTexture;
  readonly SpriteRenderer paper,landscape,upperInk,lowerInk,core,coreGlow,coreHeart,coreOrbit;
  readonly List<SpriteRenderer> tips=new List<SpriteRenderer>(),tipRings=new List<SpriteRenderer>();
  Texture2D boundInk; RunSimulator boundRun; float halfHeight=7,freeze,shake,visualTime,nextStop;
  public int Deaths {get;private set;} public int HitFlashes {get;private set;} public int EffectsCreated {get;private set;}
  public int InterpolatedFrames {get;private set;} public int ActiveEnemies=>enemies.Count;
  public Action<EnemyState> DeathMark;
  public InkBattleWorld(Transform parent,Texture2D[] masks,Texture2D slashMask,Texture2D burstMask,Texture2D ringMask,Texture2D haloMask,Texture2D paperTexture,Texture2D landscapeTexture)
  {
   root=new GameObject("Bokumyaku sprite battlefield");root.transform.SetParent(parent,false);root.transform.position=new Vector3(10000,10000,0);
   var cam=new GameObject("URP 2D battlefield camera");cam.transform.SetParent(root.transform,false);camera=cam.AddComponent<Camera>();camera.orthographic=true;camera.depth=100;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.94f,.91f,.83f);camera.cullingMask=1<<31;camera.nearClipPlane=.1f;camera.farClipPlane=30;camera.transform.localPosition=new Vector3(0,0,-10);
   var shader=Resources.Load<Shader>("BokumyakuRendering/InkSprite");if(shader==null)throw new InvalidOperationException("InkSprite shader missing");material=new Material(shader){name="Shared moving ink"};
   for(int i=0;i<5;i++)enemySprites[i]=MakeSprite(masks[i]);slash=MakeSprite(slashMask);burst=MakeSprite(burstMask);ring=MakeSprite(ringMask);halo=MakeSprite(haloMask);
   emberTexture=new Texture2D(32,32,TextureFormat.RGBA32,false){name="Enemy ember",wrapMode=TextureWrapMode.Clamp};
   var emberPixels=new Color[1024];for(int y=0;y<32;y++)for(int x=0;x<32;x++){float r=Vector2.Distance(new Vector2(x+.5f,y+.5f),new Vector2(16,16))/16;float heart=1-Mathf.SmoothStep(0,1,r/.24f);float opacity=1-Mathf.SmoothStep(0,1,(r-.25f)/.75f);emberPixels[y*32+x]=new Color(1,.035f+heart*.9f,.01f+heart*.65f,opacity);}
   emberTexture.SetPixels(emberPixels);emberTexture.Apply(false,true);ember=MakeSprite(emberTexture);
   paper=Create(MakeSprite(paperTexture),-100);landscape=Create(MakeSprite(landscapeTexture),-99);upperInk=Create(null,-80);lowerInk=Create(null,-80);
   coreGlow=Create(halo,8);coreGlow.color=new Color(1,.55f,.08f,.8f);core=Create(ring,10);core.color=new Color(1,.88f,.52f);coreHeart=Create(halo,11);coreHeart.color=new Color(1,.98f,.84f);coreOrbit=Create(ring,9);coreOrbit.color=new Color(1,.72f,.22f,.85f);
  }
  Sprite MakeSprite(Texture2D texture,Rect? area=null){if(texture==null)return null;var s=Sprite.Create(texture,area??new Rect(0,0,texture.width,texture.height),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect);ownedSprites.Add(s);return s;}
  SpriteRenderer Create(Sprite sprite,int order){var o=new GameObject("Ink sprite");o.layer=31;o.transform.SetParent(root.transform,false);var r=o.AddComponent<SpriteRenderer>();r.sharedMaterial=material;r.sprite=sprite;r.sortingOrder=order;return r;}
  void Size(SpriteRenderer r,Vector2 center,float width,float height){if(r.sprite==null)return;r.transform.localPosition=new Vector3(center.x,center.y,0);var b=r.sprite.bounds.size;r.transform.localScale=new Vector3(width/b.x,height/b.y,1);}
  void SizeArea(SpriteRenderer renderer,Vector2 center,float radius)
  {
   var top=Project(center+Vector2.up*radius);var bottom=Project(center-Vector2.up*radius);
   var p=Project(center);p.y=(top.y+bottom.y)*.5f;
   Size(renderer,p,radius*2/80f*1.23f,(top.y-bottom.y)*1.23f);
  }
  Vector2 Project(Vector2 p){float t=.5f-p.y*.48f/512f;t=t<=.5f?t*1.44f:.72f+(t-.5f)*.56f;return new Vector2(p.x/400f*5,(.5f-t)*halfHeight*2);}
  void Properties(SpriteRenderer r,float hit,float dissolve,float wobble,float phase,bool reduced){block.Clear();block.SetFloat("_Hit",hit);block.SetFloat("_Dissolve",dissolve);block.SetFloat("_Wobble",wobble);block.SetFloat("_Phase",phase);block.SetFloat("_ReducedMotion",reduced?1:0);r.SetPropertyBlock(block);}
  void Release(SpriteRenderer r){r.gameObject.SetActive(false);r.SetPropertyBlock(null);r.transform.localRotation=Quaternion.identity;pool.Push(r);}
  SpriteRenderer Rent(Sprite s,int order){var r=pool.Count>0?pool.Pop():Create(s,order);r.sprite=s;r.enabled=true;r.sortingOrder=order;r.color=Color.white;r.gameObject.SetActive(true);return r;}
  void ReleaseEnemy(EnemyView v){Release(v.body);Release(v.eye);if(v.buds!=null)foreach(var bud in v.buds)Release(bud);}
  public void Reset(RunSimulator run)
  {
   foreach(var e in enemies.Values)ReleaseEnemy(e);enemies.Clear();foreach(var e in effects)Release(e.renderer);effects.Clear();before.Clear();boundRun=run;freeze=shake=0;Deaths=HitFlashes=EffectsCreated=InterpolatedFrames=0;
  }
  public void BeforeTick(RunSimulator run){if(boundRun!=run)Reset(run);before.Clear();foreach(var e in run.Enemies)before.Add(e);foreach(var v in enemies.Values)if(!v.dying)v.previous=v.current;}
  public void AfterTick(RunSimulator run,bool reduced)
  {
   foreach(var old in before)if(old.Hp<=0)
   {
    Deaths++;DeathMark?.Invoke(old);if(!reduced&&Time.unscaledTime>=nextStop){freeze=.045f;shake=.12f;nextStop=Time.unscaledTime+.18f;}
    if(enemies.TryGetValue(old.Id,out var v)){v.dying=true;v.death=0;v.current=new Vector2((float)old.X,(float)old.Y);}
    Spawn(burst,new Vector2((float)old.X,(float)old.Y),Vector2.zero,.55f,1.2f,0,new Color(.03f,.025f,.02f,.9f));
   }
   liveIds.Clear();var live=run.Enemies;foreach(var e in live)
   {
    liveIds.Add(e.Id);
    Vector2 p=new Vector2((float)e.X,(float)e.Y);
    if(!enemies.TryGetValue(e.Id,out var v)){v=new EnemyView{state=e,previous=p,current=p,hp=e.Hp,body=Rent(enemySprites[(int)e.Kind],4),eye=Rent(ember,5)};if(e.Kind==EnemyKind.Splitter||e.Kind==EnemyKind.Boss){v.buds=new SpriteRenderer[3];for(int k=0;k<3;k++)v.buds[k]=Rent(ember,5);}enemies.Add(e.Id,v);}
    if(e.Hp<v.hp){v.hit=.13f;HitFlashes++;}v.hp=e.Hp;v.current=p;v.state=e;
   }
   remove.Clear();foreach(var pair in enemies)if(!pair.Value.dying&&!liveIds.Contains(pair.Key))remove.Add(pair.Key);
   foreach(int id in remove){ReleaseEnemy(enemies[id]);enemies.Remove(id);}
   foreach(var hit in run.Attacks)Attack(hit,run,reduced);
  }

  void Spawn(Sprite sprite,Vector2 a,Vector2 b,float life,float width,int type,Color tint,bool flash=false)
  {
   if(effects.Count>=128){Release(effects[0].renderer);effects.RemoveAt(0);}effects.Add(new Effect{renderer=Rent(sprite,flash?9:6),flash=flash,a=a,b=b,life=life,width=width,type=type,tint=tint});EffectsCreated++;
  }
  void Attack(AttackEvent hit,RunSimulator run,bool reduced)
  {
   var a=new Vector2((float)hit.X,(float)hit.Y);var b=new Vector2((float)hit.TargetX,(float)hit.TargetY);var ink=new Color(.025f,.02f,.015f,.95f);
   if(!reduced)
   {
    // Light is a short accent over the ink, not a replacement for the weapon silhouette.
    Spawn(halo,a,Vector2.zero,.14f,.85f,0,new Color(1,.82f,.35f,.9f),true);
    Spawn(burst,b,Vector2.zero,.18f,.55f,0,new Color(1,.9f,.5f,.9f),true);
    if(hit.Weapon==WeaponKind.Point||hit.Weapon==WeaponKind.Pierce||hit.Weapon==WeaponKind.Resonance)
     Spawn(slash,a,b,.13f,.095f,1,new Color(1,.88f,.46f,.95f),true);
   }
   switch(hit.Weapon)
   {
    case WeaponKind.Point: Spawn(slash,a,b,.12f,.18f,1,ink);Spawn(burst,b,Vector2.zero,.30f,.65f,0,ink);break;
    case WeaponKind.Pierce: Spawn(slash,a,b,.28f,.7f,1,ink);Spawn(burst,b,Vector2.zero,.32f,.9f,0,ink);break;
    case WeaponKind.Bleed: Spawn(burst,b,Vector2.zero,.7f,1.3f,0,new Color(.05f,.035f,.03f,.5f));break;
    case WeaponKind.Sweep:
     for(int i=-2;i<=2;i++){var d=(b-a).normalized*(float)(RunSimulator.BaseAttackRange+4*run.UpgradeLevels[4]);float angle=i*Mathf.PI/8;var end=a+new Vector2(d.x*Mathf.Cos(angle)-d.y*Mathf.Sin(angle),d.x*Mathf.Sin(angle)+d.y*Mathf.Cos(angle));Spawn(slash,a,end,.24f,.38f,1,ink);}break;
    case WeaponKind.Enso: Spawn(ring,a,Vector2.zero,.4f,150+4*run.UpgradeLevels[4],2,ink);break;
    case WeaponKind.Resonance: Spawn(slash,a,b,.24f,.3f,1,new Color(.22f,.12f,.025f,.95f));Spawn(ring,b,Vector2.zero,.3f,.6f,0,ink);break;
   }
  }
  public void Render(RunSimulator run,Texture2D persistent,float alpha,bool paused,bool reduced,bool visible,Rect viewport)
  {
   root.SetActive(visible);if(!visible)return;if(boundRun!=run)Reset(run);
   camera.rect=viewport;halfHeight=5/(Screen.width*viewport.width/(Screen.height*viewport.height));camera.orthographicSize=halfHeight;
   float dt=paused?0:Mathf.Min(Time.unscaledDeltaTime,.1f);bool frozen=freeze>0&&!reduced;if(frozen){freeze-=dt;dt=0;}visualTime+=dt;shake=Mathf.Max(0,shake-dt);
   camera.transform.localPosition=new Vector3(reduced?0:Mathf.Sin(visualTime*91)*shake*.35f,reduced?0:Mathf.Cos(visualTime*73)*shake*.3f,-10);
   Size(paper,Vector2.zero,10,halfHeight*2);Size(landscape,Vector2.zero,10,halfHeight*2);landscape.color=new Color(1,1,1,.65f);
   if(boundInk!=persistent)
   {
    if(upperInk.sprite!=null){ownedSprites.Remove(upperInk.sprite);UnityEngine.Object.Destroy(upperInk.sprite);ownedSprites.Remove(lowerInk.sprite);UnityEngine.Object.Destroy(lowerInk.sprite);}
    boundInk=persistent;upperInk.sprite=MakeSprite(persistent,new Rect(0,persistent.height/2,persistent.width,persistent.height/2));lowerInk.sprite=MakeSprite(persistent,new Rect(0,0,persistent.width,persistent.height/2));
   }
   upperInk.color=lowerInk.color=new Color(.035f,.03f,.025f);Size(upperInk,new Vector2(0,halfHeight*.28f),10,halfHeight*1.44f);Size(lowerInk,new Vector2(0,-halfHeight*.72f),10,halfHeight*.56f);
   var center=Project(Vector2.zero);Size(coreGlow,center,2.5f,2.5f);Size(core,center,1.05f,1.05f);Size(coreHeart,center,.72f,.72f);Size(coreOrbit,center,1.5f,1.5f);coreOrbit.transform.localRotation=Quaternion.Euler(0,0,31);
   var branches=run.Branches;while(tips.Count<branches.Count){tips.Add(Create(halo,7));tipRings.Add(Create(ring,8));}for(int i=0;i<tips.Count;i++){bool enabled=i<branches.Count&&branches[i].IsTip;tips[i].enabled=enabled;tipRings[i].enabled=enabled;if(enabled){var b=branches[i];Size(tips[i],Project(new Vector2((float)b.X,(float)b.Y)),.6f,.6f);tips[i].color=new Color(1,.94f,.65f,1);Size(tipRings[i],Project(new Vector2((float)b.X,(float)b.Y)),.43f,.43f);tipRings[i].color=new Color(1,.72f,.24f);}}
   // The first render after a fast-forward QA or loaded run also gets current actors.
   if(enemies.Count==0&&run.Enemies.Count>0){before.Clear();AfterTick(run,reduced);}
   var fields=run.Fields;
   while(fieldViews.Count<fields.Count)fieldViews.Add(Create(burst,-40));
   for(int i=0;i<fieldViews.Count;i++){fieldViews[i].enabled=i<fields.Count;if(i<fields.Count){var f=fields[i];SizeArea(fieldViews[i],new Vector2((float)f.X,(float)f.Y),(float)f.Radius);fieldViews[i].color=new Color(.04f,.025f,.02f,.24f);}}
   remove.Clear();foreach(var pair in enemies)
   {
    var v=pair.Value;if(v.dying)v.death+=dt;v.hit=Mathf.Max(0,v.hit-dt);if(v.death>=.45f){remove.Add(pair.Key);continue;}
    var p=Project(v.dying?v.current:Vector2.Lerp(v.previous,v.current,paused?1:Mathf.Clamp01(alpha)));
    float size=v.state.Kind==EnemyKind.Boss?1.55f:v.state.Kind==EnemyKind.Heavy?1.1f:v.state.Kind==EnemyKind.Fast?.72f:.85f;
    if(!frozen||!v.drawn){Size(v.body,p,size,size);Size(v.eye,p,size*.38f,size*.38f);
     if(v.state.Kind==EnemyKind.Fast){var direction=Project(v.current)-Project(v.previous);if(direction.sqrMagnitude>.000001f)v.body.transform.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(direction.y,direction.x)*Mathf.Rad2Deg);}v.drawn=true;}v.body.color=new Color(.025f,.02f,.02f);Properties(v.body,v.hit/.13f,v.death/.45f,reduced?0:.45f,pair.Key*.73f,reduced);
    v.eye.color=new Color(1,1,1,1-v.death/.45f);Properties(v.eye,0,v.death/.45f,0,0,reduced);
    if(v.buds!=null)
    {
     // Texture-local bud positions match the procedural lobe/branch motif.
     var bodyCenter=(Vector2)v.body.transform.localPosition;
     for(int k=0;k<3;k++)
     {
      bool boss=v.state.Kind==EnemyKind.Boss;float angle=k*Mathf.PI*2/3+(boss?.30f:.4f);
      float radius=(boss?25f:23f)/96f*size;
      Size(v.buds[k],bodyCenter+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*radius,size*.13f,size*.13f);
      v.buds[k].color=v.eye.color;Properties(v.buds[k],0,v.death/.45f,0,0,reduced);
     }
    }
    if(!paused&&!v.dying&&alpha>0&&alpha<1&&v.previous!=v.current)InterpolatedFrames++;
   }
   foreach(int id in remove){ReleaseEnemy(enemies[id]);enemies.Remove(id);}
   for(int i=effects.Count-1;i>=0;i--)
   {
    var e=effects[i];e.renderer.enabled=!(reduced&&e.flash);e.age+=dt;if(e.age>=e.life){Release(e.renderer);effects.RemoveAt(i);continue;}
    float t=e.age/e.life;var a=Project(e.a);var color=e.tint;color.a*=1-t;e.renderer.color=color;
    if(e.type==1){var b=Project(e.b);var d=b-a;Size(e.renderer,(a+b)*.5f,d.magnitude/ .75f,e.width);e.renderer.transform.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(d.y,d.x)*Mathf.Rad2Deg);}
    else if(e.type==2){SizeArea(e.renderer,e.a,e.width*(.8f+.2f*t));}
    else{float size=e.width*(.6f+t*.8f);Size(e.renderer,a,size,size);}
    Properties(e.renderer,0,e.type==0?t*.6f:0,0,0,reduced);
   }
  }
  public void Dispose(){foreach(var s in ownedSprites)if(s!=null)UnityEngine.Object.Destroy(s);UnityEngine.Object.Destroy(emberTexture);UnityEngine.Object.Destroy(material);UnityEngine.Object.Destroy(root);}
 }
}








