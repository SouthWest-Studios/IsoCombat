Shader "Custom/GlitchCircleTransition"
{
    Properties
    {
        _Color ("Color", Color) = (0,0,0,1)  // Negro por defecto
        _Radius ("Circle Radius", Range(0.0, 1.0)) = 0
        _CenterX ("Center X", Range(0.0, 1.0)) = 0.5
        _CenterY ("Center Y", Range(0.0, 1.0)) = 0.5
        _GlitchIntensity ("Glitch Intensity", Range(0.0, 1.0)) = 0.1
        _ScanLines ("Scan Lines Density", Float) = 100.0
        _NoiseScale ("Noise Scale", Float) = 10.0
    }
    SubShader
    {
        Tags
        {
            "RenderType"="Transparent" "Queue" = "Transparent"
        }
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _Color;
            float _Radius;
            float _CenterX;
            float _CenterY;
            float _GlitchIntensity;
            float _ScanLines;
            float _NoiseScale;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            float rand(float2 co)
            {
                return frac(sin(dot(co.xy, float2(12.9898, 78.233))) * 43758.5453);
            }

            float noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                
                float a = rand(i);
                float b = rand(i + float2(1.0, 0.0));
                float c = rand(i + float2(0.0, 1.0));
                float d = rand(i + float2(1.0, 1.0));
                
                float2 u = f * f * (3.0 - 2.0 * f);
                
                return lerp(a, b, u.x) +
                        (c - a) * u.y * (1.0 - u.x) +
                        (d - b) * u.x * u.y;
            }

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float drawGlitchCircle(float2 uv, float2 center, float radius)
            {
                float dist = distance(uv, center);
                
                // Efectos glitch
                float glitchOffset = _GlitchIntensity * noise(uv * _NoiseScale) * 0.05;
                float scanLines = sin(uv.y * _ScanLines + _Time.y * 10.0) * 0.02 * _GlitchIntensity;
                float horizontalGlitch = step(0.99, rand(float2(_Time.y, uv.y))) * _GlitchIntensity * 0.1;
                
                float glitchRadius = radius + glitchOffset + scanLines + horizontalGlitch;
                
                // Bordes irregulares
                float angle = atan2(uv.y - center.y, uv.x - center.x);
                float jagged = sin(angle * 8.0 + _Time.y * 5.0) * 0.01 * _GlitchIntensity;
                glitchRadius += jagged;
                
                // Invertimos la lógica aquí para que el círculo sea transparente
                return smoothstep(glitchRadius, glitchRadius + 0.02, dist);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 center = float2(_CenterX, _CenterY);
                
                // Efecto RGB split
                float2 offset = float2(noise(i.uv * 50.0) * 0.005, noise(i.uv * 50.0 + 10.0) * 0.005) * _GlitchIntensity;
                float r = drawGlitchCircle(i.uv + offset, center, _Radius);
                float g = drawGlitchCircle(i.uv, center, _Radius);
                float b = drawGlitchCircle(i.uv - offset, center, _Radius);
                
                // Efecto scanlines
                float scanLines = saturate(sin(i.uv.y * _ScanLines) * 0.1 + 0.9);
                
                fixed4 col = _Color;
                col.rgb = fixed3(r, g, b) * _Color.rgb * scanLines;
                // Usamos el canal verde para el alpha pero invertido
                col.a = g; // Ahora las áreas dentro del círculo son transparentes
                
                return col;
            }
            ENDCG
        }
    }
}