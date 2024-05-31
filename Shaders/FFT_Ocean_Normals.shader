Shader "Hidden/FFT_Ocean_Normals"
{
    SubShader
    {
        Pass
        {
            ZTest Always Cull Off ZWrite Off
            Fog { Mode off }
     
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #pragma fragmentoption ARB_precision_hint_fastest
            #pragma enable_d3d11_debug_symbols
            #include "UnityCG.cginc"

            sampler2D displacementMap;
		    float resolution;
		    float size;

            float4 frag(v2f_img i) : COLOR
            {
			    float texel = 1.0 / resolution;
		    	float texelSize = size / resolution;

		    	float3 center = tex2D(displacementMap, i.uv).rgb;
		    	float3 right = float3(texelSize, 0.0, 0.0) + tex2D(displacementMap, i.uv + float2(texel, 0.0)).rgb - center;
		    	float3 left = float3(-texelSize, 0.0, 0.0) + tex2D(displacementMap, i.uv + float2(-texel, 0.0)).rgb - center;
		    	float3 top = float3(0.0, 0.0, -texelSize) + tex2D(displacementMap, i.uv + float2(0.0, -texel)).rgb - center;
		    	float3 bottom = float3(0.0, 0.0, texelSize) + tex2D(displacementMap, i.uv + float2(0.0, texel)).rgb - center;

		    	float3 topRight = cross(right, top);
		    	float3 topLeft = cross(top, left);
		    	float3 bottomLeft = cross(left, bottom);
		    	float3 bottomRight = cross(bottom, right);

				return float4(normalize(topRight + topLeft + bottomLeft + bottomRight), 1.0);
            }
            ENDCG
        }
    }

    Fallback off
}
