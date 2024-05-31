Shader "Hidden/FFT_Ocean_Phase"
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

			sampler2D phases;
			float deltaTime;
			float resolution;
			float size;

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
			    float deltaPhase = omega(length(waveVector)) * deltaTime;
			    phase = fmod(phase + deltaPhase, 2.0 * PI);
	
				return float4(phase, 0.0, 0.0, 1.0);
            }
            ENDCG
        }
    }

    Fallback off
}
