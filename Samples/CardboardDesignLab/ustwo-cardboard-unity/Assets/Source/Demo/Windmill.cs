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

public class Windmill : MonoBehaviour {

	public Transform blades;
	public AudioSource whoosh;

	float rotationSpeed = 30;
	public float RotationSpeed{
		get{
			return rotationSpeed;
		}
		set{
			if(whoosh == null){
				rotationSpeed = value;
			}
			else{
				rotationSpeed = initialRotationSpeed * value/30f;
			}
		}
	}


	float rotation;
	float initialRotationSpeed;

	float speedMultiplier = 1;

	void OnEnable(){

		if(whoosh != null){
			whoosh.loop = true;
			whoosh.Play();
			rotation = 5;
			initialRotationSpeed = rotationSpeed = 120f / whoosh.clip.length;
		}
		else{
			speedMultiplier = 1.5f + (Random.value-0.5f);
			rotation = Random.Range(0,180);
		}
		
	}

	//float lastAngle = 0;


	void Update () {
		rotation = (rotation + speedMultiplier*rotationSpeed*Time.deltaTime + 360f)%360f;

		/*if(whoosh != null){
		if( (rotation > 58 && (lastAngle <=58 || lastAngle >=298)) || (rotation > 178 && lastAngle <=178) || (rotation > 298 && lastAngle <=298)){
			whoosh.pitch = 0.65f + 0.1f*Random.value;
			whoosh.Play();
			lastAngle = rotation;
		}
		}*/

		blades.localRotation = Quaternion.AngleAxis(rotation, Vector3.right);
	}
}
