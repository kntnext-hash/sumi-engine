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
                    portraitDetails = false;
                    if (ResumeAvailable) { SetPage(GamePage.Battle); ResumeRun(); }
                    else TryStartRun(AwaitingStart ? InitialSeed : unchecked(Run.Seed + 1));
                }
                DrawPanel(new Rect(16, 76, 408, height - 88), true);
                return;
            }
            Rect field = new Rect(0, 62, 440, height - 260);
            // Clip branch textures and rotated brush effects before drawing the fixed controls.
            GUI.BeginGroup(field);
            try { DrawField(new Rect(0, 0, field.width, field.height), true); }
            finally { GUI.EndGroup(); }
            DrawBattleHud(height);
            if (portraitDetails)
            {
                Rect sheet = new Rect(0, 62, 440, height - 260);
                Fill(sheet, new Color(.96f, .95f, .91f, .98f));
                DrawPanel(new Rect(20, 100, 400, sheet.height - 24), true);
            }
        }
    }
}
