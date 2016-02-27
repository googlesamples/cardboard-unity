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

Shader "UsTwo-Cardboard/Transparent Texture Overlay Alpha Undistortion" {

Properties {
	_Color ("Main Color", Color) = (1,1,1,1)
	_MainTex ("Main Texture (A)", 2D) = "" {}
}

Category {
	Tags { "Queue"="Overlay" "IgnoreProjector"="True" "RenderType"="Transparent" }

	Blend SrcAlpha OneMinusSrcAlpha
	AlphaTest Off
	Cull Off 
	Lighting Off 
	ZWrite Off 
	ZTest Always

	Fog { Mode Off }
	
	SubShader {
		Pass {
			CGPROGRAM
			#pragma vertex VertexProgram
			#pragma fragment FragmentProgram
			#include "UnityCG.cginc"
			#include "CardboardDistortion.cginc"
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
			    output.position = undistort(vertex.position);
			    output.uv = TRANSFORM_TEX (vertex.texcoord, _MainTex);
			    return output;
			};


			sampler2D _MainTex;
			half4 _Color;
			half4 FragmentProgram (VertexToFragment fragment) : COLOR
			{  
				half alpha = tex2D(_MainTex, fragment.uv).a;
				half4 col = _Color;
				col.a *= alpha;
				return col;
			}
			ENDCG
		}
	}
	
}
}
