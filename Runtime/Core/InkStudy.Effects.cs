using System;

namespace ConnectPiece.SumiEngine.Core
{
    public static partial class InkStudy
    {
        /// <summary>12 cells: dry, split, taper / bleed, pool, splatter /
        /// wash, mist, enso / branch, drips, spiral. All marks are editable brush input.</summary>
        public static void DrawEffects(SumiEngineCore core, int seed = 731)
        {
            if (core == null) throw new ArgumentNullException(nameof(core));
            core.Clear();
            float scale = Math.Min(core.Paper.Width / 768f, core.Paper.Height / 1024f);
            for (int cell = 0; cell < 12; cell++)
            {
                float cx = 128 + cell % 3 * 256, cy = 890 - cell / 3 * 250;
                int s = seed + cell * 127;
                if (cell < 3)
                {
                    var b = cell == 0 ? InkBrushSettings.Dry : cell == 1 ? InkBrushSettings.Split : InkBrushSettings.Dense;
                    b.radius = cell == 2 ? 26 : 32;
                    for (int j = 0; j < 3; j++)
                    {
                        float offset = (j - 1) * 55;
                        Path(core, b, s + j, scale, t => new InkPoint(cx + offset - 24 + 48*t,
                            cy - 88 + 176*t, t, (float)Math.Pow(Math.Sin(Math.PI*t), .8)));
                    }
                }
                else if (cell == 3)
                {
                    var b = InkBrushSettings.Bleed; b.radius = 60;
                    for (int j = 0; j < 5; j++)
                    { b.pigment = .22f + j * .09f; Dot(core,b,s+j,scale,cx-42+j*21,cy+(j%2)*34-15); }
                }
                else if (cell == 4)
                {
                    var b = InkBrushSettings.Pool; b.radius = 70;
                    for(int j=0;j<7;j++) Dot(core,b,s,scale,cx,cy);
                }
                else if (cell == 5)
                {
                    var b = InkBrushSettings.Splatter; b.radius = 34;
                    Dot(core,b,s,scale,cx-15,cy+10);
                    b.radius = 15; Dot(core,b,s+1,scale,cx+61,cy-59);
                }
                else if (cell == 6)
                {
                    var b=InkBrushSettings.Bleed; b.radius=29;
                    for(int j=0;j<3;j++)
                    {
                        b.pigment=.11f+j*.13f; float y=cy+62-j*60;
                        Path(core,b,s+j,scale,t=>new InkPoint(cx-82+164*t,y+8*(float)Math.Sin(t*4),t,.95f-.8f*t));
                    }
                }
                else if (cell == 7)
                {
                    var b=InkBrushSettings.Mist; b.radius=48;
                    Path(core,b,s,scale,t=>new InkPoint(cx+48*(float)Math.Sin(t*8),cy-78+156*t,t,.35f+.65f*(float)Math.Sin(Math.PI*t)));
                }
                else if (cell == 8 || cell == 11)
                {
                    var b=InkBrushSettings.Dry; b.radius=23; b.roughness=.35f; b.bristleCount=13;
                    Path(core,b,s,scale,t=> {
                        double a=(.2+t*(cell==8?1.86:3.8))*Math.PI;
                        float r=cell==8?77:83*(1f-.88f*t);
                        return new InkPoint(cx+r*(float)Math.Cos(a),cy+r*(float)Math.Sin(a),t,.12f+.88f*(float)Math.Sin(Math.PI*t));
                    });
                }
                else if (cell == 9)
                {
                    var b=InkBrushSettings.Dense;b.radius=15;b.roughness=.5f;
                    Path(core,b,s,scale,t=>new InkPoint(cx-88+176*t,cy-70+140*t+10*(float)Math.Sin(t*9),t,1f-.9f*t));
                    for(int j=0;j<6;j++)
                    {
                        float start=j/7f, x=cx-88+176*start,y=cy-70+140*start;
                        float sign=j%2==0?1:-1;b.radius=6;
                        Path(core,b,s+j+1,scale,t=>new InkPoint(x+sign*42*t,y+51*t,t,1f-t));
                        var leaf=InkBrushSettings.Bleed;leaf.radius=7;
                        Dot(core,leaf,s+j+20,scale,x+sign*42,y+51);
                    }
                }
                else
                {
                    for(int j=0;j<4;j++)
                    {
                        float x=cx-70+46*j; var b=InkBrushSettings.Bleed; b.radius=22;b.pigment=.5f-.09f*j;
                        Dot(core,b,s+j,scale,x,cy+56);
                        b.radius=8;
                        Path(core,b,s+j,scale,t=>new InkPoint(x+3*(float)Math.Sin(t*7),cy+50-140*t,t,1f-.85f*t));
                    }
                }
            }
        }

        static void Dot(SumiEngineCore core, InkBrushSettings brush, int seed, float scale,float x,float y)
        {
            brush.radius *= scale;
            core.BeginStroke(new InkPoint(x*scale,y*scale,0,1),brush,seed);core.EndStroke();
        }
        static void Path(SumiEngineCore core, InkBrushSettings brush, int seed, float scale,Func<float,InkPoint> point)
        {
            brush.radius *= scale;
            for(int i=0;i<=100;i++)
            {
                InkPoint p=point(i/100f);p.x*=scale;p.y*=scale;
                if(i==0)core.BeginStroke(p,brush,seed);else core.AppendPoint(p);
            }
            core.EndStroke();
        }
    }
}
