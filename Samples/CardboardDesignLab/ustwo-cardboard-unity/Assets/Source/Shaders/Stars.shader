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

Shader "UsTwo-Cardboard/Stars" {

Properties {
	_Color ("Main Color", Color) = (1,1,1,1)
	_MainTex ("Main Texture", 2D) = "" {}
}

Category {
	Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }

	Blend One One
	AlphaTest Off
	Cull Off 
	Lighting Off 
	ZWrite Off 
	ZTest LEqual
	Fog { Mode Off }
	
	SubShader {
		Pass {
			CGPROGRAM
			//#include "UnityCG.cginc"
			#pragma vertex VertexProgram
			#pragma fragment FragmentProgram
			#include "CardboardDistortion.cginc"

			struct VertexInput {
			    half4 position : POSITION;
			    half2 texcoord : TEXCOORD0;
			    //half4 tangent : TANGENT;
			};

			struct VertexToFragment {
			    half4 position : SV_POSITION;
			   	half2 uv : TEXCOORD0;
			   	half4 color : TEXCOORD1;
			};	


			VertexToFragment VertexProgram (VertexInput vertex)
			{
			    VertexToFragment output;



			    output.uv = vertex.texcoord;

			    half3 worldPosition = mul(_Object2World, vertex.position).xyz;
			    half3 worldVector = normalize(worldPosition - _WorldSpaceCameraPos.xyz);

			    output.color = half4(1,1,1, saturate( 2*asin(worldVector.y)/(0.5*3.1415926) ) );
			    output.position = undistort(vertex.position);
			    return output;





			    
			   
			};


			half4 _Color;
			sampler2D _MainTex;
			half4 FragmentProgram (VertexToFragment fragment) : COLOR
			{  
				//return abs(half4(fragment.screenPos.xy, 0,1));
				return fragment.color.a*tex2D(_MainTex, fragment.uv).a*_Color;
				//return _Color;
			}
			ENDCG
		}
	}
	
}
}
