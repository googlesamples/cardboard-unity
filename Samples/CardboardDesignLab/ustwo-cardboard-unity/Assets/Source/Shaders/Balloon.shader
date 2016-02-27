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

Shader "UsTwo-Cardboard/Balloon" {

Properties {
	_Explosion ("Explosion", Float) = 0
	_Color ("Main Color", Color) = (1,1,1,1)
	//_Diffuse ("Diffuse", 2D) = "" {}
	_AmbientOcclusion ("AO (A)", 2D) = "" {}
	//_EnvironmentCube("Environment Cubemape", 2D) = "" {}
}

Category {
	Tags { "Queue"="Geometry" "IgnoreProjector"="True" "RenderType"="OpaqueBalloon"}

	Blend Off
	AlphaTest Off
	Cull Off 
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
			    half4 position : POSITION;
			    half2 texcoord : TEXCOORD0;
			    half3 normal : NORMAL;
			};

			struct VertexToFragment {
			    half4 position : SV_POSITION;
			    half2 uv : TEXCOORD0;
			    half2 NDotL_Rim : TEXCOORD1;
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

			half4 _Diffuse_ST;

			half _Explosion;
			VertexToFragment VertexProgram (VertexInput vertex)
			{
			    VertexToFragment output;

			    half3 worldNormal = mul(_Object2World, half4(vertex.normal,0)).xyz;
			    half4 pos = vertex.position;
			    pos.xyz = pos.xyz*(1.0-_Explosion) + worldNormal*(_Explosion);
			    output.position = undistort(pos);
			    //output.position = mul (UNITY_MATRIX_MVP, vertex.position);
			    output.uv = TRANSFORM_TEX(vertex.texcoord, _Diffuse);
			    //output.uv = vertex.texcoord;
			    
			    output.NDotL_Rim.x = saturate(0.5 * dot(worldNormal, _PrimaryLightDirection) + 0.5);
			    //output.polarNormal = half2((atan2(worldNormal.z, worldNormal.x) / (2 * 3.1415926) ) + 0.5, asin(worldNormal.y)/(3.1415926) + 0.5);


			    half4 worldPosition = mul (_Object2World,vertex.position);

			    half3 pointVector = worldPosition.xyz - _WorldSpaceCameraPos;

			    half distanceToCamera = length(pointVector);
			    half3 normVector = pointVector / (distanceToCamera+0.0001);

			    half rimPower = saturate(1-(-dot(worldNormal, normVector)));
			    
			    half ambientBlend = saturate(0.5*(worldNormal.y + 1));
			    output.ambientColor = (_PrimaryAmbientColor.a )*( ambientBlend * _ZenithColor.rgb + (1-ambientBlend) * _PrimaryAmbientColor.rgb);
			    output.NDotL_Rim.y = _PrimaryLightDirection.w*rimPower;
			    output.fog = fog(distanceToCamera, normVector, _ZenithColor, _ZenithFog, _HorizonColor,_HorizonFog,_FogData, _PrimaryAmbientColor,_DirectionalData,_DirectionalColor, _DirectionalFog);

			   	
			    half4 shadowPoint = mul(_ShadowMatrix, worldPosition);
			    output.shadowPoint = half3(0.5*(shadowPoint.xy/abs(shadowPoint.w) + half2(1,1)),shadowPoint.z/abs(shadowPoint.w)) ;
			    return output;
			};

			

			/*inline half DecodeFloatRGBA( half4 enc )
			{
				half4 kDecodeDot = half4(1.0, 1/255.0, 1/65025.0, 1/16581375.0);
				return dot( enc, kDecodeDot );
			}*/

			half4 _PrimaryLightColor;
			
			half4 _Color;

			//sampler2D _Diffuse;
			sampler2D _AmbientOcclusion;
			sampler2D _ShadowDepth;
			half4 FragmentProgram (VertexToFragment fragment) : COLOR
			{  

				half4 compressedShadow = tex2D(_ShadowDepth, fragment.shadowPoint.xy  );
				//half shadowScale = saturate( 10*( max(abs(fragment.shadowPoint.x),abs(fragment.shadowPoint.y) )-0.9) );
				half shadowDepth = DecodeFloatRGBA(compressedShadow);


				///Exponential Shadow Maps
				const half k = 10;
				half expShadowPoint = exp(-k*fragment.shadowPoint.z);
				half shadowing =   saturate(expShadowPoint * exp(k*shadowDepth) );//shadowScale
				shadowing = saturate( shadowing + saturate(10*( max(abs(fragment.shadowPoint.x),abs(fragment.shadowPoint.y) )-0.9)) );

				
				half3 diffuse = _Color.rgb;//tex2D(_Diffuse, fragment.uv).rgb;
				half ao = tex2D(_AmbientOcclusion, fragment.uv).a;

				half3 ambient = ao *fragment.ambientColor;
				half3 lighting = saturate(ao + 0.5)*fragment.NDotL_Rim.x*_PrimaryLightColor.a *  _PrimaryLightColor.rgb*(shadowing  + (1-shadowing)*_ShadowColor );//diffuse*_PrimaryLightColor.rgb



				//shadowing = ;
				half3 color = (ambient + lighting)*((0.5 + 0.5*fragment.NDotL_Rim.y)*diffuse);
				color = fragment.fog.a * fragment.fog.rgb + (1.0-fragment.fog.a)*color;

				return half4(color, 1);

			}
			ENDCG
		}
	}
	
}
}
