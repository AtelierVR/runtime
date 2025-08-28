Shader "Nox/Mirror"
{
    Properties
    {
        _ReflectionTexLeft ("Reflection Left", 2D) = "white" {}
        _ReflectionTexRight ("Reflection Right", 2D) = "white" {}
        _ReflectionTexMono ("Reflection Mono", 2D) = "white" {}
        _ReflectionIntensity ("Reflection Intensity", Range(0, 1)) = 1
        _Tint ("Tint", Color) = (1,1,1,1)
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        LOD 100

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 screenPos : TEXCOORD1;
                float fogCoord : TEXCOORD2;
            };

            TEXTURE2D(_ReflectionTexLeft);
            SAMPLER(sampler_ReflectionTexLeft);

            TEXTURE2D(_ReflectionTexRight);
            SAMPLER(sampler_ReflectionTexRight);

            TEXTURE2D(_ReflectionTexMono);
            SAMPLER(sampler_ReflectionTexMono);

            CBUFFER_START(UnityPerMaterial)
                float4 _ReflectionTexLeft_ST;
                float4 _ReflectionTexRight_ST;
                float4 _ReflectionTexMono_ST;
                float _ReflectionIntensity;
                float4 _Tint;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionHCS = vertexInput.positionCS;
                output.screenPos = ComputeScreenPos(output.positionHCS);
                output.uv = TRANSFORM_TEX(input.uv, _ReflectionTexLeft);
                output.fogCoord = ComputeFogFactor(output.positionHCS.z);
                
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 screenUV = input.screenPos.xy / input.screenPos.w;
                
                // Utiliser la texture de réflexion appropriée selon le contexte
                half4 reflection;
                
                // En mode stéréo, utiliser Left/Right, sinon Mono
                #if defined(UNITY_SINGLE_PASS_STEREO)
                    if (unity_StereoEyeIndex == 0)
                        reflection = SAMPLE_TEXTURE2D(_ReflectionTexLeft, sampler_ReflectionTexLeft, screenUV);
                    else
                        reflection = SAMPLE_TEXTURE2D(_ReflectionTexRight, sampler_ReflectionTexRight, screenUV);
                #else
                    reflection = SAMPLE_TEXTURE2D(_ReflectionTexMono, sampler_ReflectionTexMono, screenUV);
                #endif
                
                // Appliquer l'intensité et la teinte
                reflection.rgb *= _ReflectionIntensity;
                reflection.rgb *= _Tint.rgb;
                
                // Appliquer le brouillard
                reflection.rgb = MixFog(reflection.rgb, input.fogCoord);
                
                return reflection;
            }
            ENDHLSL
        }
    }
    
    Fallback "Universal Render Pipeline/Unlit"
}
