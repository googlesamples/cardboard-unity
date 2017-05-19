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

Shader "Cardboard/Radial Undistortion" {

  Properties {
    _MainTex ("Base (RGB)", 2D) = "white" {}
  }

  SubShader {
    Tags { "RenderType" = "Opaque" }
    LOD 150

    Pass {
      CGPROGRAM
      #include "UnityCG.cginc"
      #pragma vertex vert
      #pragma fragment frag

      struct v2f {
        float4 vertex  : SV_POSITION;
        half2 texcoord : TEXCOORD0;
      };

      sampler2D _MainTex;
      float4    _MainTex_ST;

      float4 _Distortion;   // Radial distortion coefficients.
      float4 _Projection;   // Bits of the lens-affected projection matrix.
      float4 _Unprojection; // Bits of the no-lens projection matrix.

      v2f vert (appdata_base v) {
        // This is totally standard code, including the _MainTex transform.
        v2f o;
        o.vertex = mul(UNITY_MATRIX_MVP, v.vertex);
        o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
        return o;
      }

      fixed4 frag (v2f i) : SV_Target {
        // i.texcoord refers to the onscreen pixel we want to color.  We have to find the
        // corresponding pixel in the rendered texture, which means going through the lens'
        // distortion transformation.
        // First: go from these onscreen texture coordinates back into camera space, using parts
        // of the projection matrix for the "no lens" frustum.  (We don't need the whole matrix
        // because we don't care about Z.)
        float2 tex = (i.texcoord + _Unprojection.zw) / _Unprojection.xy;
        // Second: Radially distort the vector according to the lens' coefficients (pincushion).
        float r2 = dot(tex, tex);
        tex *= 1 + (_Distortion.x + _Distortion.y * r2) * r2;
        // Reproject the vector into the lens-affected frustum to get the texture coordinates
        // to look up.
        tex = tex * _Projection.xy - _Projection.zw;
        // Get the color.  But the coordinates may be out of bounds, so check for that.
        fixed4 col = all(tex >= 0) && all(tex <= 1) ? tex2D(_MainTex, tex) : fixed4(0,0,0,1);
        return col;
      }
      ENDCG
    }
  }

}
