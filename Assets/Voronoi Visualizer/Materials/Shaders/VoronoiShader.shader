Shader "Voronoi Visualizer/Voronoi Shader" {
    SubShader {
        Cull Off
        Zwrite On

        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma editor_sync_compilation

            #pragma target 4.5

            #include "UnityCG.cginc"

            struct appdata {
                float4 vertex : POSITION;
            };

            struct v2f {
                float4 vertex : SV_POSITION;
                uint instanceID : SV_InstanceID;
            };


            StructuredBuffer<float4x4> _PositionMatrixBuffer;
            StructuredBuffer<float3> _ColorBuffer;

            v2f vert(appdata v, uint instanceID : SV_InstanceID) {
                v2f o;
                o.instanceID = instanceID;
                o.vertex = mul(_PositionMatrixBuffer[instanceID], v.vertex);
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
