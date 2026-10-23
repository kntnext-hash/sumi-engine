using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Bokumyaku.Simulation;
namespace Bokumyaku.Tests {
public class DefeatNotificationTests {
static void Hurt(RunSimulator r,EnemyState e){typeof(RunSimulator).GetMethod("Hurt",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(r,new object[]{e,1e8d});}
[Test] public void SameTickSplitChildrenAreReportedExactlyOnce(){
var r=new RunSimulator(42);
typeof(RunSimulator).GetMethod("Spawn",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(r,new object[]{(EnemyKind?)EnemyKind.Splitter});
var parent=r.Enemies.Single();Hurt(r,parent);var children=r.Enemies.ToArray();
Assert.That(children.Length,Is.EqualTo(2));foreach(var child in children)Hurt(r,child);Hurt(r,parent);
Assert.That(r.DefeatedThisTick.Count,Is.EqualTo(3));Assert.That(r.Kills,Is.EqualTo(3));
Assert.That(r.DefeatedThisTick.Select(e=>e.Id).Distinct().Count(),Is.EqualTo(3));
Assert.That(r.DefeatedThisTick.Count(e=>e.IsSplitChild),Is.EqualTo(2));
Assert.That(r.Enemies.Count,Is.Zero);
}
[Test] public void NotificationsAreTransientAndNotReplayedOnRestore(){
var r=new RunSimulator(42);typeof(RunSimulator).GetMethod("Spawn",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(r,new object[]{(EnemyKind?)EnemyKind.Normal});
Hurt(r,r.Enemies.Single());var restored=RunSimulator.Restore(r.CaptureState());
Assert.That(restored.DefeatedThisTick.Count,Is.Zero);Assert.That(restored.Kills,Is.EqualTo(1));
r.Tick();Assert.That(r.DefeatedThisTick.Count,Is.Zero);
}
}}
