using ConnectPiece.SumiEngine.Core;
using UnityEngine;

namespace ConnectPiece.SumiEngine
{
    [CreateAssetMenu(fileName = "SumiBrush", menuName = "SUMI Engine/Brush Preset")]
    public sealed class SumiBrushPreset : ScriptableObject
    {
        [Min(0.6f)] public float radius = 9f;
        [Range(0,1)] public float pigment = .72f;
        [Range(0,1)] public float water = .42f;
        [Range(0,1)] public float roughness = .42f;
        [Min(0)] public float depletion = .005f;
        [Range(0,1)] public float bristleSpread = .9f;
        [Range(3,31)] public int bristleCount = 11;
        public InkBrushSettings ToSettings() => new InkBrushSettings
        {
            radius = radius, pigment = pigment, water = water, roughness = roughness,
            depletion = depletion, bristleSpread = bristleSpread, bristleCount = bristleCount
        };
    }
}
