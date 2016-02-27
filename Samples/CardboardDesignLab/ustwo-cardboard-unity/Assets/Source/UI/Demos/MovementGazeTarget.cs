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
using System.Collections.Generic;

public class MovementGazeTarget : GazeTarget{

	//private new Transform transform;

	[SerializeField] MeshRenderer meshRenderer;
	[SerializeField] new Collider collider;
	[SerializeField] Color color;
	[SerializeField] AmbientAudioTarget ambientTrack1;
	[SerializeField] AmbientAudioTarget ambientTrack2;

	[SerializeField] MeshRenderer floorGlow;
	[SerializeField] Transform floorTransform;

	float alpha;
	float alphaSpeed;
	float targetAlpha;

	[SerializeField] float movementSpeed = 1;

	[SerializeField] float timeOfDay = 0;

	[SerializeField] float defaultOrientation = 0;
	public float DefaultOrientation{
		get{
			return defaultOrientation;
		}
	}

	public AmbientAudioTarget AmbientTrack1{
		get{
			return ambientTrack1;
		}
	}
	public AmbientAudioTarget AmbientTrack2{
		get{
			return ambientTrack2;
		}
	}
	public float TimeOfDay{
		get{
			return timeOfDay;
		}
	}

	public float MovementSpeed{
		get{
			return movementSpeed;
		}
	}

	protected void Start(){
		if(PlayerMover.Instance.CurrentTarget == this){
			collider.enabled = false;
			alpha = 0;
			alphaSpeed = 0;
			color.a = alpha;
			if (meshRenderer && meshRenderer.sharedMaterial) {
				meshRenderer.sharedMaterial.color = color;
			}
		}
		else{
			//collider.enabled = true;
			alpha = 1;
			alphaSpeed = 0;
			color.a = alpha;
			if (meshRenderer && meshRenderer.sharedMaterial) {
				meshRenderer.sharedMaterial.color = color;
			}
		}
		if(floorGlow != null){
			Color fC = floorGlow.material.color;
			fC.a = alpha;
			floorGlow.material.color = fC;
		}
	}

	public void Hide(bool immediate = false){
		collider.enabled = false;

		if(immediate){
			targetAlpha = alpha = color.a = 0;
			meshRenderer.material.color = color;
			meshRenderer.enabled = false;
			if(floorGlow != null){
				floorGlow.enabled = false;
				Color fC = floorGlow.material.color;
				fC.a = alpha;
				floorGlow.material.color = fC;
			}
		}
		else{
			targetAlpha = 0;
		}
	}

	public void Show(bool immediate = false){
		collider.enabled = true;
		if(immediate){
			targetAlpha = alpha = color.a = 1;
			meshRenderer.material.color = color;
			meshRenderer.enabled = true;

			if(floorGlow != null){
				floorGlow.enabled = true;
				Color fC = floorGlow.material.color;
				fC.a = alpha;
				floorGlow.material.color = fC;
			}
		}
		else{
			targetAlpha = 1;
		}
	}	


	protected override void DoUpdate(){
		base.DoUpdate();

		if(alpha != targetAlpha){

			alpha = Smoothing.SpringSmooth(alpha, targetAlpha, ref alphaSpeed, 0.5f, Time.deltaTime);
			if(Mathf.Abs(targetAlpha - alpha) < 0.005f){
				alpha = targetAlpha;
			}
			if(alpha == 0){
				meshRenderer.enabled = false;
				if(floorGlow != null){
					floorGlow.enabled = false;
				}
			}
			else{
				meshRenderer.enabled = true;
				if(floorGlow != null){
					floorGlow.enabled = true;
				}
			}
			color.a = alpha;
			meshRenderer.material.color = color;
			if(floorGlow != null){
				Color fC = floorGlow.material.color;
				fC.a = alpha;
				floorGlow.material.color = fC;
			}
		}

		if(alpha != 0){
			if(floorTransform != null){
				floorTransform.LookAt(PlayerMover.Instance.Position);
			}
		}
		/*
		if(PlayerMover.Instance.CurrentTarget == this){


			collider.enabled = false;

			if(alpha > 0){
				alpha = 1-2f*PlayerMover.Instance.SmoothedBlend;
				color.a = alpha;
				meshRenderer.material.color = color;
			}

		}
		else{
			
			float dist = (PlayerMover.Instance.Position - transform.position).magnitude;
			if(dist > 3){
				collider.enabled = true;
				if(alpha < 0.99f){
					alpha = Smoothing.SpringSmooth(alpha, 1, ref alphaSpeed, 0.5f, Time.deltaTime);
					color.a = alpha;
					meshRenderer.material.color = color;

				}
				else if(alpha < 1){
					alpha = 1;
					alphaSpeed = 0;
					color.a = alpha;
					meshRenderer.material.color = color;
				}
			}
		}
		*/

	}

	void Awake(){

	}

	protected override void OnGazeEnter(){

	}


	protected override void OnGaze(float gazeDuration){
		//Debug.Log("gaze");
	}


	protected override void OnGazeExit(float gazeDuration){

	}

	protected override void OnButtonDown(VRButton button){
		//Debug.Log("click");
		//PlayerMover.Instance.SetTargetPosition(transform.position, this);

	}
	protected override void OnButtonUp(VRButton button){
		
	}
	protected override void OnButtonHeld(VRButton button){
		
	}



}
