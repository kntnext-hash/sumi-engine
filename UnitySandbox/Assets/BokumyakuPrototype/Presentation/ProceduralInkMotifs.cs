using System;
using System.Collections.Generic;
using Bokumyaku.Simulation;
using Bokumyaku.Presentation.Drawing;
using ConnectPiece.SumiEngine.Core;
using UnityEngine;

namespace Bokumyaku.Presentation
{
    // Visual motifs only. Caller creates these once, caches them and destroys its textures.
    public static class ProceduralInkMotifs
    {
        const int Size = 96;

        public static Texture2D CreateEnemy(EnemyKind kind)
        {
            var strokes = new List<InkPoint[]>();
            float radius = 4.5f;
            switch (kind)
            {
                case EnemyKind.Normal:
                    // Overlapping rounded lobes give the basic swarm a soft, compact outline.
                    strokes.Add(Arc(36, 43, 17, 18, 0, 360, 24));
                    strokes.Add(Arc(59, 43, 17, 18, 0, 360, 24));
                    strokes.Add(Arc(48, 60, 17, 17, 0, 360, 24));
                    strokes.Add(Line(33, 47, 62, 47, 12));
                    break;
                case EnemyKind.Fast:
                    // A pointed dart and two trailing strokes remain readable at small sizes.
                    strokes.Add(Polyline(new[] { new Vector2(26, 24), new Vector2(77, 48), new Vector2(26, 72), new Vector2(43, 48), new Vector2(26, 24) }, 21));
                    strokes.Add(Line(14, 35, 35, 41, 8));
                    strokes.Add(Line(14, 61, 35, 55, 8));
                    radius = 4;
                    break;
                case EnemyKind.Heavy:
                    // Concentric armour rings have a visibly hollow centre.
                    strokes.Add(Arc(48, 48, 30, 30, 0, 360, 24));
                    strokes.Add(Arc(48, 48, 18, 18, 0, 360, 24));
                    radius = 5;
                    break;
                case EnemyKind.Splitter:
                    // A parent body carries three budding lobes instead of two disconnected rings.
                    strokes.Add(Arc(48,48,18,22,0,360,28));
                    for(int i=0;i<3;i++)
                    {
                        float a=i*Mathf.PI*2/3+.4f;
                        var bud=new Vector2(48+Mathf.Cos(a)*23,48+Mathf.Sin(a)*23);
                        strokes.Add(Line(48,48,bud.x,bud.y,12));
                        strokes.Add(Arc(bud.x,bud.y,9,11,0,360,16));
                        strokes.Add(Line(bud.x,bud.y,bud.x+Mathf.Cos(a)*14,bud.y+Mathf.Sin(a)*14,12));
                    }
                    radius=4;
                    break;
                case EnemyKind.Boss:
                    // Forked, asymmetrical limbs make the boss a living ink tree.
                    strokes.Add(Arc(48,48,15,17,0,360,28));
                    for(int i=0;i<9;i++)
                    {
                        float a=i*Mathf.PI*2/9+.12f;
                        var mid=new Vector2(48+Mathf.Cos(a+.18f)*25,48+Mathf.Sin(a+.18f)*25);
                        var tip=new Vector2(48+Mathf.Cos(a)*40,48+Mathf.Sin(a)*40);
                        strokes.Add(Polyline(new[]{new Vector2(48,48),mid,tip},24));
                        var fork=mid+new Vector2(Mathf.Cos(a+.8f),Mathf.Sin(a+.8f))*13;
                        strokes.Add(Line(mid.x,mid.y,fork.x,fork.y,12));
                        if(i%3==0)strokes.Add(Arc(mid.x,mid.y,5,6,0,360,14));
                    }
                    radius=3;
                    break;
                case EnemyKind.Leech:
                    strokes.Add(Arc(48,48,13,17,0,360,24));
                    for(int i=0;i<6;i++)
                    {
                        float a=i*Mathf.PI/3;
                        var points=new Vector2[15];
                        for(int j=0;j<points.Length;j++){float t=j/14f,angle=a+t*1.25f;float r=10+29*t;points[j]=new Vector2(48+Mathf.Cos(angle)*r,48+Mathf.Sin(angle)*r);}
                        strokes.Add(Polyline(points,32));
                    }
                    radius=2.8f;break;
                case EnemyKind.ShieldHalo:
                    strokes.Add(Arc(48,48,13,16,0,360,24));
                    for(int i=0;i<4;i++)
                    {
                        strokes.Add(Arc(48,48,31,31,i*90+8,i*90+76,18));
                        float a=i*Mathf.PI/2;strokes.Add(Line(48+Mathf.Cos(a)*19,48+Mathf.Sin(a)*19,48+Mathf.Cos(a)*39,48+Mathf.Sin(a)*39,12));
                    }
                    radius=3.8f;break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind));
            }
            // Opaque ink bodies with broken radial strokes read as creatures, not UI rings.
            if(kind==EnemyKind.Normal||kind==EnemyKind.Heavy)
            {
                int count=kind==EnemyKind.Boss?12:kind==EnemyKind.Heavy?8:6;
                for(int i=0;i<count;i++)
                {
                    float a=i*Mathf.PI*2/count+.21f;
                    float reach=(i%3==0?40:34);
                    strokes.Add(Polyline(new[]{new Vector2(48+Mathf.Cos(a)*8,48+Mathf.Sin(a)*8),
                        new Vector2(48+Mathf.Cos(a+.15f)*23,48+Mathf.Sin(a+.15f)*23),
                        new Vector2(48+Mathf.Cos(a)*reach,48+Mathf.Sin(a)*reach)},16));
                }
                strokes.Add(Arc(48,48,8,8,0,360,20));
            }
            else if(kind==EnemyKind.Fast)
            {
                strokes.Add(Line(36,48,72,48,18));
                strokes.Add(Line(23,32,61,45,18));
                strokes.Add(Line(23,64,61,51,18));
            }
            return InkTextureBaker.Bake(Size, strokes, Style(radius), 4700 + (int)kind);
        }

        public static Texture2D CreateCore()
        {
            // The open enso ends with a tapered gap; the centre stays available for a gold accent.
            var strokes = new List<InkPoint[]>
            {
                Arc(48, 48, 31, 29, 25, 345, 24),
                Arc(48, 48, 20, 19, 195, 385, 16)
            };
            var style = Style(5.5f);
            style.roughness = .38f;
            return InkTextureBaker.Bake(Size, strokes, style, 4799);
        }

        public static Texture2D CreateHudRing()
        {
            var style = Style(2.8f);
            style.roughness = .55f;
            return InkTextureBaker.Bake(Size, new List<InkPoint[]> {
                Arc(48,48,39,38,12,350,56)
            }, style, 5800);
        }

        public static Texture2D CreateUpgradeGlyph(int kind)
        {
            var strokes = new List<InkPoint[]>();
            switch(kind)
            {
                case 0: // Three forceful strikes.
                    for(int i=0;i<3;i++)strokes.Add(Line(18+i*20,20,34+i*20,76,18));
                    break;
                case 1: // Flowing acceleration spiral.
                    var spiral=new InkPoint[64];
                    for(int i=0;i<64;i++) { float t=i/63f,a=t*Mathf.PI*4,r=32*(1-t)+3; spiral[i]=Point(48+Mathf.Cos(a)*r,48+Mathf.Sin(a)*r,i,64); }
                    strokes.Add(spiral);break;
                case 2: // Supply flows down toward a reservoir.
                    for(int i=0;i<3;i++)strokes.Add(Line(24+i*24,22,30+i*18,62,18));
                    strokes.Add(Arc(48,58,27,16,0,180,24));break;
                case 3: // A branching tree.
                    strokes.Add(Line(48,80,48,20,24));
                    for(int i=0;i<3;i++) { strokes.Add(Line(48,65-i*15,22-i*3,46-i*12,14)); strokes.Add(Line(48,65-i*15,74+i*3,46-i*12,14)); }break;
                default: // Breadth, a broad sweeping brush.
                    for(int i=0;i<3;i++)strokes.Add(Arc(48,52,27+i*5,16+i*7,20,220,24));break;
            }
            var style=Style(4);style.roughness=.45f;
            return InkTextureBaker.Bake(Size,strokes,style,5810+kind);
        }

        static InkBrushSettings Style(float radius)
        {
            var style = InkBrushSettings.Dense;
            style.radius = radius;
            style.bristleCount = 11;
            style.bristleSpread = .45f;
            style.depletion = .001f;
            style.water = .12f;
            return style;
        }

        static InkPoint Point(float x, float y, int index, int count)
        {
            float t = index / (float)(count - 1);
            return new InkPoint(x, y, index / 30f, .58f + .40f * Mathf.Sin(t * Mathf.PI));
        }

        static InkPoint[] Arc(float x, float y, float rx, float ry, float from, float to, int count)
        {
            var points = new InkPoint[count];
            for (int i = 0; i < count; i++)
            {
                float angle = Mathf.Lerp(from, to, i / (float)(count - 1)) * Mathf.Deg2Rad;
                points[i] = Point(x + rx * Mathf.Cos(angle), y + ry * Mathf.Sin(angle), i, count);
            }
            return points;
        }

        static InkPoint[] Line(float x1, float y1, float x2, float y2, int count)
        {
            return Polyline(new[] { new Vector2(x1, y1), new Vector2(x2, y2) }, count);
        }

        static InkPoint[] Polyline(Vector2[] corners, int count)
        {
            var points = new InkPoint[count];
            for (int i = 0; i < count; i++)
            {
                float position = i / (float)(count - 1) * (corners.Length - 1);
                int segment = Math.Min((int)position, corners.Length - 2);
                Vector2 p = Vector2.Lerp(corners[segment], corners[segment + 1], position - segment);
                points[i] = Point(p.x, p.y, i, count);
            }
            return points;
        }
    }
}


