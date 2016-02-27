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

public class FixedMenu : MonoBehaviour {



	private new Transform transform;
	float currentYaw = 0;
	public float maxYaw = 15;
	float previousYaw;

	public bool lockPitch = false;
	public float pitch = 0;

	public bool counterRotateUI = false;
	public Transform UI;


	public float Pitch{
		get{
			return pitch;
		}
		set{
			pitch = value;
		}
	}

	void Start(){
		transform = GetComponent<Transform>();
		previousYaw = VRInput.Instance.Yaw;
	}

	public void Reset(){
		previousYaw = VRInput.Instance.Yaw;
		currentYaw = 0;
	}
	void LateUpdate () {
		float deviceYaw = VRInput.Instance.Yaw;
		float devicePitch = VRInput.Instance.Pitch;
		
		float deltaYaw = Mathf.DeltaAngle(deviceYaw, previousYaw);

		float targetYaw = currentYaw + deltaYaw;
		
		currentYaw = Mathf.Clamp(targetYaw, -maxYaw,maxYaw);
		
		transform.position = VRInput.Instance.Position;


		previousYaw = deviceYaw;
		Quaternion menuPitch = Quaternion.AngleAxis(pitch, Vector3.right);
		Quaternion deviceRotation = Quaternion.AngleAxis(deviceYaw, Vector3.up);
		Quaternion devicePitchRotation = Quaternion.AngleAxis(-devicePitch, Vector3.right);
		///TODO: try an ease on this to smooth out the edges

		if(!lockPitch){
			transform.localRotation = deviceRotation *Quaternion.AngleAxis( currentYaw, Vector3.up)*menuPitch;
		}
		else{
			transform.localRotation = deviceRotation * Quaternion.AngleAxis( currentYaw, Vector3.up)*devicePitchRotation *menuPitch;
		}

		if(counterRotateUI){
			UI.localRotation = Quaternion.AngleAxis(-pitch, Vector3.right);
		}
	}
}
