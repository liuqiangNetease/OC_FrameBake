
Shader "OC/Alpha Blend" {
	Properties {
		_Color ("Color Tint", Color) = (1, 1, 1, 0)		
	}
	SubShader {
		Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
		
		Pass {
			Tags { "LightMode"="ForwardBase" }

			ZWrite Off
			Blend SrcAlpha OneMinusSrcAlpha
			
			CGPROGRAM
			
			#pragma vertex vert
			#pragma fragment frag
			
			#include "Lighting.cginc"
			
			fixed4 _Color;			
			
			struct a2v {
				float4 vertex : POSITION;	
			};
			
			struct v2f {
				float4 pos : SV_POSITION;						
			};
			
			v2f vert(a2v v) {
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);		
				return o;
			}
			
			fixed4 frag(v2f i) : SV_Target {
				
				return _Color;
			}
			
			ENDCG
		}
	} 
	FallBack "Transparent/VertexLit"
}
