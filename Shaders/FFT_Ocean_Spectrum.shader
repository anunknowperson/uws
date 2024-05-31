Shader "Hidden/FFT_Ocean_Spectrum"
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
			#define G 9.81
			#define KM 370.0

			float size;
			float resolution;
			float choppiness;
			sampler2D phases;
			sampler2D initialSpectrum;

			float2 multiplyComplex (float2 a, float2 b) {
				return float2(a[0] * b[0] - a[1] * b[1], a[1] * b[0] + a[0] * b[1]);
			}

			float2 multiplyByI (float2 z) {
				return float2(-z[1], z[0]);
			}

			float omega (float k) {
				return sqrt(G * k * (1.0 + k * k / KM * KM));
			}

            float4 frag(v2f_img i) : COLOR
            {
			    float2 coordinates = i.uv.xy * resolution - 0.5;
				float n = (coordinates.x < resolution * 0.5) ? coordinates.x : coordinates.x - resolution;
				float m = (coordinates.y < resolution * 0.5) ? coordinates.y : coordinates.y - resolution;
				float2 waveVector = (2.0 * PI * float2(n, m)) / size;

				float phase = tex2D(phases, i.uv).r;
				float2 phaseVector = float2(cos(phase), sin(phase));

				float2 h0 = tex2D(initialSpectrum, i.uv).rg;
				float2 h0Star = tex2D(initialSpectrum, float2(1.0 - i.uv + 1.0 / resolution)).rg;
				h0Star.y *= -1.0;

				float2 h = multiplyComplex(h0, phaseVector) + multiplyComplex(h0Star, float2(phaseVector.x, -phaseVector.y));

				float2 hX = -multiplyByI(h * (waveVector.x / length(waveVector))) * choppiness;
				float2 hZ = -multiplyByI(h * (waveVector.y / length(waveVector))) * choppiness;

				//no DC term
				if (waveVector.x == 0.0 && waveVector.y == 0.0) {
					h = float2(0.0, 0.0);
					hX = float2(0.0, 0.0);
					hZ = float2(0.0, 0.0);
				}

				return float4(hX + multiplyByI(h), hZ);
            }
            ENDCG
        }
    }

    Fallback off
}
