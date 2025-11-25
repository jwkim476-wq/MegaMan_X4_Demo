Shader "Unlit/ColorKeyAndDarkGlass"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}

        
        _GlassAlpha ("Glass Alpha", Range(0,1)) = 0.55

        _BlueDominance ("Blue Dominance", Range(0,1)) = 0.08

     
        _MinSaturation ("Min Saturation", Range(0,1)) = 0.10

      
        _MinLuma ("Min Luma To Affect", Range(0,1)) = 0.05
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
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

            sampler2D _MainTex;

            float _GlassAlpha;
            float _BlueDominance;
            float _MinSaturation;
            float _MinLuma;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);

                // ----------------------------
                // 1) COLOR KEY — pure black background 제거
                // ----------------------------
                if (col.r == 0 && col.g == 0 && col.b == 0)
                {
                    col.a = 0;
                    return col;
                }

                // ----------------------------
                // 2) DARK GLASS DETECTION
                //    "파랑/청록 계열 우세 + 약간의 채도"면 유리로 간주
                // ----------------------------

                // 휘도(밝기)
                float luma = dot(col.rgb, float3(0.299, 0.587, 0.114));
                if (luma < _MinLuma) return col;

                // 채도 근사: max-min
                float maxC = max(col.r, max(col.g, col.b));
                float minC = min(col.r, min(col.g, col.b));
                float sat = maxC - minC;

                if (sat < _MinSaturation) return col;

                // 파랑 우세 판정
                bool blueDominant = (col.b > col.r + _BlueDominance) && (col.b > col.g + _BlueDominance);

                if (blueDominant)
                {
                    col.a *= _GlassAlpha;
                }

                return col;
            }
            ENDCG
        }
    }
}