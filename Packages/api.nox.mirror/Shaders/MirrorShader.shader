Shader "Nox/MirrorShader"
{
    Properties
    {
        _LeftEyeTexture ("Left Eye Texture", 2D) = "white" {}
        _RightEyeTexture ("Right Eye Texture", 2D) = "white" {}
    }
    
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 200
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            
            #include "UnityCG.cginc"
            
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 screenPos : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            
            sampler2D _LeftEyeTexture;
            sampler2D _RightEyeTexture;
            float4 _LeftEyeTexture_ST;
            float4 _RightEyeTexture_ST;
            
            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _LeftEyeTexture);
                o.screenPos = ComputeScreenPos(o.vertex);
                
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                
                // Flip UV horizontally for mirror effect
                float2 mirrorUV = float2(1.0 - i.uv.x, i.uv.y);
                
                fixed4 col;
                
                // Check which eye we're rendering for
                #if defined(UNITY_SINGLE_PASS_STEREO) || defined(UNITY_STEREO_INSTANCING_ENABLED)
                    if (unity_StereoEyeIndex == 0)
                    {
                        // Left eye
                        col = tex2D(_LeftEyeTexture, mirrorUV);
                    }
                    else
                    {
                        // Right eye
                        col = tex2D(_RightEyeTexture, mirrorUV);
                    }
                #else
                    // Non-VR fallback - use left eye texture
                    col = tex2D(_LeftEyeTexture, mirrorUV);
                #endif
                
                return col;
            }
            ENDCG
        }
    }
    
    FallBack "Diffuse"
}
