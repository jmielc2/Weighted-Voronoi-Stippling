Shader "Fast Stippler/Point Shader" {
    Properties {
        _Color ("Color", Vector) = (0, 0, 0, 1)
    }

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
            };

            StructuredBuffer<float4x4> _PositionMatrixBuffer;
            float4 _Color;

            v2f vert (appdata v) {
                InitIndirectDrawArgs(0);
                v2f o;
                o.vertex = mul(_PositionMatrixBuffer[v.instanceID], v.vertex);
                o.vertex = UnityObjectToClipPos(o.vertex);
                return o;
            }

            float4 frag (v2f i) : SV_Target {
                return _Color;
            }
            ENDCG
        }
    }
}
