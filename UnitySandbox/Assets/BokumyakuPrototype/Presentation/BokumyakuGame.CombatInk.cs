using System.Collections.Generic;
using Bokumyaku.Simulation;
using Bokumyaku.Presentation.Drawing;
using ConnectPiece.SumiEngine.Core;
using UnityEngine;
namespace Bokumyaku.Presentation
{
    public sealed partial class BokumyakuGame
    {
        Texture2D slashInk, burstInk;
        readonly List<EnemyState> watchedEnemies=new List<EnemyState>();
        readonly List<InkBurst> inkBursts=new List<InkBurst>();
        public int ObservedInkBursts { get; private set; }
        sealed class InkBurst { public Vector2 Position; public float Life; public float Angle; }
        void EnsureCombatInk()
        {
            var style=InkBrushSettings.Dense;style.radius=10;style.roughness=.5f;style.water=.2f;style.depletion=.002f;
            var path=new InkPoint[20];
            for(int i=0;i<20;i++){float t=i/19f;path[i]=new InkPoint(12+72*t,48+7*Mathf.Sin(t*Mathf.PI),t*.3f,1-.9f*t);}
            slashInk=InkTextureBaker.Bake(96,new[]{path},style,8231);
            var paths=new List<InkPoint[]>();
            // A loaded center survives small sprite sizes; rays alone read as a faint star.
            // Bake once through SUMI, then reuse this mask for moving/expanding effects.
            var center=new InkPoint[40];
            for(int i=0;i<center.Length;i++)
            {
                float t=i/(float)(center.Length-1),angle=t*Mathf.PI*6,radius=12*(1-t);
                center[i]=new InkPoint(48+Mathf.Cos(angle)*radius,48+Mathf.Sin(angle)*radius,t*.3f,.95f);
            }
            paths.Add(center);
            for(int n=0;n<9;n++)
            {
                float angle=n*2.39996f;float reach=19+n%3*8;
                var ray=new InkPoint[8];
                for(int i=0;i<8;i++){float t=i/7f;float radius=6+reach*t;ray[i]=new InkPoint(48+Mathf.Cos(angle)*radius,48+Mathf.Sin(angle)*radius,t*.2f,1-.92f*t);}
                paths.Add(ray);
            }
            style.radius=4;burstInk=InkTextureBaker.Bake(96,paths,style,8232);
        }
        void WatchEnemies()
        {
            watchedEnemies.Clear();foreach(var enemy in Run.Enemies)watchedEnemies.Add(enemy);
        }
        void ObserveDefeats()
        {
            foreach(var enemy in watchedEnemies)
            {
                if(enemy.Hp>0)continue; // Leaking enemies retain HP, so do not show a defeat splash.
                if(inkBursts.Count>=48)inkBursts.RemoveAt(0);
                inkBursts.Add(new InkBurst{Position=new Vector2((float)enemy.X,(float)enemy.Y),Life=.55f,Angle=enemy.Id*137.5f%360});
                ObservedInkBursts++;
            }
            watchedEnemies.Clear();
        }
        void AgeBursts()
        {
            for(int i=inkBursts.Count-1;i>=0;i--){inkBursts[i].Life-=Time.unscaledDeltaTime;if(inkBursts[i].Life<=0)inkBursts.RemoveAt(i);}
        }
    }
}
