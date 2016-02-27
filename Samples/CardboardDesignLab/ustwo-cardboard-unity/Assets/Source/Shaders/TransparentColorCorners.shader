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

Shader "UsTwo-Cardboard/Transparent Color Undistortion Rounded Corners" {

Properties {
	_Color ("Main Color", Color) = (1,1,1,1)
	_Radius ("Radius", Float) = 0

	//_MainTex ("Main Texture", 2D) = "" {}
}

Category {
	Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }

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
			#include "CardboardDistortion.cginc"
			#include "UnityCG.cginc"

			struct VertexInput {
			    half4 position : POSITION;
			    half4 texcoord : TEXCOORD0;
			};

			struct VertexToFragment {
			    half4 position : SV_POSITION;
			    half3 vertexPosition : TEXCOORD0;
			    //half2 screenPos : TEXCOORD1;
			    half2 uv : TEXCOORD2;
			};


			VertexToFragment VertexProgram (VertexInput vertex)
			{
			    VertexToFragment output;

				///unity_scale.w ??
			    output.vertexPosition = vertex.position.xyz;//2.0*(vertex.texcoord -half2(0.5f,0.5));

			    output.position = undistort(vertex.position);
			   // half4 center = undistort(half4(0,0,0,1));
			    //half2 screenCenter = ComputeScreenPos(center);
			    //output.screenPos = ComputeScreenPos(output.position) - screenCenter;
			    output.uv = 2*(vertex.texcoord.xy -0.5);
			    //output.position = mul (UNITY_MATRIX_MVP, vertex.position);
			    return output;
			};

			fixed _Radius;
			fixed4 _Color;
			fixed4 FragmentProgram (VertexToFragment fragment) : COLOR
			{  

				fixed2 dist = abs(fragment.uv); 
				fixed2 pixels = abs(fragment.vertexPosition.xz); 

				fixed2 norm =   pixels/(dist+0.001);

				fixed2 edgeDist = abs(pixels-norm);

				fixed alpha = 1;
				
					if(dist.x > 0.1 && dist.y > 0.1 && _Radius > edgeDist.x  && _Radius > edgeDist.y ){//} && pixels.y > cornerSize.y ){
						fixed2 cornerV = fixed2(_Radius - edgeDist.x, _Radius-edgeDist.y );
						alpha = 1-10*saturate(length(cornerV)/_Radius-0.9);
					}
					else if(dist.x > 0.1 && _Radius > edgeDist.x){
						alpha = 1-10*saturate( (_Radius - edgeDist.x)/_Radius-0.9);
					}
					else if(dist.y > 0.1 && _Radius > edgeDist.y){
						alpha = 1-10*saturate( (_Radius - edgeDist.y)/_Radius-0.9);
					}
				
				
				return fixed4(_Color.rgb,_Color.a*alpha);// half4(_Color.rgb, dist.x);
			}
			ENDCG
		}
	}
	
}
}
