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

Shader "UsTwo-Cardboard/Solid Color Menu Stencil" {

Properties {
	_Color ("Main Color", Color) = (1,1,1,1)
	//_MainTex ("Main Texture", 2D) = "" {}
}

Category {
	Tags { "Queue"="Geometry-1" "IgnoreProjector"="True" "RenderType"="Opaque" }

	Blend Off
	AlphaTest Off
	Cull Off 
	
	Lighting Off 
	ZWrite On 
	ZTest Always

	Fog { Mode Off }
	
	SubShader {
		Pass {
			Stencil {
                Ref 3
                Comp always
                Pass replace
            }
			CGPROGRAM
			#pragma vertex VertexProgram
			#pragma fragment FragmentProgram
			#include "CardboardDistortion.cginc"
			
			struct VertexInput {
			    half4 position : POSITION;
			};

			struct VertexToFragment {
			    half4 position : SV_POSITION;
			};


			VertexToFragment VertexProgram (VertexInput vertex)
			{
			    VertexToFragment output;
			    output.position = undistort(vertex.position);
			    //output.position = mul (UNITY_MATRIX_MVP, vertex.position);
			    return output;
			};


			half4 _Color;
			half4 FragmentProgram (VertexToFragment fragment) : COLOR
			{  
				return _Color;
			}
			ENDCG
		}
	}
	
}
}
