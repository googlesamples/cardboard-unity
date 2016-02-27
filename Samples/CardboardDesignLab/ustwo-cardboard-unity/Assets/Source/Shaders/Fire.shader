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

Shader "UsTwo-Cardboard/Fire" {

Properties {
	_Color ("Main Color", Color) = (1,1,1,1)
	_RimColor ("Rim Color", Color) = (1,1,1,1)
	//_MainTex ("Main Texture", 2D) = "" {}
}

Category {
	Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Opaque" }

	Blend One OneMinusSrcAlpha
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
			    half4 position : POSITION;
			    half2 texcoord : TEXCOORD0;
			    half3 normal : NORMAL;
			};

			struct VertexToFragment {
			    half4 position : SV_POSITION;
			    half4 color : TEXCOORD0;
			    half4 fog: TEXCOORD1;
			};

			half4 _Color;
			half4 _RimColor;





			VertexToFragment VertexProgram (VertexInput vertex)
			{
			    VertexToFragment output;

			    half alpha = 0.9;
			    
			    if(vertex.texcoord.y < 0.5){
			    	half x = .2*_Time.y + 3*3.14*vertex.texcoord.x;
			    	half x2 = .1*_Time.y + (1.17*3*3.14)*vertex.texcoord.x;
			    	half mod = fmod((_Time.y + 2*vertex.texcoord.x)/(0.4 + 0.1*sin(vertex.position.x+fmod(0.11*_Time.x,1)) - 0.11*sin(vertex.position.z+fmod(0.06*_Time.x,1.1) )), 1);
			    	vertex.position.y += (1-0.25*length(vertex.position.xz))*2*saturate(vertex.texcoord.y-0.1)*(mod*mod + mod);
			    	half val =vertex.texcoord.y*0.3*( sin(4*_Time.y + 5*vertex.texcoord.x) ) + 0.2*( sin(_Time.x + 5*vertex.texcoord.x) );
			    	vertex.position.xz = vertex.position.xz + val*vertex.position.zx;
			    }
			    else if(vertex.texcoord.x > 0.5) {
			    	half val = fmod(2*_Time.y, 1);
			    	if(vertex.texcoord.y < 0.7){
			    		half scale = saturate(1-fmod(2*_Time.y, 7) );
			    		vertex.position.xyz *= scale;
			    		
			    		vertex.position.y += 1.5 + 2*val;
			    		vertex.position.xz += 0.25 + 1.5*sqrt(val)-val;


			    	}
			    	else if(vertex.texcoord.y < 0.85){
			    		half scale = saturate(1-fmod(1.3*_Time.y+2, 5.1) );
			    		vertex.position.xyz *= scale;
			    		
			    		vertex.position.y += 1.6 + 2*val;
			    		vertex.position.xz += 0.25 + 1.5*sqrt(val)-val;
			    		
			    	}
			    	else{
			    		half scale = saturate(1-fmod(1.7*_Time.y +5, 6.5) );
			    		vertex.position.xyz *= scale;
			    		
			    		vertex.position.y += 1.4 + 2*val;
			    		vertex.position.xz -= 0.25 + 1.5*sqrt(val)-val;

			    	}

			    }
			    half3 worldPosition = mul(_Object2World, vertex.position).xyz;


			    half3 pointVector = worldPosition.xyz - _WorldSpaceCameraPos;

			    half distanceToCamera = length(pointVector);
			    half3 normVector = pointVector / (distanceToCamera+0.0001);


			    half3 worldNormal = normalize(mul(_Object2World, half4(vertex.normal,0)).xyz);
			    //half3 pointRay = normalize(vertex.position.xyz);
			    half dotP = ( dot(worldNormal, normVector) + 1 ) ;


			   	//half dotP = length(vertex.position.xz);
			    output.color = (1-dotP)*_Color + dotP*_RimColor;
			    output.color.a *= alpha * saturate(12/(distanceToCamera+1) );
			    output.color.rgb *= output.color.a;
			    output.position = undistort(vertex.position);



			    return output;
			};


			
			half4 FragmentProgram (VertexToFragment fragment) : COLOR
			{  
				return fragment.color;
			}
			ENDCG
		}
	}
	
}
}
