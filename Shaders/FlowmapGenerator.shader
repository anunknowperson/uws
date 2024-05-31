Shader "Hidden/FlowmapGenerator"
{
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

            float2 _Position;
            float2 _Scale;

            StructuredBuffer<float2> _RiverPoints;
            int _RiverPointsCount;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            
            float2 nearestPoint(float2 start, float2 end, float2 pnt) {
				float2 ln = (end - start);
				float len = length(ln);
				ln = normalize(ln);
			
				float2 v = pnt - start;
				float d = dot(v, ln);
				d = clamp(d, 0.0, len);
				return start + ln * d;
			}
			
			float2 GetDirection(StructuredBuffer<float2> buffer_points, int end, float width, float2 test_point, out bool onl) {
				float minv;
				float2 direction;

				for (int i = 0; i < end; i++) {
					float2 A = buffer_points[i];
					float2 B = buffer_points[i+1];
					float2 E = test_point;
			
					float2 nearest = nearestPoint(A, B, E);
			
			
					float reqAns = distance(E, nearest);
					float distance_A = distance(A, nearest);
					float distance_B = distance(B, nearest);
			
					float t = distance_A / (distance_A + distance_B);
					
			
					if (i == 0) {
						minv = reqAns;
						
			
						direction = normalize(B - A);
			
			
					} else{
						if (reqAns < minv) {
							minv = reqAns;
							
			
							direction = normalize(B - A);
			
						}
					}
				}

				onl = minv < width;
				
				return direction;
			}

			bool isInsidePolygon2D(StructuredBuffer<float2> buffer_points, int start, int end, float2 test_point) {
				for (int i = 0; i < end; i++){
					if (distance(buffer_points[i], test_point) < 5.0){
						return true;
					}
				}

				return false;
			}

            fixed4 frag (v2f i) : SV_Target
            {
                float x = _Position.x + ((i.uv.x - 0.5)) * _Scale.x * 1.0;
				float y = _Position.y + (i.uv.y - 0.5) * _Scale.y * 1.0;
                
				bool onl;
				float2 dir = GetDirection(_RiverPoints, _RiverPointsCount - 1, 5.0, float2(x, y),  onl);


				//if (!onl){
				//	dir = float2(0.0, 0.0);
				//}

				//if (isInsidePolygon2D(_RiverPoints, 0, _RiverPointsCount, float2(x, y))){
				//	return fixed4(1.0, 0.0, 0.0, 1.0);
				//} else {
				//	return fixed4(0.0, 0.0, 0.0, 1.0);
				//}

                return fixed4((dir.x + 1) / 2, (dir.y + 1) / 2, 0.0, 1.0);
            }
            ENDCG
        }
    }
}
