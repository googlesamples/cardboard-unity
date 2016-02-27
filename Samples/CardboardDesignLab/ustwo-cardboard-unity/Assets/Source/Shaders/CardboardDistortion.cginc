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


// To use in a surface shader, add the following text to the code:
//
// #pragma surface ... vertex:vert         <-- add "vertex:vert" to this line
// #include "CardboardDistortion.cginc"    <-- copy the next 4 lines
// void vert (inout appdata_base v) {
//   v.vertex = undistort(v.vertex);
// }

// To use in a vertex shader, modify it as follows:
//
// #include "CardboardDistortion.cginc"  <-- add
//
// v2f vert (appdata_blah v) {
//   v2f o;
//   o.vertex = mul(UNITY_MATRIX_MVP, undistort(v.vertex));    <-- call undistort() here
//   ...
//   return o;
// }

float4    _Undistortion;
float     _MaxRadSq;
float4x4  _RealProjection;

float4 undistort(float4 pos) {

    
    #if SHADER_API_GLES || SHADER_API_GLES3
    pos = mul(UNITY_MATRIX_MV, pos);
    float near = -(UNITY_MATRIX_P[2][2] + 1) / UNITY_MATRIX_P[3][2];
    if (pos.z < near) {
        float r2 = clamp(dot(pos.xy, pos.xy) / (pos.z*pos.z), 0, _MaxRadSq);
        pos.xy *= 1 + (_Undistortion.x + _Undistortion.y*r2)*r2;
    }
    #else
 
        return mul(UNITY_MATRIX_MVP, pos);
    #endif
    //return mul(UNITY_MATRIX_P, pos);
    return mul(_RealProjection, pos);
    
    //return mul(transpose(UNITY_MATRIX_IT_MV), pos);
}
