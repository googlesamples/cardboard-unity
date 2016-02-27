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

using UnityEngine;
using System.Collections;

public class TimeOfDay : MonoBehaviour {

	[SerializeField] TimeOfDayLighting[] times;
	TimeSetting currentTime = new TimeSetting();


	public TimeSetting CalculateCurrentTime(float value){
		float time = Mathf.Clamp01(value / (float)times.Length);




		
		return CalculateCurrentNormalizedTime(time);
	}
	public TimeSetting CalculateCurrentNormalizedTime(float value){
		float time = Mathf.Clamp01(value);

		///don't subtract one so we can loop back around
		int segmentCount = times.Length;
		float totalTime = time*segmentCount;
		int idx = ( (int) totalTime)%times.Length;
		int nextIdx = (idx + 1)%times.Length;

		float blend = totalTime - ((int) totalTime);

		TimeSetting start = times[idx].TimeSetting;
		TimeSetting end = times[nextIdx].TimeSetting;

		currentTime.horizonColor = (1.0f-blend)*start.horizonColor + blend*end.horizonColor;
		currentTime.horizonFog = (1.0f-blend)*start.horizonFog + blend*end.horizonFog;
		currentTime.zenithColor = (1.0f-blend)*start.zenithColor + blend*end.zenithColor;
		currentTime.zenithFog = (1.0f-blend)*start.zenithFog + blend*end.zenithFog;

		currentTime.lightColor = (1.0f-blend)*start.lightColor + blend*end.lightColor;
		currentTime.shadowColor = (1.0f-blend)*start.shadowColor + blend*end.shadowColor;
		currentTime.ambientColor = (1.0f-blend)*start.ambientColor + blend*end.ambientColor;

		currentTime.lightIntensity = (1.0f-blend)*start.lightIntensity + blend*end.lightIntensity;
		currentTime.ambientIntensity = (1.0f-blend)*start.ambientIntensity + blend*end.ambientIntensity;
		currentTime.rimIntensity = (1.0f-blend)*start.rimIntensity + blend*end.rimIntensity;
		
		///let's only reset this once, plz, k thx...
		if(blend == 0){
			currentTime.farClipPlane = start.farClipPlane;
		}
		else if (blend ==1){
			currentTime.farClipPlane = end.farClipPlane;
		}
		else{
			currentTime.farClipPlane = Mathf.Max(start.farClipPlane, end.farClipPlane);//(1.0f-blend)*start.farClipPlane + blend*end.farClipPlane;
		}

		currentTime.zenithFogDistance = (1.0f-blend)*start.zenithFogDistance + blend*end.zenithFogDistance;
		currentTime.zenithFogPoint = (1.0f-blend)*start.zenithFogPoint + blend*end.zenithFogPoint;
		currentTime.zenithFogBlendDistance = (1.0f-blend)*start.zenithFogBlendDistance + blend*end.zenithFogBlendDistance;

		currentTime.horizonFogDistance = (1.0f-blend)*start.horizonFogDistance + blend*end.horizonFogDistance;
		currentTime.horizonFogPoint = (1.0f-blend)*start.horizonFogPoint + blend*end.horizonFogPoint;
		currentTime.horizonFogBlendDistance = (1.0f-blend)*start.horizonFogBlendDistance + blend*end.horizonFogBlendDistance;


		currentTime.directionalFogIntensity = (1.0f-blend)*start.directionalFogIntensity + blend*end.directionalFogIntensity;
		currentTime.directionalFogAngle = (1.0f-blend)*start.directionalFogAngle + blend*end.directionalFogAngle;
		currentTime.directionalFogDistance = (1.0f-blend)*start.directionalFogDistance + blend*end.directionalFogDistance;
		currentTime.directionalFogPoint = (1.0f-blend)*start.directionalFogPoint + blend*end.directionalFogPoint;
		currentTime.directionalFogBlendDistance = (1.0f-blend)*start.directionalFogBlendDistance + blend*end.directionalFogBlendDistance;

		currentTime.directionalColor = (1.0f-blend)*start.directionalColor + blend*end.directionalColor;
		currentTime.directionalFog = (1.0f-blend)*start.directionalFog + blend*end.directionalFog;

		currentTime.starIntensity = (1.0f-blend)*start.starIntensity + blend*end.starIntensity;

		currentTime.yaw = (1.0f-blend)*start.yaw + blend*end.yaw;
		currentTime.pitch = (1.0f-blend)*start.pitch + blend*end.pitch;

		currentTime.offset = (1.0f-blend)*start.offset + blend*end.offset;
		currentTime.shadowFieldOfView = (1.0f-blend)*start.shadowFieldOfView + blend*end.shadowFieldOfView;

		currentTime.pointLightPosition = (1.0f-blend)*start.pointLightPosition + blend*end.pointLightPosition;
		currentTime.pointLightColor = (1.0f-blend)*start.pointLightColor + blend*end.pointLightColor;
		currentTime.pointLightRadius = (1.0f-blend)*start.pointLightRadius + blend*end.pointLightRadius;

		currentTime.shadowNearClip = (1.0f-blend)*start.shadowNearClip + blend*end.shadowNearClip;
		currentTime.shadowFarClip = (1.0f-blend)*start.shadowFarClip + blend*end.shadowFarClip;
		
		return currentTime;
	}



}
	[System.Serializable]
	public struct TimeSetting{

		public Color lightColor;
		public Color shadowColor;
		public Color ambientColor;

		[Range (0,16)]
		public float lightIntensity;
		[Range (0,16)]
		public float ambientIntensity;
		[Range (0,16)]
		public float rimIntensity;

		public Color horizonColor;
		public Color horizonFog;

		public Color zenithColor;
		public Color zenithFog;

		public float farClipPlane;

		public float zenithFogDistance;
		public float zenithFogPoint;
		public float zenithFogBlendDistance;

		public float horizonFogDistance;
		public float horizonFogPoint;
		public float horizonFogBlendDistance;

		[Range (0,1)]
		public float directionalFogIntensity;
		public float directionalFogAngle;
		public float directionalFogDistance;
		public float directionalFogPoint;
		public float directionalFogBlendDistance;

		public Color directionalColor;
		public Color directionalFog;

		[Range (0,1)]
		public float starIntensity;

		public float yaw;
		public float pitch;

		public Vector3 offset;
		public float shadowFieldOfView;

		public Vector3 pointLightPosition;
		public float pointLightRadius;
		public Color pointLightColor;

		public float shadowNearClip;
		public float shadowFarClip;
	}

