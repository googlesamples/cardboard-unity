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

public class ViewLimit : MonoBehaviour {



	private new Transform transform;
	float currentYaw = 0;
	public float maxYaw = 15;
	float previousYaw;

	void Start(){
		transform = GetComponent<Transform>();
		previousYaw = VRInput.Instance.Yaw;
	}

	void LateUpdate () {
		float deviceYaw = VRInput.Instance.Yaw;
		
		float deltaYaw = Mathf.DeltaAngle(deviceYaw, previousYaw);


		currentYaw = Mathf.Clamp(currentYaw + deltaYaw, -maxYaw,maxYaw);


		previousYaw = deviceYaw;

		
		
		///TODO: try an ease on this to smooth out the edges
		transform.localRotation = VRInput.Instance.Rotation * Quaternion.AngleAxis( currentYaw, Vector3.up);
	}
}
