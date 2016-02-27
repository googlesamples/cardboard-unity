// Copyright 2014 Google Inc. All rights reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

Shader "UsTwo-Cardboard/Reticle" {

Properties {
	_Color ("Main Color", Color) = (1,1,1,1)

}

Category {
	Tags { "Queue"="Overlay" "IgnoreProjector"="True" "RenderType"="Transparent" }

	Blend SrcAlpha OneMinusSrcAlpha
	AlphaTest Off
	Cull Back 
	Lighting Off 
	ZWrite Off 
	ZTest Always

	Fog { Mode Off }
	
	SubShader {
		Pass {
			CGPROGRAM
			#pragma vertex VertexProgram
			#pragma fragment FragmentProgram
			#include "CardboardDistortion.cginc"
			struct VertexInput {
			    half4 position : POSITION;
			    half2 texcoord : TEXCOORD0;
			};

			struct VertexToFragment {
			    half4 position : SV_POSITION;
			    half4 uv : TEXCOORD0;

			};


			VertexToFragment VertexProgram (VertexInput vertex)
			{
			    VertexToFragment output;

			    ///If we make the UV.zw negative here, the ATAN2 goes in the correct direction for the desired countdown effect
			    output.uv = half4(vertex.texcoord, -2.0*(vertex.texcoord - half2(0.5,0.5)) );

			    half4 center = undistort(half4(0,0,0,1));
			    half4 edge = undistort(half4(1,1,0,1));

			    center = center/center.w;
			    edge = edge/edge.w;

			    
			    half scale = 0.10/abs(edge.x - center.x);

			    output.position = undistort(half4(scale,scale, 1, 1)*vertex.position);
			    output.position = (output.position/(output.position.w));

			    return output;
			};

			half4 _Color;
			half4 _Reticle;
			half4 FragmentProgram (VertexToFragment fragment) : COLOR
			{  

				half uvDistance = length(fragment.uv.zw );
				half angle = atan2(fragment.uv.z, fragment.uv.w)/(2.0*3.1415926)+ 0.5;
				half angleAlpha = 1-smoothstep(_Reticle.z, _Reticle.z + 0.01, angle);

				//half alpha = smoothstep(0.88,0.95, 1-uvDistance);


				half ringOuterAlpha = smoothstep(0.17+_Reticle.w,0.24+_Reticle.w, 1-uvDistance);
				half ringInnerAlpha = smoothstep(0.65-_Reticle.w,0.72-_Reticle.w, uvDistance);

				return half4(_Color.rgb,_Color.a*angleAlpha*ringOuterAlpha*ringInnerAlpha );//+ 0*alpha
			}
			ENDCG
		}
	}
	
}
}
