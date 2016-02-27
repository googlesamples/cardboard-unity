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



///This becomes the target
public class Reticle3D : MonoBehaviour {


	float currentFocalDepth = 5;
	float targetFocalDepth = 5;
	float focalDepthSpeed = 0;


	bool visualDisplay;

	public MeshRenderer meshRenderer;

	Color targetColor = Color.white;
	Color currentColor = Color.white;
	Vector4 colorSpeed;

	float minReticleDistance = 0.75f;
	float maxReticleDistance = 5;


	//float timeSinceClick


	public void SetColor(Color c){
		//Debug.Log("setcolor" + c);
		targetColor = c;
	}
	public void SetAndApplyColor(Color c){
		//Debug.Log("setapplycolor" + c);
		currentColor = c;
		meshRenderer.material.color = currentColor;

	}

	void Start(){
		meshRenderer.material.color = currentColor;
	}

	public float MaxReticleDistance{
		get{
			return maxReticleDistance;
		}
		set{
			maxReticleDistance = value;
		}
	}

	void Update(){
		if(currentColor != targetColor){
			currentColor.r = Smoothing.SpringSmooth(currentColor.r, targetColor.r, ref colorSpeed.x, 0.1f, Time.deltaTime);
			currentColor.g = Smoothing.SpringSmooth(currentColor.g, targetColor.g, ref colorSpeed.y, 0.1f, Time.deltaTime);
			currentColor.b = Smoothing.SpringSmooth(currentColor.b, targetColor.b, ref colorSpeed.z, 0.1f, Time.deltaTime);
			currentColor.a = Smoothing.SpringSmooth(currentColor.a, targetColor.a, ref colorSpeed.w, 0.1f, Time.deltaTime);
			meshRenderer.material.color = currentColor;
		}
	}

	public void SetTarget(Vector3 worldPosition){
		Vector3 localPosition = transform.parent.InverseTransformPoint(worldPosition);
		targetFocalDepth = Mathf.Clamp(localPosition.z,minReticleDistance,maxReticleDistance);
		//Debug.Log(worldPosition + ":" + localPosition);
	}
	public void SetDistance(float distance){
		
		targetFocalDepth = Mathf.Clamp(distance,minReticleDistance,maxReticleDistance);
	}
	public void UpdateReticlePosition(){

		float springSpeed = 0.05f;
		if(targetFocalDepth < currentFocalDepth){
			springSpeed = 0.05f;
		}
		else{
			springSpeed = .5f;
		}

		currentFocalDepth = Smoothing.SpringSmooth(currentFocalDepth, targetFocalDepth, ref focalDepthSpeed, springSpeed, Time.deltaTime);
		//currentFocalDepth = targetFocalDepth;
		transform.localPosition = new Vector3(0,0, currentFocalDepth);
	}

}
