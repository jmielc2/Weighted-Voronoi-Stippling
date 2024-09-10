Shader "Unlit/Point Shader" {
    Properties {
        _Color ("Color", Color) = (0, 0, 0, 0)
    }

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
            };

            StructuredBuffer<float4x4> _PositionsMatrixBuffer;
            float4 _Color;

            v2f vert (appdata v) {
                v2f o;
                float4 pos = v.vertex;
                #if defined(INSTANCING_ON)
                UNITY_SETUP_INSTANCE_ID(v);
                pos = mul(_PositionsMatrixBuffer[v.instanceID], pos);
                #endif
                // o.vertex = UnityObjectToClipPos(v.vertex);

                o.vertex = UnityObjectToClipPos(pos);
                return o;
            }

            float4 frag (v2f i) : SV_Target {
                return _Color;
            }
            ENDCG
        }
    }
}
