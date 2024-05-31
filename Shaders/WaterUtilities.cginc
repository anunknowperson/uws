sampler2D KW_WaterMaskScatterNormals;
sampler2D KW_WaterMaskScatterNormals_Blured;
sampler2D KW_WaterDepth;


inline half4 GetWaterMaskScatterNormals(float2 uv)
{
	return tex2D(KW_WaterMaskScatterNormals, uv);
}

inline half4 GetWaterMaskScatterNormalsBlured(float2 uv)
{
	return tex2D(KW_WaterMaskScatterNormals_Blured, uv);
}

inline float GetWaterDepth(float2 uv)
{
	return tex2D(KW_WaterDepth, uv).x;
}

inline float3 GetWorldSpaceViewDirNorm(float3 worldPos)
{
	return normalize(_WorldSpaceCameraPos.xyz - worldPos.xyz);
}

inline float GetWorldToCameraDistance(float3 worldPos)
{
	return length(_WorldSpaceCameraPos.xyz - worldPos.xyz);
}

Texture2D _CameraDepthTexture;
SamplerState sampler_CameraDepthTexture;

inline float GetSceneDepth(float2 uv)
{
	return _CameraDepthTexture.SampleLevel(sampler_CameraDepthTexture, uv, 0);
}

inline half GetSurfaceTension(float z, float screenPosW)
{
	return saturate((LinearEyeDepth(z) - screenPosW) * 8);
}

inline float2 GetRefractedUV_Simple(float2 uv, half3 normal, float refractionStrength)
{
	return uv + normal.xz * refractionStrength * 0.5;
}

inline float3 GetSceneColor(float2 uv)
{
	return tex2D(_CameraTexture, uv).xyz;
}

inline half3 GetAmbientColor()
{
    return _AmbientLight;
}

inline half GetWaterRawFade(float3 viewDir, float2 refractionUV, float screenPosW, out half depthAngleFix)
{
	depthAngleFix = saturate(dot(viewDir, float3(0, 1, 0)));
	float sceneZ_refracted = LinearEyeDepth(GetSceneDepth(refractionUV));
	return (sceneZ_refracted - screenPosW) * depthAngleFix;
	
}

inline void FixAboveWaterRendering(float depthAngleFix, float2 screenUV, float screenPosW, float sceneZ, inout float fade, inout half3 refraction, inout half4 volumeScattering)
{
	UNITY_BRANCH if (fade < 0)
	{
		fade = (LinearEyeDepth(sceneZ) - screenPosW) * depthAngleFix;
		refraction = GetSceneColor(screenUV);
		//volumeScattering = half4(float3(0.0, 0.0, 0.0), 1.0f);
	}
}


half3 ComputeUnderwaterColor(half3 refraction, half3 volumeLight, half fade, half transparent, half3 waterColor, half turbidity, half3 turbidityColor)
{
	float fadeExp = saturate(1 - exp(-5 * fade / transparent));

	half3 absorbedColor = pow(clamp(waterColor.xyz, 0.1, 0.95), 25 * fade / transparent) ; //min range ~ 0.0  with pow(x, 70)
	absorbedColor = lerp(pow(waterColor.xyz, 15.0) * 0.05 * volumeLight.rgb, refraction, absorbedColor);
	
	//volumeLight.rgb = lerp(refraction, volumeLight, saturate(1 - exp(-1 * fade / transparent)));
	turbidityColor = lerp(refraction, turbidityColor * volumeLight.rgb, fadeExp);
	absorbedColor = lerp(absorbedColor, turbidityColor, turbidity * 0.9 + 0.1);
	
	
	//absorbedColor *= volumeLight.rgb;

	return absorbedColor;
}


float2 GetScreenSpaceReflectionUV(half3 normal, half3 viewDir, float4x4 cameraProjectionMatrix)
{
	normal.xz = mul((float3x3)UNITY_MATRIX_V, half3(normal.x, 0, normal.z)).xz;
	viewDir = mul((float3x3)UNITY_MATRIX_V, viewDir);
	float3 ssrReflRay = reflect(-viewDir, normal);
	float4 ssrScreenPos = mul(cameraProjectionMatrix, float4(ssrReflRay, 1));
	ssrScreenPos.xy /= ssrScreenPos.w;
	return float2(ssrScreenPos.x * 0.5 + 0.5, -ssrScreenPos.y * 0.5 + 0.5);
}

half3 ComputeReflection(float2 uv){
	return tex2D(_ReflectionTexture, float2(uv.x, uv.y));
}
float ComputeWaterFresnel(half3 normal, half3 viewDir)
{
	float x = 1 - saturate(dot(normal, viewDir));
	return 0.02 + 0.98 * x * x * x * x * x * x * x; 
}


