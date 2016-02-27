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

Shader "UsTwo-Cardboard/Additive Undistortion Node" {

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

			struct VertexInput {
			    half4 position : POSITION;
			    half2 texcoord : TEXCOORD0;
			    half4 normal : NORMAL;
			};

			struct VertexToFragment {
			    half4 position : SV_POSITION;
			   	half2 uv : TEXCOORD0;
			   	half3 color : TEXCOORD1;
			};	


			half4 _Color;
			VertexToFragment VertexProgram (VertexInput vertex)
			{
			    VertexToFragment output;

			    output.uv = vertex.texcoord;
			    output.position = undistort(vertex.position);
			    half3 worldNormal = mul(_Object2World, half4(vertex.normal.xyz,0)).xyz;
			    half3 worldPosition  = mul(_Object2World, half4(vertex.position.xyz,1)).xyz;
			    half3 cameraVector = normalize(worldPosition - _WorldSpaceCameraPos); 
			    output.color = _Color.rgb*_Color.a*saturate(-0.1-dot(worldNormal, cameraVector));

			    //output.position = mul(UNITY_MATRIX_MVP, undistort(vertex.position));
			    return output;
			    


			    
			   
			};



			sampler2D _MainTex;
			fixed4 FragmentProgram (VertexToFragment fragment) : COLOR
			{  
				return fixed4(fragment.color*tex2D(_MainTex, fragment.uv).a,1);
				//return _Color*_Color.a;
			}
			ENDCG
		}
	}
	
}
}
