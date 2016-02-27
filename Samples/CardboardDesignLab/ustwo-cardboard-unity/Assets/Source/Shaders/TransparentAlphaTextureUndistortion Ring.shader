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

Shader "UsTwo-Cardboard/Transparent Alpha Texture Undistortion Ring" {

Properties {
	_Color ("Main Color", Color) = (1,1,1,1)
	_MainTex ("Main Texture", 2D) = "" {}
}

Category {
	Tags { "Queue"="Transparent -2" "IgnoreProjector"="True" "RenderType"="Transparent" }

	Blend SrcAlpha OneMinusSrcAlpha
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
			#include "UnityCG.cginc"
			struct VertexInput {
			    half4 position : POSITION;
			    half2 texcoord : TEXCOORD0;
			};

			struct VertexToFragment {
			    half4 position : SV_POSITION;
			   	half2 uv : TEXCOORD0;
			};	

			half4 _MainTex_ST;

			VertexToFragment VertexProgram (VertexInput vertex)
			{
			    VertexToFragment output;

			    //output.uv = vertex.texcoord;
			    output.uv = TRANSFORM_TEX(vertex.texcoord, _MainTex);
			   	output.position = undistort(vertex.position);
			    //output.position = mul(UNITY_MATRIX_MVP, undistort(vertex.position));
			    return output;
			   

			    
			   
			};


			half4 _Color;
			sampler2D _MainTex;
			half4 FragmentProgram (VertexToFragment fragment) : COLOR
			{  
				//return abs(half4(fragment.screenPos.xy, 0,1));

				return half4(_Color.rgb,_Color.a*tex2D(_MainTex, fragment.uv).a);
				//return _Color;
			}
			ENDCG
		}
	}
	
}
}
