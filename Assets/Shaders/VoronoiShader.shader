Shader "Custom/Voronoi Shader" {
    SubShader {
        Tags { "RenderType"="Opaque" }

        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma instancing_options assumeuniformscaling

            #include "UnityCG.cginc"

            struct appdata {
                float4 vertex : POSITION;
                #if defined(INSTANCING_ON)
                uint instanceID : SV_InstanceID;
                #endif
            };

            struct v2f {
                float4 vertex : SV_POSITION;
                #if defined(INSTANCING_ON)
                uint instanceID : SV_InstanceID;
                #endif
            };


            StructuredBuffer<float4x4> _PositionMatrixBuffer;
            StructuredBuffer<float3> _ColorBuffer;

            v2f vert(appdata v) {
                v2f o;
                float4 pos = v.vertex;
                #if defined(INSTANCING_ON)
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                pos = mul(_PositionMatrixBuffer[v.instanceID], pos);
                #endif
                // o.vertex = UnityObjectToClipPos(v.vertex);
                
                o.vertex = UnityObjectToClipPos(pos);
                return o;
            }

            float4 frag(v2f i) : SV_Target {
                #if defined(INSTANCING_ON)
                return float4(_ColorBuffer[i.instanceID], 1);
                #else
                return float4(0.2, 0.2, 0.2, 1);
                #endif
            }
            ENDCG
        }
    }
}
