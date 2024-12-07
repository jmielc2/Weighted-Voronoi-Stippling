Shader "Centroid Visualizer/Voronoi Shader" {
    SubShader {
        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #define UNITY_INDIRECT_DRAW_ARGS IndirectDrawIndexedArgs
            #include "UnityIndirect.cginc"

            struct appdata {
                float4 vertex : POSITION;
                uint instanceID : SV_InstanceID;
            };

            struct v2f {
                float4 vertex : SV_POSITION;
                uint instanceID : SV_InstanceID;
            };


            StructuredBuffer<float4x4> _PositionMatrixBuffer;
            StructuredBuffer<float3> _ColorBuffer;

            v2f vert(appdata v) {
                InitIndirectDrawArgs(0);
                uint id = GetIndirectInstanceID(v.instanceID);
                v2f o;
                o.instanceID = id;
                o.vertex = mul(_PositionMatrixBuffer[id], v.vertex);
                o.vertex = UnityObjectToClipPos(o.vertex);
                return o;
            }

            float4 frag(v2f i) : SV_Target {
                return float4(_ColorBuffer[i.instanceID], 1);
            }

            ENDCG
        }
    }
}

