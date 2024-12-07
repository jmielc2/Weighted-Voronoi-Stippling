Shader "Centroid Visualizer/Voronoi Shader" {
    SubShader {
        Cull OFF

        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma instancing_options assumeuniformscaling nolightmap
            #pragma editor_sync_compilation

            #pragma target 4.5

            #include "UnityCG.cginc"

            struct appdata {
                float3 vertex : POSITION;
            };

            struct v2f {
                float4 vertex : SV_POSITION;
                uint instanceID : SV_InstanceID;
            };

            uniform StructuredBuffer<float3> _ColorBuffer;
            uniform StructuredBuffer<float4x4> _PositionMatrixBuffer;

            v2f vert(appdata v, uint instanceID : SV_InstanceID) {
                v2f o;
                float4 pos = mul(_PositionMatrixBuffer[instanceID], float4(v.vertex, 1));
                o.vertex = mul(UNITY_MATRIX_VP, pos);
                o.instanceID = instanceID;
                return o;
            }

            float4 frag(v2f i) : SV_Target {
                return float4(_ColorBuffer[i.instanceID], 1);
            }
            ENDCG
        }
    }
}

