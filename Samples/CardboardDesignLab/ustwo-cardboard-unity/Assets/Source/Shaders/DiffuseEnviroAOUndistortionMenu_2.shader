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

Shader "UsTwo-Cardboard/Diffuse Environment AO Undistortion Menu 2" {

Properties {
	_Color ("Main Color", Color) = (1,1,1,1)
	_Diffuse ("Diffuse", 2D) = "" {}
	_AmbientOcclusion ("AO (A)", 2D) = "white" {}
	//_EnvironmentCube("Environment Cubemape", 2D) = "" {}
}

Category {
	Tags { "Queue"="Geometry" "IgnoreProjector"="True" "RenderType"="Transparent" }

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
			#include "UnityCG.cginc"
			#include "UstwoFog.cginc"
			struct VertexInput {
			    float4 position : POSITION;
			    half2 texcoord : TEXCOORD0;
			    half3 normal : NORMAL;
			};

			struct VertexToFragment {
			    half4 position : SV_POSITION;
			    half2 uv : TEXCOORD0;
			    half4 NDotL_Rim : TEXCOORD1;
			    //half2 polarNormal : TEXCOORD2;
			    half4 fog: TEXCOORD3;
			    half3 ambientColor : TEXCOORD2;
			    half3 shadowPoint:TEXCOORD4;
			};

			half4 _PrimaryLightDirection2;
			half4x4 _ShadowMatrix;
			half3 _ShadowColor2;

			half4 _ZenithColor2;
			half4 _ZenithFog2;

			half4 _HorizonColor2;
			half4 _HorizonFog2;
			half4 _FogData2;
			half4 _PrimaryAmbientColor2;
			half4 _DirectionalData2;
			half4 _DirectionalColor2;
			half4 _DirectionalFog2;

			half4 _Diffuse_ST;
			half4 _PrimaryLightColor2;
			VertexToFragment VertexProgram (VertexInput vertex)
			{
			    VertexToFragment output;
			    output.position = undistort(vertex.position);
			    //output.position = mul (UNITY_MATRIX_MVP, vertex.position);
			    output.uv = TRANSFORM_TEX(vertex.texcoord, _Diffuse);
			    //output.uv = vertex.texcoord;
			    half3 worldNormal = mul(_Object2World, half4(vertex.normal,0)).xyz;
			    output.NDotL_Rim.xyz = (_PrimaryLightColor2.a*saturate(0.5 * dot(worldNormal, _PrimaryLightDirection2) + 0.5))*_PrimaryLightColor2.rgb;
			    //output.polarNormal = half2((atan2(worldNormal.z, worldNormal.x) / (2 * 3.1415926) ) + 0.5, asin(worldNormal.y)/(3.1415926) + 0.5);



			    float4 worldPosition = mul (_Object2World,vertex.position);
			    float3 pointVector = worldPosition.xyz - _WorldSpaceCameraPos;
			    float distanceToCamera = length(pointVector);
			    float3 normVector = pointVector / (distanceToCamera+0.0001);

			    half rimPower = saturate(1-(-dot(worldNormal, normVector)));
			    
			    half ambientBlend = saturate(0.5*(worldNormal.y + 1));
			    output.ambientColor = (_PrimaryAmbientColor2.a )*( ambientBlend * _ZenithColor2.rgb + (1-ambientBlend) * _PrimaryAmbientColor2.rgb);
			    output.NDotL_Rim.w = (0.5 + 0.5* (_PrimaryLightDirection2.w*rimPower) );

			    
			    output.fog = fog(distanceToCamera, normVector, _ZenithColor2, _ZenithFog2, _HorizonColor2,_HorizonFog2,_FogData2, _PrimaryAmbientColor2,_DirectionalData2,_DirectionalColor2, _DirectionalFog2);
			    output.fog.rgb *= output.fog.a;
			   	output.NDotL_Rim.w *=(1.0-output.fog.a);

			    half4 shadowPoint = mul(_ShadowMatrix, worldPosition);
			    output.shadowPoint = half3(0.5*(shadowPoint.xy/abs(shadowPoint.w) + half2(1,1)),shadowPoint.z/abs(shadowPoint.w)) ;
			    return output;
			};

			

			/*inline half DecodeFloatRGBA( half4 enc )
			{
				half4 kDecodeDot = half4(1.0, 1/255.0, 1/65025.0, 1/16581375.0);
				return dot( enc, kDecodeDot );
			}*/

			
			
			half4 _Color;
			sampler2D _Diffuse;
			sampler2D _AmbientOcclusion;
			sampler2D _ShadowDepth;
			half4 FragmentProgram (VertexToFragment fragment) : COLOR
			{  

				half shadowing = Shadow(_ShadowDepth, fragment.shadowPoint.xyz);
				
				half3 diffuse = tex2D(_Diffuse, fragment.uv).rgb;
				half ao = 0.5*tex2D(_AmbientOcclusion, fragment.uv).a;

				half3 ambient = ao *fragment.ambientColor;
				half3 lighting = saturate(ao + 0.5)*fragment.NDotL_Rim.xyz*(shadowing  + (1-shadowing)*_ShadowColor2 );//diffuse*_PrimaryLightColor.rgb



				//shadowing = ;
				half3 color = (ambient + lighting)*(fragment.NDotL_Rim.w*diffuse);
				color = fragment.fog.rgb + color;

				return (1-_Color.a)*half4(1,1,1,1) + (_Color.a)*half4(color, 1);

			}
			ENDCG
		}
	}
	
}
}
