Shader "Centroid Visualizer/Voronoi Shader" {
    SubShader {
        Cull OFF

        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata {
                float3 vertex : POSITION;
                float3 color: COLOR;
            };

            struct v2f {
                float4 vertex : SV_POSITION;
                float3 color : COLOR;
            };

            StructuredBuffer<float4x4> _PositionMatrixBuffer;
            StructuredBuffer<float2> _ColorBuffer;
            uint _NumRegions;

            v2f vert(appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(float4(v.vertex, 1));
                o.color = v.color;
                return o;
            }

            float4 frag(v2f i) : SV_Target {
                return float4(i.color, 1);
            }

            ENDCG
        }
    }
}