float4 cubic(float v) {
	float4 n = float4(1.0, 2.0, 3.0, 4.0) - v;
	float4 s = n * n * n;
	float x = s.x;
	float y = s.y - 4.0 * s.x;
	float z = s.z - 4.0 * s.y + 6.0 * s.x;
	float w = 6.0 - x - y - z;
	return float4(x, y, z, w) * (1.0 / 6.0);
}

inline float4 Texture2DSampleAA(sampler2D tex, float2 uv)
{
	half4 color = tex2D(tex, uv.xy);
	half lum = dot(color.xz, float3(0, 1, 0));

	float2 uv_dx = ddx(uv);
	float2 uv_dy = ddy(uv);

	color += tex2D(tex, uv.xy + (0.25) * uv_dx + (0.75) * uv_dy);
	color += tex2D(tex, uv.xy + (-0.25) * uv_dx + (-0.75) * uv_dy);
	color += tex2D(tex, uv.xy + (-0.75) * uv_dx + (0.25) * uv_dy);
	color += tex2D(tex, uv.xy + (0.75) * uv_dx + (-0.25) * uv_dy);

	color /= 5.0;

	return color;
}

inline float4 Texture2DSampleBicubic(sampler2D tex, float2 uv, float4 texelSize)
{
	uv = uv * texelSize.zw - 0.5;
	float2 fxy = frac(uv);
	uv -= fxy;

	float4 xcubic = cubic(fxy.x);
	float4 ycubic = cubic(fxy.y);

	float4 c = uv.xxyy + float2(-0.5, +1.5).xyxy;
	float4 s = float4(xcubic.xz + xcubic.yw, ycubic.xz + ycubic.yw);
	float4 offset = c + float4(xcubic.yw, ycubic.yw) / s;
	offset *= texelSize.xxyy;

	half4 sample0 = tex2D(tex, offset.xz);
	half4 sample1 = tex2D(tex, offset.yz);
	half4 sample2 = tex2D(tex, offset.xw);
	half4 sample3 = tex2D(tex, offset.yw);

	float sx = s.x / (s.x + s.y);
	float sy = s.z / (s.z + s.w);

	return lerp(lerp(sample3, sample2, sx), lerp(sample1, sample0, sx), sy);
}

inline half3 GetFilteredNormal_lod0(float2 uv, float viewDist, out float normalFilteringMask, sampler2D nt)
{
	half bicubicLodDist = 10 + (1 - 1.0) * 40;
	
	half3 bicubicNormal = Texture2DSampleBicubic(nt, uv, 1.0);
	half3 normalAA = Texture2DSampleAA(nt, uv);
	half3 normal = lerp(bicubicNormal, normalAA, saturate(viewDist / bicubicLodDist));

	float rlen = rcp(saturate(length(normal)));
	normalFilteringMask = rcp(1.0 + 100.0 * (rlen - 1.0));
	normalFilteringMask = lerp(1, normalFilteringMask, saturate(viewDist / 200));
	return normal;
}

// From Unity Standard Shaders
// Ref: http://jcgt.org/published/0003/02/03/paper.pdf
inline float SmithJointGGXVisibilityTerm (float NdotL, float NdotV, float roughness)
{
#if 0
    // Original formulation:
    //  lambda_v    = (-1 + sqrt(a2 * (1 - NdotL2) / NdotL2 + 1)) * 0.5f;
    //  lambda_l    = (-1 + sqrt(a2 * (1 - NdotV2) / NdotV2 + 1)) * 0.5f;
    //  G           = 1 / (1 + lambda_v + lambda_l);

    // Reorder code to be more optimal
    half a          = roughness;
    half a2         = a * a;

    half lambdaV    = NdotL * sqrt((-NdotV * a2 + NdotV) * NdotV + a2);
    half lambdaL    = NdotV * sqrt((-NdotL * a2 + NdotL) * NdotL + a2);

    // Simplify visibility term: (2.0f * NdotL * NdotV) /  ((4.0f * NdotL * NdotV) * (lambda_v + lambda_l + 1e-5f));
    return 0.5f / (lambdaV + lambdaL + 1e-5f);  // This function is not intended to be running on Mobile,
                                                // therefore epsilon is smaller than can be represented by half
#else
    // Approximation of the above formulation (simplify the sqrt, not mathematically correct but close enough)
    float a = roughness;
    float lambdaV = NdotL * (NdotV * (1 - a) + a);
    float lambdaL = NdotV * (NdotL * (1 - a) + a);

#if defined(SHADER_API_SWITCH)
    return 0.5f / (lambdaV + lambdaL + 1e-4f); // work-around against hlslcc rounding error
#else
    return 0.5f / (lambdaV + lambdaL + 1e-5f);
#endif

#endif
}

