using Bokumyaku.Simulation;
using UnityEngine;
namespace Bokumyaku.Presentation
{
    public sealed partial class BokumyakuGame
    {
        Texture2D[] enemyInk;
        Texture2D coreInk, lightHalo;
        void EnsureInkMotifs()
        {
            if(enemyInk!=null)return;
            enemyInk=new Texture2D[5];
            for(int i=0;i<enemyInk.Length;i++)enemyInk[i]=ProceduralInkMotifs.CreateEnemy((EnemyKind)i);
            coreInk=ProceduralInkMotifs.CreateCore();
            EnsureCombatInk();
            lightHalo=new Texture2D(64,64,TextureFormat.RGBA32,false);
            lightHalo.name="Bokumyaku soft light";
            var pixels=new Color[4096];
            for(int y=0;y<64;y++)for(int x=0;x<64;x++)
            {
                float r=Vector2.Distance(new Vector2(x+.5f,y+.5f),new Vector2(32,32))/32;
                pixels[y*64+x]=new Color(1,1,1,Mathf.Pow(Mathf.Clamp01(1-r),3));
            }
            lightHalo.SetPixels(pixels);lightHalo.Apply();lightHalo.wrapMode=TextureWrapMode.Clamp;
        }
        void InkStamp(Texture2D mask,Vector2 center,float size,Color tint)
        {
            var previous=GUI.color;GUI.color=tint;
            GUI.DrawTexture(new Rect(center.x-size*.5f,center.y-size*.5f,size,size),mask);
            GUI.color=previous;
        }
        void DrawInkEnemy(EnemyKind kind,Vector2 point,float radius)
        {
            float size=kind==EnemyKind.Boss?66:kind==EnemyKind.Heavy?48:kind==EnemyKind.Fast?36:40;
            Dot(point,radius+4,new Color(red.r,red.g,red.b,.12f));
            InkStamp(enemyInk[(int)kind],point,size,ink);
            Dot(point,kind==EnemyKind.Boss?5:3.2f,red);
            Dot(point,1.2f,new Color(1f,.72f,.45f));
        }
        void DrawInkCore(Vector2 center)
        {
            InkStamp(lightHalo,center,140,new Color(1f,.56f,.08f,.75f));
            InkStamp(burstInk,center,105,new Color(.20f,.13f,.04f,.6f));
            InkStamp(coreInk,center,64,ink);
            InkStamp(coreInk,center,44,gold);
            InkStamp(lightHalo,center,56,new Color(1f,.81f,.28f));
            InkStamp(coreInk,center,40,new Color(1f,.84f,.4f));
            Dot(center,7,new Color(1f,.91f,.57f));Dot(center,3,new Color(1f,1f,.9f));
        }
        void ReleaseInkMotifs()
        {
            if(enemyInk!=null)foreach(var texture in enemyInk)if(texture!=null)Destroy(texture);
            if(coreInk!=null)Destroy(coreInk);
            if(lightHalo!=null)Destroy(lightHalo);
            if(slashInk!=null)Destroy(slashInk);
            if(burstInk!=null)Destroy(burstInk);
            slashInk=null;burstInk=null;
            enemyInk=null;coreInk=null;
        }
    }
}
