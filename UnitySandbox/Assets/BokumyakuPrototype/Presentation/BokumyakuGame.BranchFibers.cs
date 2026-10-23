using ConnectPiece.SumiEngine.Core;
using UnityEngine;
namespace Bokumyaku.Presentation
{
 public sealed partial class BokumyakuGame
 {
  // Decorative brush fibers only: no extra attack nodes or gameplay branches.
  void DrawBranchFibers(float x0,float y0,float x1,float y1,float sx,float sy,int id)
  {
   var a=new Vector2(x0,y0);var b=new Vector2(x1,y1);var d=(b-a).normalized;var normal=new Vector2(-d.y,d.x);
   var brush=InkBrushSettings.Dense;brush.radius=3.2f;brush.roughness=.7f;brush.water=.12f;brush.pigment=.8f;brush.depletion=.01f;
   for(int twig=0;twig<5;twig++)
   {
    float t=.16f+twig*.16f,sign=(id+twig)%2==0?1:-1;
    Vector2 start=Vector2.Lerp(a,b,t),end=start+d*(12+(id+twig)%4*5)+normal*sign*(16+(id*7+twig)%5*4);
    persistentInk.Begin(new InkPoint(start.x*sx,start.y*sy,0,.8f),brush,unchecked(Run.Seed+id*193+twig));
    for(int n=1;n<=10;n++)
    {
     float u=n/10f;Vector2 p=Vector2.Lerp(start,end,u)+normal*Mathf.Sin(u*Mathf.PI)*sign*4;
     persistentInk.Move(new InkPoint(p.x*sx,p.y*sy,u*.3f,Mathf.Lerp(.8f,.06f,u)));
    }
    persistentInk.End();
   }
  }
 }
}