inline float GGXTerm (float NdotH, float roughness)
{
    float a2 = roughness * roughness;
    float d = (NdotH * a2 - NdotH) * NdotH + 1.0f; // 2 mad
    return UNITY_INV_PI * a2 / (d * d + 1e-7f); // This function is not intended to be running on Mobile,
                                            // therefore epsilon is smaller than what can be represented by half
}

inline half ComputeSpecular(half nl, half nv, half nh, half viewDistNormalized, half smoothness)
{
	half V = SmithJointGGXVisibilityTerm(nl, nv, smoothness);
	half D = GGXTerm(nh, viewDistNormalized * 0.1 + smoothness);

	//half specularTerm = V * D * UNITY_PI;
	half specularTerm = V * D;
	
	//#ifdef UNITY_COLORSPACE_GAMMA
	//	specularTerm = sqrt(max(1e-4h, specularTerm));
	//#endif


	specularTerm = max(0, specularTerm * nl );

	return specularTerm;
}

half3 ComputeSunlight(half3 normal, half3 viewDir, float3 lightDir, float3 lightColor, half shadowMask, float viewDist, float waterFarDistance, half KW_Transparent)
{
	half3 halfDir = normalize(lightDir + viewDir);
	half nh = saturate(dot(normal, halfDir));
	half nl = saturate(dot(normal, lightDir));
	half lh = saturate(dot(lightDir, halfDir));
	half fresnel = saturate(dot(normal, viewDir));
	
	float viewDistNormalized = saturate(viewDist / (waterFarDistance * 2));
	half3 specular = ComputeSpecular(nl, fresnel, nh, viewDistNormalized, 0.04f);
	specular = clamp(specular - 0.25 * saturate(1 - 0.04f * 10), 0, 2);
	//half sunset = saturate(0.01 + dot(lightDir, float3(0, 1, 0))) * 30;

	return shadowMask * specular * lightColor;

}

inline half3 ComputeDisplaceUsingFlowMap(float2 flowMap, half3  displace, float2 uv, float time)
{
	half blendMask = abs(flowMap.x) + abs(flowMap.y);
	if (blendMask < 0.01) return displace;
	
	half time1 = frac(time + 0.5);
	half time2 = frac(time);
	half flowLerp = abs((0.5 - time1) / 0.5);
	half flowLerpFix = lerp(1, 0.65, abs(flowLerp * 2 - 1)); 

	half3 tex1 = tex2Dlod(_DisplacementTexture, float4(uv - 0.25 * flowMap * time1, 0.0, 0.0)) / 10.0 * _DisplacementScale;
	half3 tex2 = tex2Dlod(_DisplacementTexture, float4(uv - 0.25 * flowMap * time2, 0.0, 0.0)) / 10.0 * _DisplacementScale;
	half3 flowDisplace = lerp(tex1, tex2, flowLerp);
	flowDisplace.xz *= flowLerpFix;
	return lerp(displace, flowDisplace, saturate(blendMask));
}


inline half3 ComputeNormalUsingFlowMap(float2 flowMap, half3 normal, float2 uv, float time)
{
	half blendMask = abs(flowMap.x) + abs(flowMap.y);
	//if (blendMask < 0.01) return normal;

	half time1 = frac(time + 0.5);
	half time2 = frac(time);
	half flowLerp = abs((0.5 - time1) / 0.5);
	half flowLerpFix = lerp(1, 0.65, abs(flowLerp * 2 - 1)); //fix interpolation bug, TODO: I need find what cause this. 
	
	half3 tex1 = tex2Dlod(_NormalTexture, float4(uv - 0.25 * flowMap * time1, 0.0, 0.0));
	half3 tex2 = tex2Dlod(_NormalTexture, float4(uv - 0.25 * flowMap * time2, 0.0, 0.0));
	half3 flowNormal = lerp(tex1, tex2, flowLerp);
	flowNormal.xz *= flowLerpFix;
	return lerp(normal, flowNormal, saturate(blendMask));
}

inline half3 GetFlowmapNormal(float3 worldPos, float2 uv, half3 normal)
{
	float2 flowMapUV =  (worldPos.xz - _FlowmapPosition) / _FlowmapScale - float2(0.5, 0.5);
	float2 flowmap = tex2Dlod(_Flowmap, float4(flowMapUV, 0.0, 0.0)) * 2 - 1;
	return ComputeNormalUsingFlowMap(flowmap, normal, uv, _Time.y * _FlowSpeedScale);
}