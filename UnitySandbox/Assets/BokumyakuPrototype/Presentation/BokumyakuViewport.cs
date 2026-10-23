using UnityEngine;

namespace Bokumyaku.Presentation
{
    /// <summary>One portrait area for camera and UI, including resized desktop windows.</summary>
    public static class BokumyakuViewport
    {
        public static Rect Fit(Rect safe)
        {
            float width=Mathf.Min(safe.width,safe.height*9f/16f);
            return new Rect(safe.x+(safe.width-width)*.5f,safe.y,width,safe.height);
        }

        public static Rect Current=>Fit(Screen.safeArea);

        public static void Apply(RectTransform target,UnityEngine.UI.CanvasScaler scaler)
        {
            var area=Current;
            target.anchorMin=new Vector2(area.xMin/Screen.width,area.yMin/Screen.height);
            target.anchorMax=new Vector2(area.xMax/Screen.width,area.yMax/Screen.height);
            // Match-width scaling must use the fitted area, not the entire desktop width.
            var reference=scaler.referenceResolution;
            reference.x=440f*Screen.width/Mathf.Max(1,area.width);
            scaler.referenceResolution=reference;
        }
    }
}
