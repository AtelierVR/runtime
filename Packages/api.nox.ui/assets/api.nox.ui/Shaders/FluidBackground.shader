Shader "Custom/FluidBackground"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color1 ("Color 1", Color) = (0.2, 0.5, 1.0, 1.0)
        _Color2 ("Color 2", Color) = (1.0, 0.3, 0.6, 1.0)
        _Speed ("Speed", Range(0.1, 5.0)) = 1.0
        _Scale ("Scale", Range(0.1, 10.0)) = 2.0
        _Complexity ("Complexity", Range(1, 5)) = 3
        _FluidStrength ("Fluid Strength", Range(0.1, 2.0)) = 1.0
        _Steps ("Steps", Range(1, 10)) = 1
        [Toggle] _UseGlobalPosition ("Use Global Position", Float) = 1
        [Toggle] _UseTexture ("Use Texture", Float) = 0
        [Toggle] _UsePixelNoise ("Use Pixel Noise", Float) = 0
        _PixelNoiseStrength ("Pixel Noise Strength", Range(0.0, 1.0)) = 0.05
        
        [HideInInspector] _StencilComp ("Stencil Comparison", Float) = 8
        [HideInInspector] _Stencil ("Stencil ID", Float) = 0
        [HideInInspector] _StencilOp ("Stencil Operation", Float) = 0
        [HideInInspector] _StencilWriteMask ("Stencil Write Mask", Float) = 255
        [HideInInspector] _StencilReadMask ("Stencil Read Mask", Float) = 255
        [HideInInspector] _ColorMask ("Color Mask", Float) = 15
        [HideInInspector] unity_GUIZTestMode ("ZTest Mode", Float) = 4
    }
    
    SubShader
    {
        Tags 
        { 
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }
        
        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }
        
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 worldPosition : TEXCOORD1;
                float4 screenPos : TEXCOORD2;
                float4 color : COLOR;
            };

            float4 _Color1;
            float4 _Color2;
            float _Speed;
            float _Scale;
            float _Complexity;
            float _FluidStrength;
            float _UseGlobalPosition;
            float _UseTexture;
            float _UsePixelNoise;
            float _PixelNoiseStrength;
            int _Steps;
            
            sampler2D _MainTex;
            float4 _MainTex_ST;
            
            float4 _ClipRect;

            v2f vert (appdata v)
            {
                v2f o;
                o.worldPosition = v.vertex;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.screenPos = ComputeScreenPos(o.vertex);
                
                #ifdef UNITY_UI_CLIP_RECT
                o.worldPosition = v.vertex;
                #endif
                
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            float hash(float2 p)
            {
                return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
            }
            
            float betterNoise(float2 p)
            {
                float3 p3 = frac(float3(p.xyx) * float3(443.897, 441.423, 437.195));
                p3 += dot(p3, p3.yzx + 19.19);
                return frac((p3.x + p3.y) * p3.z);
            }

            float perlinNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                
                float a = hash(i);
                float b = hash(i + float2(1.0, 0.0));
                float c = hash(i + float2(0.0, 1.0));
                float d = hash(i + float2(1.0, 1.0));
                
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            float fbm(float2 p)
            {
                float value = 0.0;
                float amplitude = 0.5;
                
                [unroll]
                for(int i = 0; i < 5; i++)
                {
                    if (i >= _Complexity) break;
                    value += amplitude * perlinNoise(p);
                    p *= 2.0;
                    amplitude *= 0.5;
                }
                
                return value;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Precompute time values
                float time = _Time.y * _Speed;
                float slowTime1 = time * 0.2;
                float slowTime2 = time * 0.15;
                float slowTime3 = time * 0.3;
                float slowTime4 = time * 0.2;
                
                // Choose between global position (screen space) or local UV
                float2 uv = (_UseGlobalPosition > 0.5) ? 
                    (i.screenPos.xy / i.screenPos.w * _Scale * 0.1) : 
                    (i.uv * _Scale);
                
                // Create smoother fluid patterns in motion
                float2 p = uv + float2(sin(slowTime1) * 0.3, cos(slowTime2) * 0.3);
                
                // Precompute constant offsets
                static const float2 offset1 = float2(3.2, 2.3);
                static const float2 offset2 = float2(1.7, 9.2);
                static const float2 offset3 = float2(8.3, 2.8);
                
                // Use fewer layers for smoother motion
                float2 q = float2(
                    fbm(p),
                    fbm(p + offset1)
                );
                
                // Precompute fluid factor
                float fluidFactor = _FluidStrength * 0.5;
                float2 qScaled = fluidFactor * q;
                
                // Reduce complexity for more fluidity
                float2 r = float2(
                    fbm(p + qScaled + offset2 + 0.1 * time),
                    fbm(p + qScaled + offset3 + 0.08 * time)
                );
                
                // Create smooth fluid pattern
                float f = fbm(p + _FluidStrength * 0.3 * r);
                
                // Add large smooth wave to vary separation
                float wave = sin(uv.x * 2.0 + slowTime3) * cos(uv.y * 1.5 - slowTime4) * 0.15;
                
                // Combine with noise for smooth but organic boundary
                float separation = f + wave;
                
                // Multiply separation by number of steps to create repetitions
                float repeatedSeparation = frac(separation * _Steps); // frac is more efficient than fmod
                
                // Very fine transition to avoid clear line
                float mixFactor = smoothstep(0.495, 0.505, repeatedSeparation); // Precomputed threshold ± edge
                
                // Select color based on separation - sharp transition
                fixed4 col = (mixFactor > 0.5) ? _Color2 : _Color1;
                
                // If using texture, sample and combine it
                if (_UseTexture > 0.5)
                {
                    fixed4 texColor = tex2D(_MainTex, i.uv);
                    col.rgb *= texColor.rgb;
                    col.a *= texColor.a;
                }
                
                // Apply vertex color (for UI compatibility)
                col *= i.color;
                
                // Calculate UI mask alpha
                #ifdef UNITY_UI_CLIP_RECT
                col.a *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);
                #endif
                
                // Apply per-pixel noise if enabled
                if (_UsePixelNoise > 0.5)
                {
                    // Generate unique noise for each pixel based on screen position
                    float2 pixelPos = i.screenPos.xy / i.screenPos.w * _ScreenParams.xy;
                    float pixelNoise = (betterNoise(pixelPos) - 0.5) * 2.0; // Combine in one line
                    
                    // Apply noise to RGB colors
                    col.rgb = saturate(col.rgb + pixelNoise * _PixelNoiseStrength);
                }
                
                #ifdef UNITY_UI_ALPHACLIP
                clip (col.a - 0.001);
                #endif
                
                return col;
            }
            ENDCG
        }
    }
    
    FallBack "Diffuse"
}
