UNITY_DECLARE_TEX2D(_Decal1);
UNITY_DECLARE_TEX2D_NOSAMPLER(_Decal2);
UNITY_DECLARE_TEX2D_NOSAMPLER(_Decal3);
UNITY_DECLARE_TEX2D_NOSAMPLER(_Decal4);
UNITY_DECLARE_TEX2D_NOSAMPLER(_Decal5);
UNITY_DECLARE_TEX2D_NOSAMPLER(_Decal6);
UNITY_DECLARE_TEX2D_NOSAMPLER(_Decal7);
UNITY_DECLARE_TEX2D_NOSAMPLER(_Decal8);
UNITY_DECLARE_TEX2D_NOSAMPLER(_Decal9);
UNITY_DECLARE_TEX2D_NOSAMPLER(_Decal10);
UNITY_DECLARE_TEX2D_NOSAMPLER(_Decal11);
UNITY_DECLARE_TEX2D_NOSAMPLER(_Decal12);
UNITY_DECLARE_TEX2D_NOSAMPLER(_Decal13);
UNITY_DECLARE_TEX2D_NOSAMPLER(_Decal14);
UNITY_DECLARE_TEX2D_NOSAMPLER(_Decal15);
UNITY_DECLARE_TEX2D_NOSAMPLER(_Decal16);

float4x4 _Transform[16];
float4 _Positions[16];
float _Weigths[16];
float _Transitions[16];
int _DecalCount;

float4 sampleDecal(int index, float2 position){
	if (index == 0){
		return UNITY_SAMPLE_TEX2D(_Decal1, position);
	} else if (index == 1) {
		return UNITY_SAMPLE_TEX2D_SAMPLER(_Decal2, _Decal1, position);
	} else if (index == 2) {
		return UNITY_SAMPLE_TEX2D_SAMPLER(_Decal3, _Decal1, position);
	} else if (index == 3) {
		return UNITY_SAMPLE_TEX2D_SAMPLER(_Decal4, _Decal1, position);
	} else if (index == 4) {
		return UNITY_SAMPLE_TEX2D_SAMPLER(_Decal5, _Decal1, position);
	} else if (index == 5) {
		return UNITY_SAMPLE_TEX2D_SAMPLER(_Decal6, _Decal1, position);
	} else if (index == 6) {
		return UNITY_SAMPLE_TEX2D_SAMPLER(_Decal7, _Decal1, position);
	} else if (index == 7) {
		return UNITY_SAMPLE_TEX2D_SAMPLER(_Decal8, _Decal1, position);
	} else if (index == 8) {
		return UNITY_SAMPLE_TEX2D_SAMPLER(_Decal9, _Decal1, position);
	} else if (index == 9) {
		return UNITY_SAMPLE_TEX2D_SAMPLER(_Decal10, _Decal1, position);
	} else if (index == 10) {
		return UNITY_SAMPLE_TEX2D_SAMPLER(_Decal11, _Decal1, position);
	} else if (index == 11) {
		return UNITY_SAMPLE_TEX2D_SAMPLER(_Decal12, _Decal1, position);
	} else if (index == 12) {
		return UNITY_SAMPLE_TEX2D_SAMPLER(_Decal13, _Decal1, position);
	} else if (index == 13) {
		return UNITY_SAMPLE_TEX2D_SAMPLER(_Decal14, _Decal1, position);
	} else if (index == 14) {
		return UNITY_SAMPLE_TEX2D_SAMPLER(_Decal15, _Decal1, position);
	} else if (index == 15) {
		return UNITY_SAMPLE_TEX2D_SAMPLER(_Decal16, _Decal1, position);
	} 

	return float4(0.0, 0.0, 0.0, 0.0);
}

float sampleDecalsHeigth(float3 position, out float h){
	for (int i = 0; i < _DecalCount; i++){
		if (_Transitions[i] == 0.0){
			continue;
		}

		float3 transformed = mul(_Transform[i], position - _Positions[i]);

		transformed.x += 0.5;
		transformed.z += 0.5;

		if (transformed.x > 0 && transformed.x < 1 && transformed.z > 0 && transformed.z < 1){
			h = _Positions[i].y;
			return 1.0 - abs(0.5 - transformed.x);
		}
		
	}

	return 0.0;
}

float4 sampleDecals(float4 waterColor, float3 position){
	float4 clr = waterColor;

	for (int i = 0; i < _DecalCount; i++){
		float3 transformed = mul(_Transform[i], position - _Positions[i]);

		transformed.x += 0.5;
		transformed.z += 0.5;

		if (transformed.x > 0 && transformed.x < 1 && transformed.z > 0 && transformed.z < 1){
			float4 sampled = sampleDecal(i,  float2(transformed.x, transformed.z));

			clr = lerp(clr, float4(sampled.rgb, 1), _Weigths[i] * sampled.a);
		}
		
	}

	return clr;
}