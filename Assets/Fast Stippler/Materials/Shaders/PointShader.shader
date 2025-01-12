Shader "Fast Stippler/Point Shader" {
    Properties {
        _Color ("Color", Color) = (0, 0, 0, 1)
        _Scale ("Scale", float) = 0.5
    }

    SubShader {
        Zwrite Off
        Cull Off

        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma target 4.5

            #include "UnityCG.cginc"

            struct appdata {
                float4 vertex : POSITION;
            };

            struct v2f {
                float4 vertex : SV_POSITION;
            };

            StructuredBuffer<float4x4> _PositionMatrixBuffer;
            float4 _Color;
            float _Scale;

            v2f vert (appdata v, uint instanceID : SV_InstanceID) {
                v2f o;
                float4x4 transform = _PositionMatrixBuffer[instanceID];
                transform._m00_m11_m22 = _Scale;
                o.vertex = mul(transform, v.vertex);
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
