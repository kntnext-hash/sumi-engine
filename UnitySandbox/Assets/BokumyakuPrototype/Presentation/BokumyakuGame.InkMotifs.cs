using Bokumyaku.Simulation;
using UnityEngine;
namespace Bokumyaku.Presentation
{
    public sealed partial class BokumyakuGame
    {
        Texture2D[] enemyInk;
        Texture2D coreInk, lightHalo;
        Texture2D hudRing;
        Texture2D[] upgradeGlyphs;
        void EnsureInkMotifs()
        {
            if(enemyInk!=null)return;
            enemyInk=new Texture2D[7];
            for(int i=0;i<enemyInk.Length;i++)enemyInk[i]=ProceduralInkMotifs.CreateEnemy((EnemyKind)i);
            coreInk=ProceduralInkMotifs.CreateCore();
            hudRing=ProceduralInkMotifs.CreateHudRing();
            upgradeGlyphs=new Texture2D[5];
            for(int i=0;i<5;i++)upgradeGlyphs[i]=ProceduralInkMotifs.CreateUpgradeGlyph(i);
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
        void ReleaseInkMotifs()
        {
            if(enemyInk!=null)foreach(var texture in enemyInk)if(texture!=null)Destroy(texture);
            if(coreInk!=null)Destroy(coreInk);
            if(hudRing!=null)Destroy(hudRing);
            if(upgradeGlyphs!=null)foreach(var texture in upgradeGlyphs)if(texture!=null)Destroy(texture);
            hudRing=null;upgradeGlyphs=null;
            if(lightHalo!=null)Destroy(lightHalo);
            if(slashInk!=null)Destroy(slashInk);
            if(burstInk!=null)Destroy(burstInk);
            slashInk=null;burstInk=null;
            enemyInk=null;coreInk=null;
        }
    }
}
