Shader "Hidden/HighEndWater"
{

	SubShader
	{
		Tags { "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" }
		Cull Off
		
		LOD 100

		Blend SrcAlpha OneMinusSrcAlpha
		ZWrite On

		GrabPass {
			"_CameraTexture"
		}

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fog

			#pragma target 3.0

			#pragma multi_compile REFLECTION_NO REFLECTION_CUBEMAP REFLECTION_PLANAR
			#pragma multi_compile _ FLOW_FLOWMAP

			#include "UnityCG.cginc"
			#include "UnityLightingCommon.cginc"

			sampler2D _CameraTexture;
			sampler2D _ReflectionTexture;
			
			float4x4 _CameraProjection;
			float3 _AmbientLight;

			float3 _WaterColor;// = float3(0.6f, 0.87f, 0.9f);
			float3 _TurbidityColor;// = float3(0.3f, 0.4f, 0.5f);
			float _Turbidity;// = 0.9f;
			float _Transparency;// = 50.0f;
			float _RefractionStrength;// = 0.05f;
			
			float _DisplacementScale;
			float _FlowSpeedScale;


			sampler2D _DisplacementTexture;
			sampler2D _NormalTexture;

			sampler2D _Flowmap;
			float2 _FlowmapPosition;
			float2 _FlowmapScale;

			#include "WaterUtilities.cginc"
			#include "WaterDecals.cginc"
			
			
			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv :TEXCOORD0;
			};
			
		
			struct v2f
			{

				float4 vertex : POSITION;
				
				float3 worldPos :TEXCOORD0;
				float4 screenPos :TEXCOORD1;

				float2 uv :TEXCOORD2;
				float4 uvGrab : TEXCOORD3;

			};

			v2f vert(appdata v)
			{
				v2f o;

				o.worldPos = mul(unity_ObjectToWorld, v.vertex);

				o.uv = (o.worldPos.xz / 20.0);
				
				float3 disp = tex2Dlod(_DisplacementTexture, float4(o.uv / _DisplacementScale , 0.0, 0.0)) / 10.0 * _DisplacementScale;

				#if FLOW_FLOWMAP
				float2 flowMapUV =  (o.worldPos.xz - _FlowmapPosition) / _FlowmapScale - float2(0.5, 0.5);
				float2 flowmap = tex2Dlod(_Flowmap, float4(flowMapUV, 0.0, 0.0)) * 2 - 1;
				disp = ComputeDisplaceUsingFlowMap(flowmap, disp, o.uv / _DisplacementScale , _Time.y * _FlowSpeedScale);
				#endif

				
				float h = 0.0;
				float t = sampleDecalsHeigth(o.worldPos, h);

				if (t > 0.7){
					disp = float3(0.0, 0.0, 0.0);
					t = 1.0;
				}

				float3 finalPos = v.vertex + disp;

				finalPos.y = lerp(finalPos.y, h, t);


				o.vertex = UnityObjectToClipPos(finalPos);

				o.screenPos = ComputeScreenPos(o.vertex);
				o.uvGrab = ComputeGrabScreenPos(o.vertex);
		
				return o;
			}

			

			fixed4 frag(v2f i, half facing : VFACE) : SV_Target
			{
                float2 screenUV = i.screenPos.xy / i.screenPos.w;

				float3 viewDir = GetWorldSpaceViewDirNorm(i.worldPos);
				float viewDist = GetWorldToCameraDistance(i.worldPos);

				float fogFactor = 0.0;

				float sceneZ = GetSceneDepth(screenUV);
				half surfaceTensionFade = GetSurfaceTension(sceneZ, i.screenPos.w);

				// FILTERING
				float normalFilteringMask;
				float3 normal = GetFilteredNormal_lod0(i.uv / _DisplacementScale, viewDist, normalFilteringMask, _NormalTexture);

				normal = normalize(normal);

				#if FLOW_FLOWMAP 
				normal = GetFlowmapNormal(i.worldPos, i.uv / _DisplacementScale, normal);
				#endif


				normal.xz *= normalFilteringMask;


				if (viewDist > 750){
					normal = float3(0.0, 1.0, 0.0);
				}

				// END FILTERING

				// REFRACTION

				float2 refractionUV;
				refractionUV = GetRefractedUV_Simple(screenUV, normal, _RefractionStrength);
				half3 refraction = GetSceneColor(refractionUV);


				
				// END REFRACTION

				// UNDERWATER
				half4 volumeScattering = half4(GetAmbientColor(), 1.0);

				float depthAngleFix;
				float fade = GetWaterRawFade(viewDir, refractionUV, i.screenPos.w, depthAngleFix);
				FixAboveWaterRendering(depthAngleFix, screenUV, i.screenPos.w, sceneZ, fade, refraction, volumeScattering);

				half3 underwaterColor = ComputeUnderwaterColor(refraction, volumeScattering.rgb,  fade, _Transparency, _WaterColor, _Turbidity, _TurbidityColor);
				//underwaterColor += ComputeSSS(screenUV, underwaterColor, 1.0, 5.0, i.normal);

				
				
				// END UNDERWATER
				
				// REFLECTION

				half3 planarReflection = 0;
				half3 skyReflection = 0;
				half4 ssrReflection = 0;
				half3 sunReflection = 0;

				

				sunReflection = ComputeSunlight(normal, viewDir, _WorldSpaceLightPos0.xyz, _LightColor0.xyz, 1, viewDist, 1000.0, _Transparency);

				half3 finalReflection = 0;
				
				#if REFLECTION_NO
				finalReflection = half3(0.4, 0.4, 0.4) + sunReflection;
				#endif

				#if REFLECTION_CUBEMAP
				float3 reflectedDir = reflect(-viewDir, normalize(lerp(normal, float3(0.0, 1.0, 0.0), 0.8)));
                float4 c = UNITY_SAMPLE_TEXCUBE_LOD(unity_SpecCube0, reflectedDir, 0);
				half3 skyColor = DecodeHDR (c, unity_SpecCube0_HDR); 

				finalReflection = skyColor + sunReflection;
				#endif

				#if REFLECTION_PLANAR
				float2 refl_uv = GetScreenSpaceReflectionUV(lerp(normal, float3(0.0, 1.0, 0.0), 0.8), viewDir, _CameraProjection);

				planarReflection = ComputeReflection(refl_uv);
				finalReflection = planarReflection + sunReflection;
				#endif

				// END REFLECTION

				half waterFresnel = ComputeWaterFresnel(normal, viewDir);

				half3 finalColor = lerp(underwaterColor, finalReflection, waterFresnel);

				// if (dot(normal, viewDir) > 0){

				half3 colorWithDecals = sampleDecals(fixed4(finalColor, 1.0), i.worldPos);

				if (facing > 0){
					return float4(colorWithDecals, 1.0);
				} else {
					float z = GetSceneDepth(screenUV);
					float linearZ = viewDist;
					float uFade = max(0, linearZ) * 0.25;
					half3 finalUnderwaterColor = ComputeUnderwaterColor(colorWithDecals, volumeScattering.rgb,  uFade, _Transparency, _WaterColor, _Turbidity, _TurbidityColor);

					return float4(finalUnderwaterColor, 1.0);
				}

				
			}

			ENDCG
		}
	}
	//Fallback "Diffuse"
}
