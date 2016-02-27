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

#ifndef CYLINDERMAPPING_CGINC
#define CYLINDERMAPPING_CGINC

float Cylinder_Depth;
float Cylinder_Angle;
float Cylinder_Radius;

float4 MapCoordinate(float4 coord)
{
	float theta = (coord.x / _ScreenParams.x) * Cylinder_Angle;
	float radius = Cylinder_Radius * _ScreenParams.x;
	float depth = Cylinder_Depth * _ScreenParams.x;
	
	coord.x = sin(theta) * radius;
	coord.z = (cos(theta) * radius) + depth;
	
	return coord;
}

#endif
