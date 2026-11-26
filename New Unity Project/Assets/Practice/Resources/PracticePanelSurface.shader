Shader "FIRST180/Panel surface" {
    Properties { _Color ("Color", Color) = (1,1,1,1) }
    SubShader {
        Tags { "RenderType"="Opaque" }
        Pass {
            Cull Off
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"
            struct Input { float4 vertex : POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varying { float4 vertex : SV_POSITION; UNITY_VERTEX_OUTPUT_STEREO };
            fixed4 _Color;
            Varying vert(Input v) {
                Varying o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.vertex=UnityObjectToClipPos(v.vertex);
                return o;
            }
            fixed4 frag(Varying i) : SV_Target { return _Color; }
            ENDCG
        }
    }
}
