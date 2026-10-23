using UnityEngine;
namespace Bokumyaku.Presentation
{
 public sealed partial class InkBattleWorld
 {
  void DrawDrain(EnemyView view,Vector2 source,Vector2 target,bool reduced)
  {
   bool active=view.threat.enabled;
   for(int i=0;i<view.drain.Length;i++)view.drain[i].enabled=active;
   if(!active)return;
   // The core loses life to the creature; the particles travel in that direction.
   float bow=(view.state.Id%2==0?1:-1)*.24f;
   for(int i=0;i<6;i++)
   {
    var a=InkFlowPath.Point(source,target,i/6f,bow);
    var b=InkFlowPath.Point(source,target,(i+1)/6f,bow);
    var d=b-a;var part=view.drain[i];
    Size(part,(a+b)*.5f,d.magnitude/.75f+.04f,.12f);
    part.transform.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(d.y,d.x)*Mathf.Rad2Deg);
    part.color=new Color(.12f,.015f,.025f,.9f);
    Properties(part,0,0,0,0,reduced);
   }
   for(int i=0;i<3;i++)
   {
    float t=reduced?(i+.5f)/3f:Mathf.Repeat(visualTime*.7f+i/3f,1);
    var part=view.drain[6+i];float size=.08f+.06f*Mathf.Sin(t*Mathf.PI);
    Size(part,InkFlowPath.Point(source,target,t,bow),size,size);
    part.color=new Color(1,1,1,.85f);
    Properties(part,0,0,0,0,reduced);
   }
  }
 }
 // Geometry only: reusable for flowing ink between arbitrary drawing anchors.
 public static class InkFlowPath
 {
  public static Vector2 Point(Vector2 source,Vector2 target,float progress,float bow)
  {
   float t=Mathf.Clamp01(progress);var d=target-source;
   var normal=d.sqrMagnitude>.000001f?new Vector2(-d.y,d.x).normalized:Vector2.zero;
   return Vector2.Lerp(source,target,t)+normal*(4*t*(1-t)*bow);
  }
 }
}
