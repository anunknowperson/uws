Shader "Hidden/FlowmapVisual"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float3 worldPos :TEXCOORD1;
                float4 vertex : SV_POSITION;
            };

            sampler2D _Flowmap;
			float2 _FlowmapPosition;
			float2 _FlowmapScale;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 flowMapUV =  (i.worldPos.xz - _FlowmapPosition) / _FlowmapScale - float2(0.5, 0.5);
				float2 flowmap = tex2Dlod(_Flowmap, float4(flowMapUV, 0.0, 0.0));


                
                return fixed4(flowmap, 0.0, 1.0);
            }
            ENDCG
        }
    }
}
