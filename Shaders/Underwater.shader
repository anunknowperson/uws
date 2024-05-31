Shader "Hidden/Underwater"
{
    SubShader
	{
		Tags { "Queue" = "Transparent-1" "IgnoreProjector" = "True" "RenderType" = "Transparent" }
		Cull Off
		ZTest Always
		ZWrite Off

		LOD 100

		Blend SrcAlpha OneMinusSrcAlpha
		

		GrabPass {
			"_CameraTexture"
		}

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

			sampler2D _CameraTexture;
			sampler2D _ReflectionTexture;
			sampler2D _Flowmap;

			sampler2D _DisplacementTexture;
			sampler2D _NormalTexture;
			float _DisplacementScale;
			float3 _FlowmapPosition;
			float _FlowmapScale;
			float _FlowSpeedScale;

			float _Transparency;
			float3 _WaterColor;
			float _Turbidity;
			float3 _TurbidityColor;

            half3 _AmbientLight;

            #include "WaterUtilities.cginc"

            struct appdata
			{
				float4 vertex : POSITION;
			};
			
		
			struct v2f
			{

				float4 vertex : POSITION;
				
				float3 worldPos :TEXCOORD0;
				float4 screenPos :TEXCOORD1;
				float4 uvGrab : TEXCOORD4;
			};

            v2f vert(appdata v)
			{
				v2f o;

				o.worldPos = mul(unity_ObjectToWorld, v.vertex);
				

				o.vertex = UnityObjectToClipPos(v.vertex );
				o.screenPos = ComputeScreenPos(o.vertex);
				o.uvGrab = ComputeGrabScreenPos(o.vertex);
		
				return o;
			}

            fixed4 frag (v2f i) : SV_Target
            {
				//return float4(_WaterColor, 1.0);
                float2 screenUV = i.screenPos.xy / i.screenPos.w;


				//float3 viewDir = GetWorldSpaceViewDirNorm(i.worldPos);
				//float viewDist = GetWorldToCameraDistance(i.worldPos);

				//float fogFactor = 0.0;

				float sceneZ = GetSceneDepth(screenUV);
				half surfaceTensionFade = GetSurfaceTension(sceneZ, i.screenPos.w);


				//half3 refraction = GetSceneColor(screenUV);

				// END REFRACTION

				// UNDERWATER
				half4 volumeScattering = half4(GetAmbientColor(), 1.0);

				//float waterDepth = GetWaterDepth(screenUV);
				float z = GetSceneDepth(screenUV);
				float linearZ = LinearEyeDepth(z);
				//float depthSurface = LinearEyeDepth(waterDepth);
				//half waterSurfaceMask = saturate((depthSurface - linearZ));

				//half2 normals = GetWaterMaskScatterNormals(screenUV.xy).zw * 2 - 1;
				half3 refraction = GetSceneColor(screenUV.xy)	;

				

				//half3 waterColorBellow = GetSceneColor(screenUV.xy + normals);
				//half3 refraction = lerp(waterColorBellow, waterColorUnder, waterSurfaceMask);

				//float depthAngleFix;
				float fade = max(0, linearZ) * 0.25;
				//FixAboveWaterRendering(depthAngleFix, screenUV, i.screenPos.w, sceneZ, fade, refraction, volumeScattering);

				half3 underwaterColor = ComputeUnderwaterColor(refraction, volumeScattering.rgb,  fade, _Transparency, _WaterColor, _Turbidity, _TurbidityColor);
				//underwaterColor += ComputeSSS(screenUV, underwaterColor, 1.0, 5.0, i.normal);

				return half4(underwaterColor, 1.0);
            }
            ENDCG
        }
    }
}
