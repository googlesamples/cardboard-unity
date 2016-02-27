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

public class SimpleSpringMenu : MonoBehaviour {



	private new Transform transform;
	float currentYaw = 0;
	public float minYaw = -15;
	public float maxYaw = 15;

	//float originalMinYaw;
	//float originalMaxYaw;

	float previousYaw;

	public bool lockPitch = false;
	public float pitch = 0;

	public bool counterRotateUI = false;
	public Transform UI;

	float yawSpeed = 0;
	float yawAcceleration = 0;
	public float springPower = 0.5f;
	public float maxYawSpeed = 5;
	public float buffer = 5;
	float targetYawSpeed;


	[SerializeField] Transform leftTransform;
	[SerializeField] Transform rightTransform;

	//float leftMenuYaw = -10;
	//float leftMenuYawSpeed;

	//float rightMenuYaw=10;
	//float rightMenuYawSpeed;

	public int Direction{
		get{
			if(currentYaw < -1){
				return -1;
			}
			else if(currentYaw > 1){
				return 1;
			}
			else{
				return 0;
			}
		}
	}


	void Start(){
		transform = GetComponent<Transform>();
		previousYaw = VRInput.Instance.Yaw;
		//originalMaxYaw = maxYaw;
		//originalMinYaw = minYaw;
	}

	void DepricatedLateUpdate () {
		float deviceYaw = VRInput.Instance.Yaw;
		
		float deltaYaw = Mathf.DeltaAngle(deviceYaw, previousYaw);

		float targetYaw = currentYaw + deltaYaw;
		currentYaw = targetYaw;
		if(targetYaw < minYaw){
			//currentYaw = targetYaw;
			/*if(deltaYaw < 0){
				float power = Mathf.Clamp01( Mathf.Abs(targetYaw - minYaw)/10f );
				currentYaw = (1-power)*targetYaw + (power)*currentYaw;//Smoothing.SpringSmooth(currentYaw, minYaw, ref yawSpeed, springPower, Time.deltaTime);
			}
			else{
				currentYaw = targetYaw;
			}*/
			float power = Mathf.Clamp01( Mathf.Abs(targetYaw - minYaw)/buffer );
			targetYawSpeed = maxYawSpeed * (power);

			
			//currentYaw = Smoothing.SpringSmooth(currentYaw, minYaw, ref yawSpeed, springPower, Time.deltaTime);
		}
		else if(targetYaw > maxYaw){
			//currentYaw = targetYaw;

			/*if(deltaYaw > 0){
				float power = Mathf.Clamp01( Mathf.Abs(targetYaw - maxYaw)/10f );
				power = Mathf.Sqrt(power);
				currentYaw = (1-power)*targetYaw + (power)*currentYaw;//Smoothing.SpringSmooth(currentYaw, minYaw, ref yawSpeed, springPower, Time.deltaTime);
			}
			else{
				currentYaw = targetYaw;
			}*/
			float power = Mathf.Clamp01( Mathf.Abs(targetYaw - maxYaw)/buffer );
			targetYawSpeed = -maxYawSpeed * (power);

			
			//currentYaw = Smoothing.SpringSmooth(currentYaw, maxYaw, ref yawSpeed, springPower, Time.deltaTime);
		}
		else{
			targetYawSpeed = 0;
			
			
		}
		
		yawSpeed = Smoothing.SpringSmooth(yawSpeed, targetYawSpeed, ref yawAcceleration, springPower, Time.deltaTime);
		currentYaw = currentYaw + yawSpeed *Time.deltaTime;
		//float targetLeftYaw = Mathf.Clamp(-currentYaw, menuMinYaw, menuMaxYaw);
		//leftMenuYaw = targetLeftYaw;//Smoothing.SpringSmooth(leftMenuYaw, targetLeftYaw, ref leftMenuYawSpeed, 0.25f, Time.deltaTime);

		//leftTransform.localRotation = Quaternion.AngleAxis(leftMenuYaw , Vector3.up);

		//float targetRightYaw = Mathf.Clamp(-currentYaw, -menuMaxYaw, -menuMinYaw);
		//rightMenuYaw = targetRightYaw;//Smoothing.SpringSmooth(rightMenuYaw, targetRightYaw, ref rightMenuYawSpeed, 0.25f, Time.deltaTime);

		//rightTransform.localRotation = Quaternion.AngleAxis(rightMenuYaw , Vector3.up);
		//currentYaw = Mathf.Clamp(targetYaw, -maxYaw,maxYaw);
		
		transform.position = VRInput.Instance.Position;


		previousYaw = deviceYaw;
		Quaternion menuPitch = Quaternion.AngleAxis(pitch, Vector3.right);
		Quaternion deviceRotation = Quaternion.AngleAxis(deviceYaw, Vector3.up);
		///TODO: try an ease on this to smooth out the edges

		if(!lockPitch){
			transform.localRotation = deviceRotation * Quaternion.AngleAxis( currentYaw, Vector3.up)*menuPitch;
		}
		else{
			transform.localRotation = VRInput.Instance.Rotation * Quaternion.AngleAxis( currentYaw, Vector3.up)*menuPitch;
		}

		if(counterRotateUI){
			UI.localRotation = Quaternion.AngleAxis(-pitch, Vector3.right);
		}
	}
}
