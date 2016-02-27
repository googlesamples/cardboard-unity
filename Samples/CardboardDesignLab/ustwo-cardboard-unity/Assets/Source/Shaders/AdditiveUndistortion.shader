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

Shader "UsTwo-Cardboard/Additive Undistortion" {

Properties {
	_Color ("Main Color", Color) = (1,1,1,1)
	_MainTex ("Main Texture", 2D) = "" {}
}

Category {
	Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }

	Blend One One
	AlphaTest Off
	Cull Back 
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
			   //	half3 color : TEXCOORD1;
			};	


			half4 _Color;
			half4 _MainTex_ST;
			VertexToFragment VertexProgram (VertexInput vertex)
			{


			    VertexToFragment output;
			    output.position = undistort(vertex.position);
			    output.uv = TRANSFORM_TEX (vertex.texcoord, _MainTex);

			    //output.color = _Color.rgb;

			    return output;
			    


			    
			   
			};



			sampler2D _MainTex;
			half4 FragmentProgram (VertexToFragment fragment) : COLOR
			{  
				return half4(_Color.a*_Color.rgb*tex2D(_MainTex, fragment.uv).a,1);
				//return _Color*_Color.a;
			}
			ENDCG
		}
	}
	
}
}
