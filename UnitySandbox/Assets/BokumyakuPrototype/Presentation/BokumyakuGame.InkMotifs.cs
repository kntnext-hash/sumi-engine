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
            float size=kind==EnemyKind.Boss?56:kind==EnemyKind.Heavy?42:kind==EnemyKind.Fast?30:34;
            Dot(point,radius+4,new Color(red.r,red.g,red.b,.12f));
            InkStamp(enemyInk[(int)kind],point,size,ink);
            Dot(point,kind==EnemyKind.Boss?4:2.4f,red);
        }
        void DrawInkCore(Vector2 center)
        {
            Dot(center,22,new Color(gold.r,gold.g,gold.b,.13f));
            InkStamp(coreInk,center,64,ink);
            Ring(center,11,gold);Dot(center,5,gold);
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
