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

Shader "UsTwo-Cardboard/Diffuse Environment AO Undistortion Solid Color" {

Properties {
	_Color ("Main Color", Color) = (1,1,1,1)
	//_Diffuse ("Diffuse", 2D) = "" {}
	_AmbientOcclusion ("AO (A)", 2D) = "white" {}
	//_EnvironmentCube("Environment Cubemape", 2D) = "" {}
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
			
			CGPROGRAM
			#pragma vertex VertexProgram
			#pragma fragment FragmentProgram
			#include "CardboardDistortion.cginc"
			#include "UnityCG.cginc"
			#include "UstwoFog.cginc"

			struct VertexInput {
			    float4 position : POSITION;
			    float2 texcoord : TEXCOORD0;
			    float3 normal : NORMAL;
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

			half4 _PrimaryLightDirection;
			half4x4 _ShadowMatrix;
			half3 _ShadowColor;

			half4 _ZenithColor;
			half4 _ZenithFog;

			half4 _HorizonColor;
			half4 _HorizonFog;
			half4 _FogData;
			half4 _PrimaryAmbientColor;
			half4 _DirectionalData;
			half4 _DirectionalColor;
			half4 _DirectionalFog;

			half4 _PointLightPosition;
			half4 _PointLightColor;

			half4 _AmbientOcclusion_ST;
			half4 _PrimaryLightColor;
			VertexToFragment VertexProgram (VertexInput vertex)
			{
			    VertexToFragment output;
			    output.position = undistort(vertex.position);
			    //output.position = mul (UNITY_MATRIX_MVP, vertex.position);
			    output.uv = TRANSFORM_TEX(vertex.texcoord, _AmbientOcclusion);
			    //output.uv = vertex.texcoord;
			    float3 worldNormal = normalize(mul(_Object2World, float4(vertex.normal,0)).xyz);
			    output.NDotL_Rim.xyz = (_PrimaryLightColor.a*saturate(0.5 * dot(worldNormal, _PrimaryLightDirection) + 0.5))*_PrimaryLightColor.rgb;
			    //output.polarNormal = half2((atan2(worldNormal.z, worldNormal.x) / (2 * 3.1415926) ) + 0.5, asin(worldNormal.y)/(3.1415926) + 0.5);


			    float4 worldPosition = mul (_Object2World,vertex.position);
			    float3 pointVector = worldPosition.xyz - _WorldSpaceCameraPos;
			    float distanceToCamera = length(pointVector);
			    float3 normVector = pointVector / (distanceToCamera+0.0001);

			    half rimPower = saturate(1-(-dot(worldNormal, normVector)));
			    
			    half ambientBlend = saturate(0.5*(worldNormal.y + 1));
			    output.ambientColor = (_PrimaryAmbientColor.a )*( ambientBlend * _ZenithColor.rgb + (1-ambientBlend) * _PrimaryAmbientColor.rgb);
			    output.NDotL_Rim.w = (0.5 + 0.5* (_PrimaryLightDirection.w*rimPower) );
			    
			    output.fog = fog(distanceToCamera, normVector, _ZenithColor, _ZenithFog, _HorizonColor,_HorizonFog,_FogData, _PrimaryAmbientColor,_DirectionalData,_DirectionalColor, _DirectionalFog);
			    output.fog.rgb *= output.fog.a;

			    float3 lVector = ( _PointLightPosition.xyz-worldPosition.xyz );
			    float len = length(lVector);
			    float3 normLVector = lVector / (len+0.001); 

			    float atten = 1 + len/(0.01 + _PointLightPosition.w);
				half3 pLight = saturate( dot(normLVector, worldNormal) )*8*_PointLightColor.rgb * 1/(atten*atten);
			    output.fog.rgb += pLight;
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

			//sampler2D _Diffuse;
			sampler2D _AmbientOcclusion;
			sampler2D _ShadowDepth;
			fixed4 FragmentProgram (VertexToFragment fragment) : COLOR
			{  

				fixed shadowing = Shadow(_ShadowDepth, fragment.shadowPoint.xyz);
				//return half4(shadowing,0,0,1);
				fixed3 diffuse = _Color.rgb;//tex2D(_Diffuse, fragment.uv).rgb;
				fixed ao = 0.5*tex2D(_AmbientOcclusion, fragment.uv).a;

				fixed3 ambient = ao *fragment.ambientColor;
				fixed3 lighting = saturate(ao + 0.5)*fragment.NDotL_Rim.xyz*(shadowing  + (1-shadowing)*_ShadowColor );//diffuse*_PrimaryLightColor.rgb



				//shadowing = ;
				fixed3 color = (ambient + lighting)*(fragment.NDotL_Rim.w*diffuse);
				color = fragment.fog.rgb + color;
				
				return fixed4(color, 1);

			}
			ENDCG
		}
	}
	
}
}
