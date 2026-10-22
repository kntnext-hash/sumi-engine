using System;
using System.Collections.Generic;
using ConnectPiece.SumiEngine.Core;
using UnityEngine;
namespace Bokumyaku.Presentation.Drawing
{
    /// <summary>Reusable point paths to a tintable ink mask. No combat or game-state dependencies.</summary>
    public static class InkTextureBaker
    {
        public static Texture2D Bake(int size, IReadOnlyList<InkPoint[]> strokes, InkBrushSettings style, int seed)
        {
            if(size<16 || size>512)throw new ArgumentOutOfRangeException(nameof(size));
            if(strokes==null)throw new ArgumentNullException(nameof(strokes));
            var core=new SumiEngineCore(size,size,seed);
            for(int i=0;i<strokes.Count;i++)
            {
                var points=strokes[i];
                if(points==null || points.Length==0)continue;
                core.BeginStroke(points[0],style,unchecked(seed+i*7919));
                for(int j=1;j<points.Length;j++)core.AppendPoint(points[j]);
                core.EndStroke();
            }
            for(int i=0;i<3;i++)core.Step(1f/30f);
            var colors=new Color32[size*size];
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
                colors[y*size+x]=new Color32(255,255,255,(byte)Mathf.RoundToInt(255*Mathf.Clamp01(1-Mathf.Exp(-2.5f*core.Paper.PigmentAt(x,y)))));
            var texture=new Texture2D(size,size,TextureFormat.RGBA32,false,false)
            {name="SUMI baked ink mask",filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp,hideFlags=HideFlags.DontSave};
            texture.SetPixels32(colors);texture.Apply(false,false);return texture;
        }
    }
}
