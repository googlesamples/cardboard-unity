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

[ExecuteInEditMode]
public class Lighting : MonoBehaviour {

	/*
	public Color LightColor;
	public Color ShadowColor;
	[Range (0,16)]
	public float lightIntensity;

	public Color AmbientColor;
	[Range (0,16)]
	public float ambientIntensity;
	[Range (0,16)]
	public float rimIntensity;

	public Color HorizonColor;
	public Color HorizonFog;

	public Color ZenithColor;
	public Color ZenithFog;


	[SerializeField] float farClipPlane = 200;



	public float zenithFogDistance;
	public float zenithFogPoint;
	public float zenithFogBlendDistance = 0.2f;

	public float horizonFogDistance;
	public float horizonFogPoint;
	public float horizonFogBlendDistance = 0.2f;

	*/


	public bool hideFogInEditor = true;

	[SerializeField] TimeSetting currentTime;
	public TimeSetting CurrentTime{
		get{
			return currentTime;
		}
		set{
			currentTime = value;

		}
	}
	public void SetTime(float time){
		currentTime = timeOfDay.CalculateCurrentTime(time);
	}


	public TimeOfDay timeOfDay;

	public enum LightingVariables{Primary, Secondary}
	[SerializeField] LightingVariables currentLighting  = LightingVariables.Primary;
	readonly string[] primaryLighting = new string[]{"_PrimaryLightDirection", "_PrimaryLightColor", "_PrimaryAmbientColor", "_ZenithColor", "_HorizonColor", "_HorizonFog", "_ZenithFog", "_FogData", "_ShadowColor", "_DirectionalData", "_DirectionalColor", "_DirectionalFog", "_PointLightPosition","_PointLightColor"};
	readonly string[] secondaryLighting = new string[]{"_PrimaryLightDirection2", "_PrimaryLightColor2", "_PrimaryAmbientColor2", "_ZenithColor2", "_HorizonColor2", "_HorizonFog2", "_ZenithFog2", "_FogData2", "_ShadowColor2", "_DirectionalData2", "_DirectionalColor", "_DirectionalFog", "_PointLightPosition2","_PointLightColor2"};

	[SerializeField] TimeOfDayLighting editorTimeOfDaySetting;


	string[] lightingNames;

	[SerializeField] bool animatedRotation = false;
	[SerializeField] Transform targetPoint = null;
	[SerializeField] float distance = 75;
	[SerializeField] ShadowMap shadows;

	public void CopyValues(){
		/*
		currentTime.lightColor = LightColor;
		currentTime.shadowColor = ShadowColor;
		currentTime.ambientColor = AmbientColor;


		currentTime.lightIntensity = lightIntensity;
		currentTime.ambientIntensity = ambientIntensity;
		currentTime.rimIntensity = rimIntensity;

		currentTime.horizonColor = HorizonColor;
		currentTime.horizonFog = HorizonFog;

		currentTime.zenithColor = ZenithColor;
		currentTime.zenithFog = ZenithFog;

		currentTime.farClipPlane = farClipPlane;

		currentTime.zenithFogDistance = zenithFogDistance;
		currentTime.zenithFogPoint = zenithFogPoint;
		currentTime.zenithFogBlendDistance = zenithFogBlendDistance;

		currentTime.horizonFogDistance = horizonFogDistance;
		currentTime.horizonFogPoint = horizonFogPoint;
		currentTime.horizonFogBlendDistance = horizonFogBlendDistance;
		*/
	}


	// void Start(){
	// 	lightingNames = primaryLighting;
	// 	if(currentLighting == LightingVariables.Secondary){
	// 		lightingNames = secondaryLighting;
	// 	}
	// }

	void OnEnable(){
		lightingNames = primaryLighting;
		if(currentLighting == LightingVariables.Secondary){
			lightingNames = secondaryLighting;
		}
		Update();
		if(shadows != null){
			shadows.SetNearFarClip(currentTime.shadowNearClip, currentTime.shadowFarClip);
			shadows.Recalculate();
		}
	}

	//void UpdateTime(){
	//	currentTime = timeOfDay.CalculateCurrentTime(Time.time*0.1f%1);
	//}

	float previousYaw;
	float previousPitch;
	Vector3 previousTarget;



	Color pointColor;

