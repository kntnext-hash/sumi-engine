using UnityEngine;

namespace Bokumyaku.Presentation.Drawing
{
    /// <summary>Texture-free circular backing shared by gauges and round controls.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class UiDiscGraphic : UnityEngine.UI.MaskableGraphic
    {
        protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper mesh)
        {
            mesh.Clear();
            var rect=GetPixelAdjustedRect();
            float radius=Mathf.Min(rect.width,rect.height)*.5f;
            var center=rect.center;
            mesh.AddVert(center,color,Vector2.zero);
            const int segments=96;
            for(int i=0;i<=segments;i++)
            {
                float angle=i*Mathf.PI*2/segments;
                mesh.AddVert(center+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*radius,color,Vector2.zero);
                if(i>0)mesh.AddTriangle(0,i,i+1);
            }
        }
    }
}
