Shader "Custom/DustMaskPaint"
{
    Properties
    {
        _MainTex ("Dust Mask", 2D) = "black" {}
        _BrushTex ("Brush", 2D) = "white" {}

        _BrushPosition ("Brush Position", Vector) = (0.5, 0.5, 0, 0)
        _BrushSize ("Brush Size", Vector) = (0.1, 0.1, 0, 0)
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
        }

        Pass
        {
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            sampler2D _BrushTex;

            float4 _BrushPosition;
            float4 _BrushSize;

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

            float4 frag(v2f i) : SV_Target
            {
                float4 currentMask = tex2D(
                    _MainTex,
                    i.uv
                );

                // Position relative to the brush.
                float2 brushUV =
                    (i.uv - _BrushPosition.xy)
                    / _BrushSize.xy
                    + 0.5;

                // Outside brush area.
                if (brushUV.x < 0.0 ||
                    brushUV.x > 1.0 ||
                    brushUV.y < 0.0 ||
                    brushUV.y > 1.0)
                {
                    return currentMask;
                }

                float4 brush = tex2D(
                    _BrushTex,
                    brushUV
                );

                float newDust =
                    currentMask.g * brush.g;

                return float4(
                    0.0,
                    newDust,
                    0.0,
                    1.0
                );
            }

            ENDHLSL
        }
    }
}