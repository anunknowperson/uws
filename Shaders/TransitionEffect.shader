Shader "Hidden/TransitionEffect"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            sampler2D _MainTex;

            float4 depth_blend2(float4 a, float4 b, float t) {
            	// https://www.gamasutra.com
            	// /blogs/AndreyMishkinis/20130716/196339/Advanced_Terrain_Texture_Splatting.php
            	float d = 0.1;
            	float ma = max(a.a + (1.0 - t), b.a + t) - d;
            	float ba = max(a.a + (1.0 - t) - ma, 0.0);
            	float bb = max(b.a + t - ma, 0.0);
            	return (a * ba + b * bb) / (ba + bb);
            }

            float4 texture_antitile(sampler2D tex, float2 uv) {
            	float frequency = 2.0;
            	float scale = 1.3;
            	float sharpness = 0.7;
            	
            	// Rotate and scale UV
            	float rot = 3.14 * 0.6;
            	float cosa = cos(rot);
            	float sina = sin(rot);
            	float2 uv2 = float2(cosa * uv.x - sina * uv.y, sina * uv.x + cosa * uv.y) * scale;
            	
            	float4 col0 = tex2D(tex, uv);
            	float4 col1 = tex2D(tex, uv2);
            	//col0 = vec4(0.0, 0.0, 1.0, 1.0);
            	// Periodically alternate between the two versions using a warped checker pattern
            	float t = 0.5 + 0.5 
            		* sin(uv2.x * frequency + sin(uv.x) * 2.0) 
            		* cos(uv2.y * frequency + sin(uv.y) * 2.0);
            	// Using depth blend because classic alpha blending smoothes out details
            	return depth_blend2(col0, col1, smoothstep(0.5 * sharpness, 1.0 - 0.5 * sharpness, t));
            }

            fixed4 frag (v2f i) : SV_Target
            {
                
                float q1 = 0.5 - abs(0.5 - i.uv.x);
                float q2 = 0.5 -  distance(i.uv, float2(0.5, 0.5));

                float4 foam = tex2D(_MainTex, i.uv * 2.0 + float2(_Time.y / 25.0, 0.0));
                float4 foam2 = tex2D(_MainTex, i.uv * 10.0 + float2(_Time.y / 25.0, 0.0));
                float4 foam3 = tex2D(_MainTex, i.uv * 20.0 + float2(_Time.y / 25.0, 0.0));

                foam = texture_antitile(_MainTex, i.uv * 10.0 + float2(_Time.y / 25.0, 0.0));

                float4 finalfoam = foam ;

                return float4(1.0, 1.0, 1.0, (finalfoam.r + finalfoam.g + finalfoam.b)/3.0 * q1 * q2 * 2.0);
            }
            ENDCG
        }
    }
}
