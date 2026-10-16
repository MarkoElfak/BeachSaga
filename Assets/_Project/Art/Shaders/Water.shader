// Stylised beach water for URP. No depth texture needed: the water mesh carries a shore mask in its vertex
// colours (R = shore, G = depth, B = wave mask), baked from the terrain height on the CPU.
Shader "BeachTycoon/Water"
{
    Properties
    {
        _ShallowColor ("Shallow Color", Color) = (0.42, 0.86, 0.88, 0.45)
        _DeepColor ("Deep Color", Color) = (0.1, 0.42, 0.7, 0.94)
        _FoamColor ("Foam Color", Color) = (1, 1, 1, 1)
        _NoiseTex ("Noise (R)", 2D) = "gray" {}
        _WaveAmplitude ("Wave Amplitude", Float) = 0.06
        _WaveLength ("Wave Length", Float) = 7
        _WaveSpeed ("Wave Speed", Float) = 1.1
        _RippleScale ("Ripple Scale", Float) = 1
        _RippleStrength ("Ripple Strength", Range(0, 1)) = 0.35
        _FoamBands ("Foam Bands", Float) = 2.5
        _FoamSpeed ("Foam Speed", Float) = 0.22
        _FoamWidth ("Foam Width", Range(0.01, 1)) = 0.28
        _Smoothness ("Smoothness", Range(0, 1)) = 0.92
        _SkyReflection ("Sky Reflection", Range(0, 1)) = 0.35
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }

        Pass
        {
            Name "Water"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

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
                float4 _ShallowColor;
                float4 _DeepColor;
                float4 _FoamColor;
                float4 _NoiseTex_ST;
                float _WaveAmplitude;
                float _WaveLength;
                float _WaveSpeed;
                float _RippleScale;
                float _RippleStrength;
                float _FoamBands;
                float _FoamSpeed;
                float _FoamWidth;
                float _Smoothness;
                float _SkyReflection;
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
                float3 normalWS : TEXCOORD1;
                float4 mask : TEXCOORD2;
                float fogFactor : TEXCOORD3;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            // Two crossing sine waves; returns height and its xz slope.
            float Waves(float2 p, out float2 slope)
            {
                float k = 6.2831853 / max(_WaveLength, 0.01);
                float t = _Time.y * _WaveSpeed;
                float2 d1 = float2(0.28, 0.96);
                float2 d2 = float2(-0.6, 0.8);
                float a1 = dot(d1, p) * k + t;
                float a2 = dot(d2, p) * k * 1.7 + t * 1.3;
                float amp2 = _WaveAmplitude * 0.45;
                slope = d1 * cos(a1) * _WaveAmplitude * k + d2 * cos(a2) * amp2 * k * 1.7;
                return sin(a1) * _WaveAmplitude + sin(a2) * amp2;
            }

            Varyings Vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float2 slope;
                // Calmer near the shore, flat at the mesh border so the skirt joins seamlessly.
                float waveMask = input.color.b * lerp(0.35, 1.0, input.color.g);
                positionWS.y += Waves(positionWS.xz, slope) * waveMask;
                slope *= waveMask;

                output.positionWS = positionWS;
                output.normalWS = normalize(float3(-slope.x, 1.0, -slope.y));
                output.positionCS = TransformWorldToHClip(positionWS);
                output.mask = input.color;
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float shore = input.mask.r;
                float depth = input.mask.g;
                float2 uv = input.positionWS.xz * _NoiseTex_ST.xy * 0.1 * _RippleScale;
                float n1 = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, uv + _Time.y * float2(0.021, 0.034)).r;
                float n2 = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, uv * 1.7 - _Time.y * float2(0.03, 0.012)).r;

                float3 normalWS = normalize(input.normalWS + float3(n1 - 0.5, 0.0, n2 - 0.5) * _RippleStrength);
                float3 viewDirWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                Light light = GetMainLight();

                float4 water = lerp(_ShallowColor, _DeepColor, saturate(depth));
                float ndl = saturate(dot(normalWS, light.direction));
                float3 ambient = SampleSH(normalWS);
                float3 color = water.rgb * (light.color * (0.45 + 0.55 * ndl) + ambient * 0.6);

                float fresnel = pow(1.0 - saturate(dot(normalWS, viewDirWS)), 4.0);
                color += fresnel * _SkyReflection * SampleSH(float3(0.0, 1.0, 0.0));

                // Foam: bands rolling toward the beach, plus a bright line at the waterline.
                float bands = frac(shore * _FoamBands - _Time.y * _FoamSpeed + n1 * 0.35);
                float rolling = smoothstep(1.0 - _FoamWidth, 1.0, bands) * smoothstep(0.3, 0.75, shore) * 0.75;
                float edge = smoothstep(0.8, 0.96, shore + (n2 - 0.5) * 0.18);
                float foam = saturate(max(rolling, edge));
                color = lerp(color, _FoamColor.rgb * (light.color * 0.85 + ambient * 0.4), foam);

                float3 halfDir = normalize(light.direction + viewDirWS);
                float specular = pow(saturate(dot(normalWS, halfDir)), exp2(10.0 * _Smoothness + 1.0)) * _Smoothness;
                color += specular * light.color * (1.0 - foam) * 1.5;

                float alpha = saturate(max(water.a, foam) + fresnel * 0.25);
                color = MixFog(color, input.fogFactor);
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
