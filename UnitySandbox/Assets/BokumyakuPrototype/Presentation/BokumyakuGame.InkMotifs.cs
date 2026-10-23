using Bokumyaku.Simulation;
using UnityEngine;
namespace Bokumyaku.Presentation
{
    public sealed partial class BokumyakuGame
    {
        Texture2D[] enemyInk;
        Texture2D coreInk;
        void EnsureInkMotifs()
        {
            if(enemyInk!=null)return;
            enemyInk=new Texture2D[5];
            for(int i=0;i<enemyInk.Length;i++)enemyInk[i]=ProceduralInkMotifs.CreateEnemy((EnemyKind)i);
            coreInk=ProceduralInkMotifs.CreateCore();
            EnsureCombatInk();
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
            Dot(center,27,new Color(gold.r,gold.g,gold.b,.22f));
            InkStamp(coreInk,center,64,ink);
            InkStamp(coreInk,center,44,gold);
            Dot(center,9,gold);Dot(center,4,new Color(1f,.95f,.72f));
        }
        void ReleaseInkMotifs()
        {
            if(enemyInk!=null)foreach(var texture in enemyInk)if(texture!=null)Destroy(texture);
            if(coreInk!=null)Destroy(coreInk);
            if(slashInk!=null)Destroy(slashInk);
            if(burstInk!=null)Destroy(burstInk);
            slashInk=null;burstInk=null;
            enemyInk=null;coreInk=null;
        }
    }
}
