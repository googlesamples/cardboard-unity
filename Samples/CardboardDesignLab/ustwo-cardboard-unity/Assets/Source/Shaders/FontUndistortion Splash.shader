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

Shader "UsTwo-Cardboard/Font Undistortion Splash" {

Properties {
	_Color ("Text Color", Color) = (1,1,1,1)
	_MainTex ("Main Texture", 2D) = "" {}
}

Category {
	Tags {
			"Queue"="Overlay +2"
			"IgnoreProjector"="True"
			"RenderType"="Transparent"
			"PreviewType"="Plane"
		}
		Lighting Off 
		Cull Off 
		ZTest Always 
		ZWrite Off 
		Fog { Mode Off }
		Blend SrcAlpha OneMinusSrcAlpha
	
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
			    half4 color : COLOR;
			};

			struct VertexToFragment {
			    half4 position : SV_POSITION;
			   	half2 uv : TEXCOORD0;
			   	half4 color : COLOR;
			};	

			sampler2D _MainTex;
			uniform half4 _MainTex_ST;
			uniform half4 _Color;
			VertexToFragment VertexProgram (VertexInput vertex)
			{
			    VertexToFragment output;

			    output.uv = TRANSFORM_TEX(vertex.texcoord,_MainTex);
			    /*half4 pos = vertex.position;
			    half radius = 3;
			    half circum = 3.1415926*2*radius;
			    half arclength = pos.x / circum;
			    pos.z = 1+cos(arclength);*/
			    output.position = undistort(vertex.position);
			    output.color = vertex.color * _Color;
			    return output;



			    
			   
			};


			half4 FragmentProgram (VertexToFragment fragment) : COLOR
			{  
				half4 col = fragment.color;
				col.a *= tex2D(_MainTex, fragment.uv).a;
				return col;


			}
			ENDCG
		}
	}
	
}
}
