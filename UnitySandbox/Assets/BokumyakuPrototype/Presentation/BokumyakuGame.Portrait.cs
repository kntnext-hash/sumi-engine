using Bokumyaku.Simulation;
using UnityEngine;

namespace Bokumyaku.Presentation
{
    public sealed partial class BokumyakuGame
    {
        bool portraitDetails;
        // Presentation only: coordinates, prices and purchases still come from the existing simulation.
        void DrawPortrait(float height)
        {
            Fill(new Rect(0, 0, 440, height), paper);
            DrawArt(new Rect(0, 0, 440, height), paperArt, .5f, ScaleMode.ScaleAndCrop);
            if (Page != GamePage.Battle)
            {
                GUI.Label(new Rect(16, 12, 220, 42), "墨 脈", title);
                if (Button(new Rect(272, 12, 152, 46), ResumeAvailable ? "戦場へ戻る" : "出撃する", true))
                {
                    if (ResumeAvailable) { SetPage(GamePage.Battle); ResumeRun(); }
                    else TryStartRun(AwaitingStart ? InitialSeed : unchecked(Run.Seed + 1));
                }
                DrawPanel(new Rect(16, 76, 408, height - 88), true);
                return;
            }
            Rect field = new Rect(8, 88, 424, height - 258);
            DrawField(field, true);
            Fill(new Rect(0, 0, 440, 86), new Color(.96f, .94f, .88f, .97f));
            GUI.Label(new Rect(16, 8, 210, 34), "WAVE " + Run.Wave, number);
            GUI.Label(new Rect(16, 43, 292, 30), "墨 " + Run.RunInk + "   結晶 " + Meta.Crystals, heading);
            if (Button(new Rect(318, 14, 106, 54), IsPaused ? "再開" : "一時停止", IsPaused)) SetPaused(!IsPaused);
            float controls = height - 162;
            GUI.Label(new Rect(16, controls, 138, 26), "墨核 " + Run.CoreHp + " / 10", heading);
            GUI.Label(new Rect(16, controls + 27, 138, 22), FormatTime(Run.ElapsedSeconds) + " · " + WeaponNames[(int)Run.Weapon], small);
            if (Button(new Rect(156, controls, 72, 48), "×" + Speed, Speed == 2)) SetSpeed(Speed == 1 ? 2 : 1);
            if (Button(new Rect(236, controls, 108, 48), AutoRepeatEnabled ? "周回 ON" : "周回 OFF", AutoRepeatEnabled)) SetAutoRepeat(!AutoRepeatEnabled);
            if (Button(new Rect(352, controls, 72, 48), portraitDetails ? "閉じる" : "詳細", portraitDetails)) portraitDetails = !portraitDetails;
            for (int i = 0; i < 4; i++)
            {
                var kind = (UpgradeKind)i;
                int level = Run.UpgradeLevels[i], cap = Run.UpgradeMaxLevel(kind), cost = Run.UpgradeCost(kind);
                GUI.enabled = !loadBlocked && !storageFailed && !AwaitingStart && Run.State == RunStatus.Running && level < cap && Run.RunInk >= cost;
                string label = UpgradeNames[i] + "  Lv." + level + "\n" + (level >= cap ? "MAX" : "墨 " + cost);
                if (Button(new Rect(12 + i * 107, height - 102, 101, 70), label, false)) BuyUpgrade(kind);
                GUI.enabled = true;
            }
            GUI.Label(new Rect(16, height - 29, 408, 24), string.IsNullOrEmpty(saveWarning) ? notice : "保存待ち：" + saveWarning, small);
            if (portraitDetails)
            {
                Rect sheet = new Rect(8, 88, 424, height - 258);
                Fill(sheet, new Color(.96f, .95f, .91f, .98f));
                DrawPanel(new Rect(20, 100, 400, sheet.height - 24), true);
            }
        }
    }
}
