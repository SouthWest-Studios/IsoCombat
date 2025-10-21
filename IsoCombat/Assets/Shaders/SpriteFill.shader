Shader "Custom/SpriteFillWithWave"
{
    Properties
    {
        [PerRendererData]_MainTex("Sprite Texture", 2D) = "white" {}
        [HDR]_Color("Tint", Color) = (1,1,1,1)
        _FillAmount("Fill Amount", Range(0,1)) = 1
        [Enum(LeftToRight, 0, RightToLeft, 1, BottomToTop, 2, TopToBottom, 3)]
        _FillDirection("Fill Direction", Float) = 0

        _WaveAmplitude("Wave Amplitude", Range(0, 0.2)) = 0.02
        _WaveFrequency("Wave Frequency", Float) = 30
        _WaveSpeed("Wave Speed", Float) = 2

        _GlowIntensity("Glow Intensity", Float) = 1.0
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType"="Transparent" }
        LOD 100

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        Lighting Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;
            float _FillAmount;
            float _FillDirection;

            float _WaveAmplitude;
            float _WaveFrequency;
            float _WaveSpeed;

            float _GlowIntensity;

            float _TimeY;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;

                float time = _Time.y * _WaveSpeed;
                float waveOffset = 0;

                // Apply directional wave at the edge
                if (_FillDirection == 0 || _FillDirection == 1) // Horizontal fill
                {
                    waveOffset = sin(uv.y * _WaveFrequency + time) * _WaveAmplitude;

                    if (_FillDirection == 0 && uv.x > _FillAmount + waveOffset) discard;
                    else if (_FillDirection == 1 && uv.x < 1.0 - _FillAmount - waveOffset) discard;
                }
                else // Vertical fill
                {
                    waveOffset = sin(uv.x * _WaveFrequency + time) * _WaveAmplitude;

                    if (_FillDirection == 2 && uv.y > _FillAmount + waveOffset) discard;
                    else if (_FillDirection == 3 && uv.y < 1.0 - _FillAmount - waveOffset) discard;
                }

                fixed4 col = tex2D(_MainTex, uv) * _Color * _GlowIntensity;
                return col;
            }
            ENDCG
        }
    }
}
