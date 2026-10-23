Shader "Bokumyaku/InkSprite"
{
    Properties
    {
        [PerRendererData] _MainTex ("Ink silhouette", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Hit ("Impact flash", Range(0,1)) = 0
        _Dissolve ("Paper absorption", Range(0,1)) = 0
        _Wobble ("Ink breathing", Range(0,1)) = 0
        _Phase ("Individual phase", Float) = 0
        _InkTime ("Presentation time", Float) = 0
        _ReducedMotion ("Reduced motion", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "CanUseSpriteAtlas"="True" }
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"
        TEXTURE2D(_MainTex);
        SAMPLER(sampler_MainTex);
        CBUFFER_START(UnityPerMaterial)
            float4 _Color;
            float _Hit;
            float _Dissolve;
            float _Wobble;
            float _Phase;
            float _InkTime;
            float _ReducedMotion;
        CBUFFER_END
        struct Attributes
        {
            float3 positionOS : POSITION;
            float4 color : COLOR;
            float2 uv : TEXCOORD0;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };
        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float4 color : COLOR;
            float2 uv : TEXCOORD0;
            UNITY_VERTEX_OUTPUT_STEREO
        };
        float InkHash(float2 p)
        {
            p = frac(p * float2(123.34, 456.21));
            p += dot(p, p + 45.32);
            return frac(p.x * p.y);
        }
        float InkNoise(float2 p)
        {
            float2 cell = floor(p);
            float2 t = frac(p);
            t = t * t * (3.0 - 2.0 * t);
            return lerp(lerp(InkHash(cell), InkHash(cell + float2(1,0)), t.x),
                        lerp(InkHash(cell + float2(0,1)), InkHash(cell + float2(1,1)), t.x), t.y);
        }
        Varyings InkVertex(Attributes input)
        {
            Varyings output;
            UNITY_SETUP_INSTANCE_ID(input);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
            SetUpSpriteInstanceProperties();
            input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);
            output.positionCS = TransformObjectToHClip(input.positionOS);
            output.color = input.color * _Color * unity_SpriteColor;
            output.uv = input.uv;
            return output;
        }
        half4 InkFragment(Varyings input) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            float death = saturate(_Dissolve);
            float motion = 1.0 - step(0.5, _ReducedMotion);
            float phase = _InkTime * 2.1 + _Phase;
            // Standalone full-rect sprite masks; keep two transparent pixels at their edges.
            float impact = saturate(_Hit) * motion;
            float2 uv = (input.uv - 0.5) * (1.0 + death * 0.7 - impact * 0.06) + 0.5;
            float2 ripple = float2(sin(phase + uv.y * 15.0), cos(phase * 0.83 + uv.x * 13.0));
            uv += ripple * (0.0035 * _Wobble * motion * (1.0 - death));
            half4 ink = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
            float grain = InkNoise(input.uv * 62.0 + _Phase);
            float erosion = smoothstep(death - 0.10, death + 0.16, grain);
            float coverage = lerp(1.0, erosion, saturate(death * 4.0));
            float inBounds = step(0.0, uv.x) * step(uv.x, 1.0) * step(0.0, uv.y) * step(uv.y, 1.0);
            ink *= input.color;
            ink.rgb = lerp(ink.rgb, half3(1,1,1), saturate(_Hit) * motion);
            ink.a *= coverage * (1.0 - death) * inBounds;
            return ink;
        }
        ENDHLSL

        Pass
        {
            Name "Ink2D"
            Tags { "LightMode"="Universal2D" }
            HLSLPROGRAM
            #pragma vertex InkVertex
            #pragma fragment InkFragment
            #pragma multi_compile_instancing
            ENDHLSL
        }
        Pass
        {
            Name "InkForward"
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex InkVertex
            #pragma fragment InkFragment
            #pragma multi_compile_instancing
            ENDHLSL
        }
    }
    Fallback Off
}


