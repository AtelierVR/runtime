Shader "Nox/MirrorShader"
{
    Properties
    {
        [HideInInspector] _LeftEyeTexture ("Left Eye Texture", 2D) = "white" {}
        [HideInInspector] _RightEyeTexture ("Right Eye Texture", 2D) = "white" {}
        _Color ("Tint Color", Color) = (1,1,1,1)
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType" = "Opaque" 
            "Queue" = "Geometry"
            "RenderPipeline" = "UniversalPipeline"
        }
        LOD 100
        
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            
            // XR/Stereo rendering keywords
            #pragma multi_compile_instancing
            #pragma instancing_options renderinglayer
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 screenPos : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };
            
            TEXTURE2D(_LeftEyeTexture);
            SAMPLER(sampler_LeftEyeTexture);
            TEXTURE2D(_RightEyeTexture);
            SAMPLER(sampler_RightEyeTexture);
            
            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
            CBUFFER_END
            
            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.screenPos = ComputeScreenPos(output.positionCS);
                
                return output;
            }
            
            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                
                // Calculate screen UV from screen position
                float2 screenUV = input.screenPos.xy / input.screenPos.w;
                
                half4 reflectionColor;
                
                // Detect which eye we're rendering for using projection matrix
                // Left eye has projection[0][2] <= 0, right eye has > 0
                if (unity_CameraProjection[0][2] <= 0)
                {
                    reflectionColor = SAMPLE_TEXTURE2D(_LeftEyeTexture, sampler_LeftEyeTexture, screenUV);
                }
                else
                {
                    reflectionColor = SAMPLE_TEXTURE2D(_RightEyeTexture, sampler_RightEyeTexture, screenUV);
                }
                
                // Apply tint color
                reflectionColor *= _Color;
                
                return reflectionColor;
            }
            ENDHLSL
        }
        
        // Depth only pass for shadows
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            
            ZWrite On
            ColorMask 0
            
            HLSLPROGRAM
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment
            
            #pragma multi_compile_instancing
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            struct Attributes
            {
                float4 position : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };
            
            Varyings DepthOnlyVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                
                output.positionCS = TransformObjectToHClip(input.position.xyz);
                return output;
            }
            
            half4 DepthOnlyFragment(Varyings input) : SV_TARGET
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                return 0;
            }
            ENDHLSL
        }
    }
    
    // Fallback for built-in render pipeline
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 100
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_instancing
            
            #include "UnityCG.cginc"
            
            struct appdata
            {
                float4 vertex : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            struct v2f
            {
                float4 pos : SV_POSITION;
                float4 screenPos : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            
            sampler2D _LeftEyeTexture;
            sampler2D _RightEyeTexture;
            fixed4 _Color;
            
            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                
                o.pos = UnityObjectToClipPos(v.vertex);
                o.screenPos = ComputeScreenPos(o.pos);
                
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                
                // Calculate screen UV
                float2 screenUV = i.screenPos.xy / i.screenPos.w;
                
                fixed4 col;
                
                // Detect eye using projection matrix (like MVRMirror)
                if (unity_CameraProjection[0][2] <= 0)
                {
                    col = tex2D(_LeftEyeTexture, screenUV);
                }
                else
                {
                    col = tex2D(_RightEyeTexture, screenUV);
                }
                
                return col * _Color;
            }
            ENDCG
        }
    }
    
    FallBack "Diffuse"
}