	void Update () {
		
		float worldYaw = 0;
		if(SceneManager.Instance != null){
			worldYaw = SceneManager.Instance.OrientationYaw;
		}

		if(animatedRotation){
			float yaw = currentTime.yaw;
			float pitch = currentTime.pitch;
			if(SceneManager.Instance != null){
				yaw += SceneManager.Instance.OrientationYaw;
			}
			Quaternion rotation = Quaternion.AngleAxis(yaw, Vector3.up) * Quaternion.AngleAxis(pitch, Vector3.right);
			
			if(shadows != null){
				shadows.SetFieldOfView(currentTime.shadowFieldOfView);
				shadows.SetNearFarClip(currentTime.shadowNearClip, currentTime.shadowFarClip);
			}

			if(shadows != null && (previousYaw != yaw || previousPitch != pitch) ){
				previousYaw = yaw;
				previousPitch = pitch;
				shadows.Recalculate();

				//Debug.Log("recalc");
			}
			transform.rotation = rotation;
			if(targetPoint != null){
				
				Vector3 targetPosition = targetPoint.position + Quaternion.AngleAxis(worldYaw, Vector3.up)*currentTime.offset - transform.forward * distance;
				if(targetPosition != previousTarget){
					previousTarget = targetPosition;
					if(shadows!=null){
						shadows.Recalculate();
					}
				}

				transform.position = targetPosition;
			}
		}


		float zenithDistance = currentTime.zenithFogDistance;
		float horizonDistance = currentTime.horizonFogDistance;
		float directionalDistance = currentTime.directionalFogDistance;
		float directionalFogIntensity = currentTime.directionalFogIntensity;
		#if UNITY_EDITOR
		if(hideFogInEditor){
				zenithDistance = 1000;
				horizonDistance = 1000;
				directionalDistance = 1000;
				directionalFogIntensity = 0;
			}
			if(!Application.isPlaying){
			
			


				if(editorTimeOfDaySetting != null){
					currentTime = editorTimeOfDaySetting.TimeSetting;
				}
			}
		#endif
		directionalDistance = Mathf.Clamp(directionalDistance, 1, 1000);

		if(StereoCamera.Instance != null && StereoCamera.Instance.FarClipPlane != currentTime.farClipPlane +3){
			Debug.Log("update clip");
			//StereoCamera.Instance.FarClipPlane = currentTime.farClipPlane+3;
			StereoCamera.Instance.EnvironmentScale = new Vector3(currentTime.farClipPlane, currentTime.farClipPlane, currentTime.farClipPlane);
		}


		//UpdateTime();
		Vector3 lightDir = -transform.forward;
		Shader.SetGlobalVector(lightingNames[0], new Vector4(lightDir.x, lightDir.y, lightDir.z, currentTime.rimIntensity));
		Shader.SetGlobalVector(lightingNames[1], new Vector4(currentTime.lightColor.r, currentTime.lightColor.g, currentTime.lightColor.b, currentTime.lightIntensity));

		Shader.SetGlobalVector(lightingNames[2], new Vector4(currentTime.ambientColor.r, currentTime.ambientColor.g, currentTime.ambientColor.b, currentTime.ambientIntensity)) ;




		Shader.SetGlobalVector(lightingNames[3], new Vector4(currentTime.zenithColor.r, currentTime.zenithColor.g, currentTime.zenithColor.b, 1f/(0.1f+zenithDistance) ) );
		Shader.SetGlobalVector(lightingNames[4], new Vector4(currentTime.horizonColor.r, currentTime.horizonColor.g, currentTime.horizonColor.b, 1f/(0.1f+horizonDistance) ) );


		Shader.SetGlobalVector(lightingNames[5], new Vector4(currentTime.horizonFog.r, currentTime.horizonFog.g, currentTime.horizonFog.b, currentTime.horizonFogPoint / (0.1f+currentTime.horizonFogDistance) ) );
		Shader.SetGlobalVector(lightingNames[6], new Vector4(currentTime.zenithFog.r, currentTime.zenithFog.g, currentTime.zenithFog.b, currentTime.zenithFogPoint / (0.1f+currentTime.zenithFogDistance) ));

		///_FogData
		Shader.SetGlobalVector(lightingNames[7], new Vector4(1f/(0.1f+currentTime.horizonFogBlendDistance/ (0.1f+currentTime.horizonFogDistance) ), 
		 1f/(0.1f+currentTime.zenithFogBlendDistance/ (0.1f+currentTime.zenithFogDistance) ),
		 1f/(0.1f+currentTime.directionalFogBlendDistance/ (0.1f+currentTime.directionalFogDistance) ),
		 currentTime.directionalFogPoint / (0.1f+currentTime.directionalFogDistance) ));

		Shader.SetGlobalVector(lightingNames[8], currentTime.shadowColor);

		float realAngle = 0;
		if(SceneManager.Instance != null){
			realAngle = Mathf.Deg2Rad*(SceneManager.Instance.OrientationYaw + currentTime.directionalFogAngle);
		}
		Shader.SetGlobalVector(lightingNames[9], new Vector4(directionalFogIntensity, 1f/(0.1f+directionalDistance) ,Mathf.Sin(realAngle),Mathf.Cos(realAngle)  ));
		Shader.SetGlobalVector(lightingNames[10], new Vector4(currentTime.directionalColor.r, currentTime.directionalColor.g, currentTime.directionalColor.b, 0) );
		Shader.SetGlobalVector(lightingNames[11], new Vector4(currentTime.directionalFog.r, currentTime.directionalFog.g, currentTime.directionalFog.b, 0) );


		Vector3 position = Quaternion.AngleAxis(worldYaw, Vector3.up)*currentTime.pointLightPosition;
		//_PointLightPosition
		Shader.SetGlobalVector(lightingNames[12], new Vector4(position.x, position.y, position.z, currentTime.pointLightRadius) );
		//_PointLightColor

		pointColor = pointColor + Time.deltaTime * (currentTime.pointLightColor - pointColor) + 3*Time.deltaTime*(Random.value-0.5f)*currentTime.pointLightColor;

		Shader.SetGlobalVector(lightingNames[13], new Vector4(pointColor.r, pointColor.g, pointColor.b, 0) );
		if(StarRendering.Instance != null){
			StarRendering.Instance.Color = new Color(currentTime.starIntensity,currentTime.starIntensity, currentTime.starIntensity, currentTime.starIntensity );
		}
		//Shader.SetGlobalVector("_FogBackColor", FogBackColor);

		//Shader.SetGlobalVector("_FogParameters", new Vector4(fogDistance, groundFogDensity, backFogDensity, fogEnabled));	
		//Vector3 fogD = forwardDirection.normalized;
		//Shader.SetGlobalVector("_FogForwardDirection", new Vector4(fogD.x, fogD.y, fogD.z, 0));
	}



	void OnDrawGizmos(){
		float worldYaw = 0;
		if(SceneManager.Instance != null){
			worldYaw = SceneManager.Instance.OrientationYaw;
		}
		Vector3 position = Quaternion.AngleAxis(worldYaw, Vector3.up)*currentTime.pointLightPosition;
		Gizmos.DrawWireSphere(position, currentTime.pointLightRadius);
	}

}
