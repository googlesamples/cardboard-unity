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

Shader "UsTwo-Cardboard/Solid Texture Undistortion" {

Properties {
	//_Color ("Main Color", Color) = (1,1,1,1)
	//_BarrelPower ("Barrel Power", Float) = 1
	//_BarrelLinear ("Barrel Power", Float) = 0

	//_BarrelPower2 ("Barrel Power 2", Float) = 1
	//_BarrelPower4 ("Barrel Power 4", Float) = 0
	_MainTex ("Main Texture", 2D) = "" {}
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
			//#include "UnityCG.cginc"
			#pragma vertex VertexProgram
			#pragma fragment FragmentProgram
			#include "CardboardDistortion.cginc"

			struct VertexInput {
			    half4 position : POSITION;
			    half2 texcoord : TEXCOORD0;
			};

			struct VertexToFragment {
			    half4 position : SV_POSITION;
			   	half2 uv : TEXCOORD0;
			};	

//
			//half4 _Distortion;   // Radial distortion coefficients.
      		//half4 _Projection;   // Bits of the lens-affected projection matrix.
      		//half4 _Unprojection; // Bits of the no-lens projection matrix.
      		//half4 _Undistortion;   // Radial distortion coefficients.
      		//half4x4 _DistProjection;

      		
      		//half _BarrelPower;
      		//half _BarrelLinear;

      		//half _BarrelPower2;
      		//half _BarrelPower4;
			VertexToFragment VertexProgram (VertexInput vertex)
			{
			    VertexToFragment output;

			    output.uv = vertex.texcoord;
			    output.position = undistort(vertex.position);
			    //output.position = mul(UNITY_MATRIX_MVP, undistort(vertex.position));
			    return output;



			    //half2 screenPos =  output.position.xy/output.position.w ;

			    	

    				//half r2 = saturate(dot(screenPos, screenPos));
    				//half r4 = saturate(r2*r2);
    				///this correction is using brown's distortion model
    				//screenPos *= ((1+_BarrelPower2*r2)/(1+_BarrelPower2)) * (1+_BarrelPower4*r4)/(1+_BarrelPower4);
    				//screenPos *= 1+(_Undistortion.x*r2) + (_Undistortion.y*r4);




    			//output.position.xy = screenPos* output.position.w;

    			//return output;



			    
			   
			};


			//half4 _Color;
			sampler2D _MainTex;
			half4 FragmentProgram (VertexToFragment fragment) : COLOR
			{  
				//return abs(half4(fragment.screenPos.xy, 0,1));
				return tex2D(_MainTex, fragment.uv);
				//return _Color;
			}
			ENDCG
		}
	}
	
}
}
