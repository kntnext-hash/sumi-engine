using System;
using System.IO;
using System.Collections.Generic;
using Bokumyaku.Simulation;
public static class BokumyakuOpeningEconomyCheck
{
 public static object Run(){
 var rows=new List<object>();
 foreach(int seed in new[]{42,73,101}){
 int bank=0,bestMilestone=0;var totals=new int[3];
 for(int round=0;round<3;round++){
 var run=new RunSimulator(seed+round,new MetaProgress());run.SetBuild(BuildPreset.Farming);run.SetWeapon(WeaponKind.Point);
 for(int tick=0;tick<60000&&run.State==RunStatus.Running;tick++)run.Tick();
 if(run.State==RunStatus.Running)throw new Exception("bounded run did not end");
 int milestone=run.CompletedWave/5*5;int reward=run.CrystalsReward+Math.Max(0,milestone-bestMilestone);bestMilestone=Math.Max(bestMilestone,milestone);bank+=reward;totals[round]=bank;
 rows.Add(new{seed=seed+round,round=round+1,seconds=run.TickCount/30.0,run.CompletedWave,reward,bank});
 }
 foreach(int price in new[]{500,650,800}){int purchaseRound=Array.FindIndex(totals,v=>v>=price)+1;rows.Add(new{seed,candidatePrice=price,purchaseRound,target=purchaseRound>=2&&purchaseRound<=3});}
 }
 var result=new{rows,method="Three short seed paths, three initial-meta Farming/Point runs each. Candidate prices only; no research purchased, no first-week or full QA claim."};
 File.WriteAllText(Path.GetFullPath("../docs/evidence/opening-economy.json"),Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
 }
}
