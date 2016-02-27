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

Shader "UsTwo-Cardboard/Environment Background 1" {

Properties {
	_Color ("Main Color", Color) = (1,1,1,1)
}

Category {
	Tags { "Queue"="Geometry" "IgnoreProjector"="True" "RenderType"="Opaque" }

	Blend Off
	AlphaTest Off
	Cull Back 
	Lighting Off 
	ZWrite On 
	ZTest LEqual
	Fog { Mode Off }
	
	SubShader {
		Pass {
			Stencil {
                Ref 0
                Comp Equal
            }
			CGPROGRAM
			#pragma vertex VertexProgram
			#pragma fragment FragmentProgram
			#include "CardboardDistortion.cginc"


			struct VertexInput {
			    float4 position : POSITION;
			    //half2 texcoord : TEXCOORD0;
			    //half3 normal : NORMAL;
			};

			struct VertexToFragment {
			    half4 position : SV_POSITION;

			    half4 fog: TEXCOORD0;

			};



			half4 _ZenithColor;
			half4 _ZenithFog;

			half4 _HorizonColor;
			half4 _HorizonFog;
			half4 _FogData;
			half4 _DirectionalData;
			half4 _DirectionalColor;
			half4 _DirectionalFog;
			VertexToFragment VertexProgram (VertexInput vertex)
			{

			    VertexToFragment output;
			    //output.position = undistort(vertex.position);
			    output.position = mul (UNITY_MATRIX_MVP, vertex.position);

			    float4 worldPosition = mul (_Object2World,vertex.position);
			    float3 pointVector = worldPosition.xyz - _WorldSpaceCameraPos;
			    float distanceToCamera = length(pointVector);
			    float3 normVector = pointVector / (distanceToCamera+0.0001);

			    ///output.position.z
			    
			    half fogVertical = saturate(normVector.y);

			    ///We can skip the normalize here
			   half fogAngle = _DirectionalData.x * saturate((dot(_DirectionalData.zw,normalize(normVector.xz) )));//(atan2(normVector.z, normVector.x) / (2 * 3.1415926) ) + 0.5;	

				half3 horizonColor =( (1-fogAngle)*_HorizonColor.rgb + fogAngle*_DirectionalColor) ;	   	
			   	half3 fogColor = (1-fogVertical)*horizonColor + fogVertical*_ZenithColor.rgb;
			   	//half verticalBlend = smoothstep(_FogData.z, _FogData.w, fogVertical);
			    //half3 fogColor = (1-verticalBlend)*_HorizonColor.rgb + verticalBlend*_ZenithColor.rgb;
			    //half3 groundColor = fogAngle*_FogForwardColor.rgb + (1.0-fogAngle) * _FogBackColor.rgb;
			    //half3 fogColor = fogVertical*_FogSkyColor.rgb + (1.0-fogVertical)*groundColor;

			    output.fog = half4(fogColor,1);

			    return output;
			};

			

			fixed4 _Color;

			fixed4 FragmentProgram (VertexToFragment fragment) : COLOR
			{  

				return (1-_Color.a)*fixed4(1,1,1,1) + (_Color.a)*fixed4(fragment.fog.rgb, 1);
			}
			ENDCG
		}
	}
	
}
}
