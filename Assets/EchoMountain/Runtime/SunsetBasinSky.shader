Shader "Echo Mountain/Sunset Basin Sky"
{
    Properties
    {
        _Zenith ("Zenith", Color) = (0.23, 0.29, 0.42, 1)
        _Horizon ("Horizon", Color) = (0.96, 0.48, 0.20, 1)
        _Mist ("Mist", Color) = (0.85, 0.53, 0.40, 1)
        _SunDirection ("Sun Direction", Vector) = (0.22, 0.14, 0.97, 0)
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off ZWrite Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Zenith, _Horizon, _Mist;
                float4 _SunDirection;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 direction : TEXCOORD0; };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.direction = input.positionOS.xyz;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float3 direction = normalize(input.direction);
                float towardSun = saturate(dot(direction, normalize(_SunDirection.xyz)));
                half3 sky = lerp(_Horizon.rgb, _Zenith.rgb, smoothstep(0.02, 0.72, direction.y));
                sky = lerp(sky, _Mist.rgb, 0.94h * exp(-max(0.0, direction.y + 0.012) * 16.0));
                float bands = sin(direction.y * 71.0 + sin(direction.x * 12.0) * 0.62);
                float breaks = smoothstep(-0.7, 0.85, sin(direction.x * 21.0 + direction.y * 24.0));
                float wisps = smoothstep(0.40, 0.98, bands) * smoothstep(0.07, 0.15, direction.y)
                    * (1.0 - smoothstep(0.23, 0.45, direction.y)) * lerp(0.22, 1.0, breaks);
                sky += half3(0.12h, 0.053h, 0.027h) * wisps;
                sky += half3(0.29h, 0.11h, 0.022h) * pow(towardSun, 32.0);
                sky += half3(0.26h, 0.10h, 0.014h) * pow(towardSun, 330.0);
                sky = lerp(sky, half3(1.0h, 0.81h, 0.43h), smoothstep(0.99925, 0.99944, towardSun));
                return half4(sky, 1);
            }
            ENDHLSL
        }
    }
}
