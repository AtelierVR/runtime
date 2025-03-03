Shader "Custom/LayeredWavesGlobal"
{
    Properties
    {
        // Couleurs de chaque vague
        _WaveColor1("Wave Color 1", Color) = (0.2, 0.2, 1.0, 1.0)
        _WaveColor2("Wave Color 2", Color) = (0.3, 0.3, 0.9, 1.0)
        _WaveColor3("Wave Color 3", Color) = (0.4, 0.4, 0.8, 1.0)
        _WaveColor4("Wave Color 4", Color) = (0.5, 0.5, 0.7, 1.0)
        _WaveColor5("Wave Color 5", Color) = (0.6, 0.6, 0.6, 1.0)

        // Amplitudes locales
        _Amplitude1("Amplitude 1", Float) = 0.05
        _Amplitude2("Amplitude 2", Float) = 0.05
        _Amplitude3("Amplitude 3", Float) = 0.05
        _Amplitude4("Amplitude 4", Float) = 0.05
        _Amplitude5("Amplitude 5", Float) = 0.05

        // Fréquences locales
        _Frequency1("Frequency 1", Float) = 4.0
        _Frequency2("Frequency 2", Float) = 3.0
        _Frequency3("Frequency 3", Float) = 2.0
        _Frequency4("Frequency 4", Float) = 3.5
        _Frequency5("Frequency 5", Float) = 5.0

        // Vitesses locales
        _Speed1("Speed 1", Float) = 0.5
        _Speed2("Speed 2", Float) = 0.4
        _Speed3("Speed 3", Float) = 0.6
        _Speed4("Speed 4", Float) = 0.7
        _Speed5("Speed 5", Float) = 0.3

        // Phases initiales (décalage)
        _Phase1("Phase 1", Float) = 0.0
        _Phase2("Phase 2", Float) = 0.5
        _Phase3("Phase 3", Float) = 1.0
        _Phase4("Phase 4", Float) = 1.5
        _Phase5("Phase 5", Float) = 2.0

        // Offsets verticaux de base pour chaque "couche" de vague
        _VerticalOffset1("Vertical Offset 1", Float) = 0.2
        _VerticalOffset2("Vertical Offset 2", Float) = 0.4
        _VerticalOffset3("Vertical Offset 3", Float) = 0.6
        _VerticalOffset4("Vertical Offset 4", Float) = 0.8
        _VerticalOffset5("Vertical Offset 5", Float) = 1.0

        // Variables globales
        _GlobalAmplitude("Global Amplitude", Float) = 1.0
        _GlobalFrequency("Global Frequency", Float) = 1.0
        _GlobalSpeed("Global Speed", Float) = 1.0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 100

        Pass
        {
            // Add stencil buffer support
            Stencil
            {
                Ref 1
                Comp Equal
                Pass Keep
            }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv      : TEXCOORD0;
                float4 vertex  : SV_POSITION;
            };

            // Couleurs
            float4 _WaveColor1;
            float4 _WaveColor2;
            float4 _WaveColor3;
            float4 _WaveColor4;
            float4 _WaveColor5;

            // Amplitudes locales
            float _Amplitude1;
            float _Amplitude2;
            float _Amplitude3;
            float _Amplitude4;
            float _Amplitude5;

            // Fréquences locales
            float _Frequency1;
            float _Frequency2;
            float _Frequency3;
            float _Frequency4;
            float _Frequency5;

            // Vitesses locales
            float _Speed1;
            float _Speed2;
            float _Speed3;
            float _Speed4;
            float _Speed5;

            // Phases
            float _Phase1;
            float _Phase2;
            float _Phase3;
            float _Phase4;
            float _Phase5;

            // Offsets verticaux
            float _VerticalOffset1;
            float _VerticalOffset2;
            float _VerticalOffset3;
            float _VerticalOffset4;
            float _VerticalOffset5;

            // Variables globales
            float _GlobalAmplitude;
            float _GlobalFrequency;
            float _GlobalSpeed;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float time = _Time.y;

                // Calcul des positions des vagues en tenant compte des variables globales
                float wave1 = _VerticalOffset1
                              + (_Amplitude1 * _GlobalAmplitude) * sin((_Frequency1 * _GlobalFrequency) * (uv.x + (_Speed1 * _GlobalSpeed) * time) + _Phase1);
                float wave2 = _VerticalOffset2
                              + (_Amplitude2 * _GlobalAmplitude) * sin((_Frequency2 * _GlobalFrequency) * (uv.x + (_Speed2 * _GlobalSpeed) * time) + _Phase2);
                float wave3 = _VerticalOffset3
                              + (_Amplitude3 * _GlobalAmplitude) * sin((_Frequency3 * _GlobalFrequency) * (uv.x + (_Speed3 * _GlobalSpeed) * time) + _Phase3);
                float wave4 = _VerticalOffset4
                              + (_Amplitude4 * _GlobalAmplitude) * sin((_Frequency4 * _GlobalFrequency) * (uv.x + (_Speed4 * _GlobalSpeed) * time) + _Phase4);

                // Attribution de la couleur en fonction de la hauteur (uv.y) par rapport aux vagues
                float4 col;
                if (uv.y < wave1)
                    col = _WaveColor1;
                else if (uv.y < wave2)
                    col = _WaveColor2;
                else if (uv.y < wave3)
                    col = _WaveColor3;
                else if (uv.y < wave4)
                    col = _WaveColor4;
                else
                    col = _WaveColor5;

                return col;
            }
            ENDCG
        }
    }
    FallBack "Unlit/Texture"
}