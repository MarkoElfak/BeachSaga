// Soft drifting fog over locked land, for URP. Two layers of world-space noise scroll slowly in different
// directions; the mesh's vertex alpha fades the fog out at a piece's edges. _Reveal (0..1, set per renderer)
// dissolves it patchily when the land opens, and _Layer shifts the noise so stacked layers differ.
Shader "BeachTycoon/Fog"
{
    Properties
    {
        _LightColor ("Light Color", Color) = (0.97, 0.98, 1, 1)
        _ShadeColor ("Shade Color", Color) = (0.74, 0.79, 0.86, 1)
        _NoiseTex ("Noise (R)", 2D) = "gray" {}
        _Scale ("Noise Scale", Float) = 0.045
        _Drift ("Drift (xy, zw)", Vector) = (0.012, 0.006, -0.008, 0.01)
        _Density ("Density", Range(0, 1)) = 0.86
        _Billow ("Billow", Range(0, 1)) = 0.55
        _Reveal ("Reveal", Range(0, 1)) = 0
        _Layer ("Layer", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent+20" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }

        Pass
        {
            Name "Fog"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_NoiseTex);
            SAMPLER(sampler_NoiseTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _LightColor;
                float4 _ShadeColor;
                float4 _NoiseTex_ST;
                float4 _Drift;
                float _Scale;
                float _Density;
                float _Billow;
                float _Reveal;
                float _Layer;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float edge : TEXCOORD1;
                float fogFactor : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.edge = input.color.a;
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 p = input.positionWS.xz * _Scale + _Layer * float2(0.37, 0.71);
                float n1 = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, p + _Time.y * _Drift.xy).r;
                float n2 = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, p * 2.3 + _Time.y * _Drift.zw).r;
                float cloud = saturate(n1 * 0.65 + n2 * 0.45);

                // Thick in the middle, billowing at the edges, dissolving in patches as it is revealed.
                // Thinner, shaded patches between the puffs let the land below show faintly.
                float puff = smoothstep(0.25, 0.8, cloud);
                float body = saturate(input.edge * (1.0 + _Billow) - (1.0 - cloud) * _Billow);
                float keep = saturate((cloud + 0.35 - _Reveal * 1.4) * 4.0);
                float alpha = body * _Density * lerp(0.7, 1.0, puff) * keep;

                Light light = GetMainLight();
                float3 color = lerp(_ShadeColor.rgb, _LightColor.rgb, puff) * (0.7 + 0.3 * light.color);
                color = MixFog(color, input.fogFactor);
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
