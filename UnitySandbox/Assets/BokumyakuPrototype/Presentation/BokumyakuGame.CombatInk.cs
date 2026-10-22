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
            var style=InkBrushSettings.Dense;style.radius=7;style.roughness=.5f;style.water=.2f;style.depletion=.002f;
            var path=new InkPoint[20];
            for(int i=0;i<20;i++){float t=i/19f;path[i]=new InkPoint(12+72*t,48+7*Mathf.Sin(t*Mathf.PI),t*.3f,1-.9f*t);}
            slashInk=InkTextureBaker.Bake(96,new[]{path},style,8231);
            var paths=new List<InkPoint[]>();
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
        void BrushBeam(Vector2 from,Vector2 to,float thickness,float alpha)
        {
            if(slashInk==null)return;
            var matrix=GUI.matrix;var color=GUI.color;
            try
            {
                GUIUtility.RotateAroundPivot(Mathf.Atan2(to.y-from.y,to.x-from.x)*Mathf.Rad2Deg,from);
                GUI.color=new Color(ink.r,ink.g,ink.b,alpha);
                // The source stroke occupies x=12..84 of the 96px mask.
                float length=Vector2.Distance(from,to)/.75f;
                GUI.DrawTexture(new Rect(from.x-length*.125f,from.y-thickness*.5f,length,thickness),slashInk);
            }
            finally{GUI.matrix=matrix;GUI.color=color;}
        }
        void DrawInkBursts(Rect field)
        {
            foreach(var burst in inkBursts)
            {
                float age=1-burst.Life/.55f;var center=Logical(field,burst.Position);var matrix=GUI.matrix;
                try{GUIUtility.RotateAroundPivot(burst.Angle,center);InkStamp(burstInk,center,25+age*30,new Color(ink.r,ink.g,ink.b,(1-age)*.8f));}
                finally{GUI.matrix=matrix;}
            }
        }
    }
}
