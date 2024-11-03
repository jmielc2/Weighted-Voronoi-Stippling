Shader "Unlit/Test Indirect Shader" {
    SubShader {
        Tags { "RenderType"="Opaque" }
        LOD 100

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
            StructuredBuffer<float4> _ColorBuffer;

            v2f vert (appdata v) {
                InitIndirectDrawArgs(0);
                v2f o;
                o.vertex = mul(_PositionMatrixBuffer[v.instanceID], v.vertex);
                o.vertex = UnityObjectToClipPos(o.vertex);
                o.instanceID = v.instanceID;
                return o;
            }

            float4 frag (v2f i) : SV_Target {
                return _ColorBuffer[i.instanceID];
            }
            ENDCG
        }
    }
}
