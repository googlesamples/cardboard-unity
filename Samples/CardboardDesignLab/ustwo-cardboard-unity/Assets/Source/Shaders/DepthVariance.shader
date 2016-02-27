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

Shader "UsTwo-Cardboard/Depth Variance" {

Properties {
	//_Color ("Main Color", Color) = (1,1,1,1)
	//_MainTex ("Main Texture", 2D) = "" {}
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

			struct VertexInput {
			    float4 position : POSITION;
			};

			struct VertexToFragment {
			    float4 position : SV_POSITION;
			   float4 pos : TEXCOORD0;
			};


			VertexToFragment VertexProgram (VertexInput vertex)
			{
			    VertexToFragment output;
			   // output.worldPosition = mul (_Object2World, vertex.position).xyz;
			    output.position = mul (UNITY_MATRIX_MVP, vertex.position);
			    output.pos = output.position/output.position.w;
			    return output;
			};

			// Encoding/decoding [0..1) floats into 8 bit/channel RGBA. Note that 1.0 will not be encoded properly.
			inline float4 EncodeFloatRGBA( float v )
			{
				float4 kEncodeMul = float4(1.0, 255.0, 65025.0, 16581375.0);
				float kEncodeBit = 1.0/255.0;
				float4 enc = kEncodeMul * v;
				enc = frac (enc);
				enc -= enc.yzww * kEncodeBit;
				return enc;
			}
			float4 FragmentProgram (VertexToFragment fragment) : COLOR
			{  
				//float depth = 1.0- saturate( length(fragment.worldPosition - _WorldSpaceCameraPos)/100.0);

				float dist = saturate(100*(max(abs(fragment.pos.x), abs(fragment.pos.y))-0.99));
				return EncodeFloatRGBA(fragment.pos.z)+dist;

				/*
				//return depth;
				const float toFixed = 255.0/256.0;
				fixed4 output;
				output.r = (depth*toFixed*1);
				output.g = frac(depth*toFixed*255);
				output.b = frac(depth*toFixed*255*255);
				output.a = frac(depth*toFixed*255*255*255);
				*/
				//const float fromFixed = 256.0/255.0;
				//float4 val = output*fromFixed;
				//float value = val.r +val.g/255.0 + val.b/(255.0*255.0) + val.a/(255.0*255.0*255.0);

				//return output;
			}
			ENDCG
		}
	}
	
}
}
