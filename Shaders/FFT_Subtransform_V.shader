Shader "Hidden/FFT_Subtransform_V"
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

            #define PI 3.14159265359

            sampler2D input;
            
            float transformSize = 512.0;
            float subtransformSize = 250.0;

            float2 multiplyComplex (float2 a, float2 b) {
			    return float2(a[0] * b[0] - a[1] * b[1], a[1] * b[0] + a[0] * b[1]);
		    }

            float4 frag(v2f_img i) : COLOR
            {
			    float index = i.uv.y * transformSize - 0.5;

			    float evenIndex = floor(index / subtransformSize) * (subtransformSize * 0.5) + fmod(index, subtransformSize * 0.5);
                
                float4 even = tex2D(input, float2(i.uv.x * transformSize, evenIndex + 0.5) / transformSize).rgba;
				float4 odd = tex2D(input, float2(i.uv.x * transformSize, evenIndex + transformSize * 0.5 + 0.5) / transformSize).rgba;


			    float twiddleArgument = -2.0 * PI * (index / subtransformSize);
			    float2 twiddle = float2(cos(twiddleArgument), sin(twiddleArgument));

			    float2 outputA = even.xy + multiplyComplex(twiddle, odd.xy);
			    float2 outputB = even.zw + multiplyComplex(twiddle, odd.zw);

                return float4(outputA, outputB);
            }
            ENDCG
        }
    }

    Fallback off
}
