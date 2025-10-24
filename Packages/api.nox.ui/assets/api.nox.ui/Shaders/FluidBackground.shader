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
        [Toggle] _UseGlobalPosition ("Use Global Position", Float) = 1
        [Toggle] _UseTexture ("Use Texture", Float) = 0
        
        // Propriétés pour le masque UI
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
        
        // Support du stencil pour les masques UI
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

            // Fonction de bruit simple
            float noise(float2 p)
            {
                return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
            }

            // Bruit de Perlin simplifié
            float perlinNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                
                float a = noise(i);
                float b = noise(i + float2(1.0, 0.0));
                float c = noise(i + float2(0.0, 1.0));
                float d = noise(i + float2(1.0, 1.0));
                
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            // Bruit fractal
            float fbm(float2 p)
            {
                float value = 0.0;
                float amplitude = 0.5;
                float frequency = 1.0;
                
                for(int i = 0; i < _Complexity; i++)
                {
                    value += amplitude * perlinNoise(p * frequency);
                    frequency *= 2.0;
                    amplitude *= 0.5;
                }
                
                return value;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Choisir entre position globale (screen space) ou UV local
                float2 uv;
                if (_UseGlobalPosition > 0.5)
                {
                    // Utiliser la position écran normalisée pour un effet global
                    uv = i.screenPos.xy / i.screenPos.w;
                    // Ajuster l'échelle pour l'espace écran (généralement plus grand)
                    uv *= _Scale * 0.1;
                }
                else
                {
                    // Utiliser les UVs locaux
                    uv = i.uv * _Scale;
                }
                
                float time = _Time.y * _Speed;
                
                // Créer des motifs fluides plus lisses en mouvement
                float2 p = uv + float2(sin(time * 0.2) * 0.3, cos(time * 0.15) * 0.3);
                
                // Utiliser moins de couches pour un mouvement plus lisse
                float2 q = float2(
                    fbm(p + float2(0.0, 0.0)),
                    fbm(p + float2(3.2, 2.3))
                );
                
                // Réduire la complexité pour plus de fluidité
                float2 r = float2(
                    fbm(p + _FluidStrength * 0.5 * q + float2(1.7, 9.2) + 0.1 * time),
                    fbm(p + _FluidStrength * 0.5 * q + float2(8.3, 2.8) + 0.08 * time)
                );
                
                // Créer un motif fluide lisse
                float f = fbm(p + _FluidStrength * 0.3 * r);
                
                // Ajouter une grande vague lisse pour varier la séparation
                float wave = sin(uv.x * 2.0 + time * 0.3) * cos(uv.y * 1.5 - time * 0.2) * 0.15;
                
                // Combiner avec le bruit pour une frontière lisse mais organique
                float separation = f + wave;
                
                // Seuil fixe pour une séparation stable
                float threshold = 0.5;
                
                // Transition très fine pour éviter la ligne claire
                float edge = 0.005; // Très petite largeur de transition
                float mixFactor = smoothstep(threshold - edge, threshold + edge, separation);
                
                // Sélectionner la couleur basée sur la séparation - transition nette
                fixed4 col = mixFactor > 0.5 ? _Color2 : _Color1;
                
                // Si on utilise la texture, l'échantillonner et la combiner
                if (_UseTexture > 0.5)
                {
                    fixed4 texColor = tex2D(_MainTex, i.uv);
                    // Multiplier la texture par l'effet fluide
                    col.rgb *= texColor.rgb;
                    col.a *= texColor.a;
                }
                
                // Calculer l'alpha du masque UI en premier
                float maskAlpha = 1.0;
                
                #ifdef UNITY_UI_CLIP_RECT
                maskAlpha = UnityGet2DClipping(i.worldPosition.xy, _ClipRect);
                #endif
                
                // Appliquer la couleur et l'alpha du vertex (pour la compatibilité UI)
                col.rgb *= i.color.rgb;
                col.a *= i.color.a;
                
                // Appliquer l'alpha du masque
                col.a *= maskAlpha;
                
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
