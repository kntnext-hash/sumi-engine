using ConnectPiece.SumiEngine.Core;
using UnityEngine;
namespace Bokumyaku.Presentation
{
 public sealed partial class BokumyakuGame
 {
  // Root flourishes belong to the paper, not the simulation: no attack tips or gold nodes.
  void DrawRootFlourishes()
  {
   float sx=canvas.width/384f,sy=canvas.height/512f;
   for(int i=0;i<7;i++)
   {
    float angle=Mathf.Lerp(-1.25f,1.25f,i/6f);
    var start=new Vector2(192,256);
    var end=start+new Vector2(Mathf.Sin(angle)*172,-Mathf.Cos(angle)*210);
    var control=start+new Vector2(Mathf.Sin(angle)*45,-115);
    var brush=InkBrushSettings.Dense;brush.radius=18;brush.pigment=.8f;brush.water=.12f;brush.roughness=.65f;brush.depletion=.003f;
    persistentInk.Begin(new InkPoint(start.x*sx,start.y*sy,0,.9f),brush,unchecked(Run.Seed+i*487));
    for(int n=1;n<=32;n++)
    {
     float t=n/32f;var p=(1-t)*(1-t)*start+2*(1-t)*t*control+t*t*end;
     p.x+=Mathf.Sin(t*Mathf.PI*2+i)*Mathf.Sin(t*Mathf.PI)*7;
     persistentInk.Move(new InkPoint(p.x*sx,p.y*sy,t*.6f,Mathf.Lerp(.9f,.04f,t*t)));
    }
    persistentInk.End();
   }
   // Short side roots anchor the core without adding gameplay nodes.
   for(int i=0;i<7;i++)
   {
    float angle=Mathf.Lerp(-1.25f,1.25f,i/6f);
    var start=new Vector2(192,256);var end=start+new Vector2(Mathf.Sin(angle)*172,-Mathf.Cos(angle)*210);
    var control=start+new Vector2(Mathf.Sin(angle)*45,-115);
    for(int fork=0;fork<3;fork++)
    {
     float t=.38f+fork*.17f;var junction=(1-t)*(1-t)*start+2*(1-t)*t*control+t*t*end;
     junction.x+=Mathf.Sin(t*Mathf.PI*2+i)*Mathf.Sin(t*Mathf.PI)*7;
     var direction=(end-control).normalized;var side=new Vector2(-direction.y,direction.x)*((i+fork)%2==0?1:-1);
     var tip=junction+direction*(30+fork*8)+side*(22+fork*7);
     var brush=InkBrushSettings.Dense;brush.radius=6-fork;brush.water=.12f;brush.roughness=.65f;
     persistentInk.Begin(new InkPoint(junction.x*sx,junction.y*sy,0,.8f),brush,unchecked(Run.Seed+i*487+fork+71));
     for(int n=1;n<=12;n++){float u=n/12f;var p=Vector2.Lerp(junction,tip,u)+side*Mathf.Sin(u*Mathf.PI)*8;persistentInk.Move(new InkPoint(p.x*sx,p.y*sy,u*.4f,Mathf.Lerp(.8f,.03f,u)));}
     persistentInk.End();
    }
   }
   persistentInk.Flush();
  }
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




