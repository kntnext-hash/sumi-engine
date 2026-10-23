using ConnectPiece.SumiEngine.Core;
using UnityEngine;
namespace Bokumyaku.Presentation
{
 public sealed partial class BokumyakuGame
 {
  // Root flourishes belong to the paper, not the simulation: no attack tips or gold nodes.
  // Seven deliberately unequal cubic paths leave paper between the roots. Keep 35 slices:
  // two passes on each trunk, then three connected forks per trunk (one stroke per slice).
  static void RootPath(int i,out Vector2 a,out Vector2 b,out Vector2 c,out Vector2 d)
  {
   a=new Vector2(192,256);
   switch(i)
   {
    case 0:b=new Vector2(151,219);c=new Vector2(106,292);d=new Vector2(8,223);break;
    case 1:b=new Vector2(182,194);c=new Vector2(84,124);d=new Vector2(16,160);break;
    case 2:b=new Vector2(218,195);c=new Vector2(100,131);d=new Vector2(70,20);break;
    case 3:b=new Vector2(160,217);c=new Vector2(250,109);d=new Vector2(193,38);break;
    case 4:b=new Vector2(247,251);c=new Vector2(326,171);d=new Vector2(315,70);break;
    case 5:b=new Vector2(231,278);c=new Vector2(283,179);d=new Vector2(376,220);break;
    default:b=new Vector2(188,208);c=new Vector2(272,112);d=new Vector2(366,136);break;
   }
  }
  static Vector2 RootPosition(int i,float t)
  {
   RootPath(i,out var a,out var b,out var c,out var d);
   float v=1-t;
   var p=v*v*v*a+3*v*v*t*b+3*v*t*t*c+t*t*t*d;
   // A restrained wandering edge follows the curve without turning every root into a fan.
   p.x+=Mathf.Sin(t*Mathf.PI*2+i*.83f)*Mathf.Sin(t*Mathf.PI)*3.5f;
   return p;
  }
  void DrawRootFlourish(int slice)
  {
   float sx=canvas.width/384f,sy=canvas.height/512f;
   if(slice<14)
   {
    int i=slice/2,pass=slice%2;
    var start=RootPosition(i,0);
    var brush=InkBrushSettings.Dense;
    brush.radius=17+(i*3%5);brush.pigment=.8f;brush.water=.12f;brush.roughness=.65f;brush.depletion=.003f;
    if(pass==1){brush.texture=InkTexture.Pool;brush.radius=7.5f+(i%3);brush.roughness=.3f;brush.pigment=.9f;brush.water=.25f;}
    persistentInk.Begin(new InkPoint(start.x*sx,start.y*sy,0,.45f),brush,unchecked(Run.Seed+i*487));
    for(int n=1;n<=32;n++)
    {
     float t=n/32f;var p=RootPosition(i,t);
     // A light heel under the luminous core, a loaded middle, then a long pointed tip.
     float pressure=Mathf.Lerp(.55f,.025f,t)+Mathf.Sin(t*Mathf.PI)*.46f;
     persistentInk.Move(new InkPoint(p.x*sx,p.y*sy,t*.6f,pressure));
    }
    persistentInk.End();
   }
   else
   {
    int i=(slice-14)/3,fork=(slice-14)%3;
    float t=.27f+fork*.19f+(i%3)*.025f;
    var junction=RootPosition(i,t);
    var direction=(RootPosition(i,t+.01f)-RootPosition(i,t-.01f)).normalized;
    var side=new Vector2(-direction.y,direction.x)*((i+fork)%2==0?1:-1);
    var tip=junction+direction*(32+fork*9+(i%3)*7)+side*(26+((i+fork)%3)*12);
    tip.x=Mathf.Clamp(tip.x,5,379);tip.y=Mathf.Clamp(tip.y,5,294);
    var control=junction+direction*(24+fork*5)-side*8;
    var brush=InkBrushSettings.Dense;brush.radius=8.5f-fork*1.3f;brush.water=.12f;brush.roughness=.65f;
    persistentInk.Begin(new InkPoint(junction.x*sx,junction.y*sy,0,.78f),brush,unchecked(Run.Seed+i*487+fork+71));
    for(int n=1;n<=12;n++)
    {
     float u=n/12f,v=1-u;var p=v*v*junction+2*v*u*control+u*u*tip;
     persistentInk.Move(new InkPoint(p.x*sx,p.y*sy,u*.4f,Mathf.Lerp(.78f,.025f,u)));
    }
    persistentInk.End();
   }
   persistentInk.Flush();
  }
  // Decorative brush fibers only: no extra attack nodes or gameplay branches.
  static Vector2 BranchStrokePoint(Vector2 a,Vector2 b,int id,float t)
  {
   var d=(b-a).normalized;var normal=new Vector2(-d.y,d.x);
   float bow=Mathf.Sin(t*Mathf.PI)*(id%2==0?9f:-9f);
   return Vector2.Lerp(a,b,t)+normal*bow;
  }
  void DrawBranchFibers(float x0,float y0,float x1,float y1,float sx,float sy,int id)
  {
   var a=new Vector2(x0,y0);var b=new Vector2(x1,y1);var d=(b-a).normalized;var normal=new Vector2(-d.y,d.x);
   var brush=InkBrushSettings.Dense;brush.radius=3.2f;brush.roughness=.7f;brush.water=.12f;brush.pigment=.8f;brush.depletion=.01f;
   for(int twig=0;twig<5;twig++)
   {
    float t=.16f+twig*.16f,sign=(id+twig)%2==0?1:-1;
    Vector2 start=BranchStrokePoint(a,b,id,t);
    var tangent=(BranchStrokePoint(a,b,id,t+.01f)-BranchStrokePoint(a,b,id,t-.01f)).normalized;
    var side=new Vector2(-tangent.y,tangent.x);
    Vector2 end=start+tangent*(12+(id+twig)%4*5)+side*sign*(16+(id*7+twig)%5*4);
    persistentInk.Begin(new InkPoint(start.x*sx,start.y*sy,0,.8f),brush,unchecked(Run.Seed+id*193+twig));
    for(int n=1;n<=10;n++)
    {
     float u=n/10f;Vector2 p=Vector2.Lerp(start,end,u)+side*Mathf.Sin(u*Mathf.PI)*sign*4;
     persistentInk.Move(new InkPoint(p.x*sx,p.y*sy,u*.3f,Mathf.Lerp(.8f,.06f,u)));
    }
    persistentInk.End();
   }
  }
 }
}
