Shader "Centroid Visualizer/Voronoi Shader" {
    SubShader {
        Cull OFF

        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata {
                float4 vertex : POSITION;
                float color: COLOR;
            };

            struct v2f {
                float4 vertex : SV_POSITION;
                float color : COLOR;
            };

            uniform StructuredBuffer<float2> _ColorBuffer;
            uniform uint _NumRegions;

            v2f vert(appdata v) {
                v2f o;
                o.vertex = mul(UNITY_MATRIX_VP, v.vertex);
                o.color = v.color;
                return o;
            }

            float4 frag(v2f i) : SV_Target {
                int index = min(floor(i.color * _NumRegions), _NumRegions - 1);
                return float4(i.color, _ColorBuffer[index].xy, 1);
            }

            ENDCG
        }
    }
}

