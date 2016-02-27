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

Shader "UsTwo-Cardboard/Ocean" {

Properties {
	_Color ("Main Color", Color) = (1,1,1,1)
	_Color2 ("High Color", Color) = (1,1,1,1)
	_Noise ("Noise", 2D) = "" {}
	//_AmbientOcclusion ("AO (A)", 2D) = "" {}
	//_EnvironmentCube("Environment Cubemape", 2D) = "" {}
}

Category {
	Tags { "Queue"="Geometry" "IgnoreProjector"="True" "RenderType"="Opaque" }

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
			#pragma glsl
			struct VertexInput {
			    half4 position : POSITION;
			    half2 texcoord : TEXCOORD0;
			    half3 normal : NORMAL;
			};

			struct VertexToFragment {
			    half4 position : SV_POSITION;
			    half2 uv : TEXCOORD0;
			    half3 normal : TEXCOORD1;
			    half3 worldPosition : TEXCOORD2;
			};

			sampler2D _Noise;
			VertexToFragment VertexProgram (VertexInput vertex)
			{
			    VertexToFragment output;
			    output.worldPosition = mul (_Object2World, vertex.position).xyz;
			    output.uv = vertex.texcoord;

			    half noiseTex = tex2D(_Noise, vertex.texcoord + half2(_Time.x, _Time.x)).a;
			    half noiseTexX = tex2D(_Noise, vertex.texcoord + half2(_Time.x +.01, _Time.x)).a;
			    half noiseTexZ = tex2D(_Noise, vertex.texcoord + half2(_Time.x, _Time.x +.01)).a;



			    half3 worldPositionX = output.worldPosition;
			    half3 worldPositionZ = output.worldPosition;


			    output.worldPosition.y += 20*noiseTex;
			    worldPositionX.y += 20*noiseTexX;
			    worldPositionZ.y += 20*noiseTexZ;
			    //half3 worldPositionMinusX = output.worldPosition;
			   // half3 worldPositionMinusZ = output.worldPosition;
			    //output.worldPosition.y += 0.5*sin(_Time.y + 0.5*(output.worldPosition.x + output.worldPosition.z) );
			    //output.worldPosition.y += 0.1*sin(4*_Time.y + 0.5*(-output.worldPosition.x + output.worldPosition.z) );
			    
			    output.position = mul (UNITY_MATRIX_VP, half4(output.worldPosition,1));

			    worldPositionX += half3(-1,0,0);
			    worldPositionZ += half3(0,0,1);


			    //worldPositionX.y += sin(_Time.y + 0.5*(worldPositionX.x + worldPositionX.z) );
			    //worldPositionZ.y += sin(_Time.y + 0.5*(worldPositionZ.x + worldPositionZ.z) );




			    half3 deltaX = normalize(worldPositionX -output.worldPosition);
			    half3 deltaZ = normalize(worldPositionZ - output.worldPosition);

			    output.normal =cross((deltaX), (deltaZ) );// + cross((deltaMinusX), (deltaMinusZ) ));
			    
			    //output.normal = mul(_Object2World, half4(vertex.normal,0)).xyz;

			    return output;
			};

			half3 _PrimaryLightDirection;
			half4 _PrimaryLightColor;

			half4 _Color;
			half4 _Color2;
			//sampler2D _Diffuse;
			//sampler2D _EnvironmentCube;
			//sampler2D _AmbientOcclusion;

			half4 FragmentProgram (VertexToFragment fragment) : COLOR
			{  

				half noiseTex = tex2D(_Noise, fragment.uv).a;
				half blend = fragment.worldPosition.y;
				half3 col = blend * _Color2.rgb + (1-blend)*_Color.rgb;
				//return half4(col, 1);

				half3 worldNormal = normalize(fragment.normal);
				half NDotL = saturate(0.5 * dot(worldNormal, _PrimaryLightDirection) + 0.5);
				//half3 diffuse = tex2D(_Diffuse, fragment.uv).rgb;
				//half ao = tex2D(_AmbientOcclusion, fragment.uv).a;
				///instead of a cubemap, we'll do the real math, to get a better result, fewer texlookups, too
				//half3 ambient = texCUBE(_EnvironmentCube, worldNormal).rgb;
				///We can skip the ASIN op, and encode the texture warped vertically.  Encoding the ATAN2 is not as easy, so we'll eat it
				//half2 polarNormal = half2((atan2(worldNormal.z, worldNormal.x) / (2 * 3.1415926) ) + 0.5, 0.5*(worldNormal.y) + 0.5);
				//half3 ambient = ao * tex2D(_EnvironmentCube, polarNormal).rgb;
				//half3 color = ambient + NDotL*diffuse*_PrimaryLightColor.rgb*2*_PrimaryLightColor.a;

				//return half4(color, 1);
				return NDotL;
				return _Color;
			}
			ENDCG
		}
	}
	
}
}
