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

public class Blackout : MonoBehaviour {

	static Blackout instance;
	public static Blackout Instance{
		get{ return instance;}
	}

	[SerializeField] Material blackoutMaterial;
	[SerializeField] MeshRenderer meshRenderer;

	[SerializeField] float smoothPower = 0.25f;

	[SerializeField] float targetBlackoutValue = 0;
	float blackoutSpeed =0;
	float currentBlackoutValue =0;

	float epsilon = 0.005f;

	Color color;
	public float TargetBlackoutValue{
		get{
			return targetBlackoutValue;
		}
		set{
			targetBlackoutValue = value;

			//hacking this in
			SetFogFade(false);
		}
	}
	public float BlackoutValue{
		get{
			return currentBlackoutValue;
		}

	}



	public void SetFogFade(bool fogFade){
		if(fogFade){
			blackoutMaterial.SetFloat("_Environment",1);
		}
		else{
			blackoutMaterial.SetFloat("_Environment",0);
		}
		
	}


	public void SetColor(Color c){
		color = c;
	}


	void Awake(){
		instance = this;
		targetBlackoutValue = currentBlackoutValue = blackoutMaterial.color.a;

		SetMeshRendererVisibility();
	}


	void Update(){

		//We don't want to be setting the material values every frame.
		if(currentBlackoutValue != targetBlackoutValue){
			currentBlackoutValue = Smoothing.SpringSmooth(currentBlackoutValue, targetBlackoutValue, ref blackoutSpeed, smoothPower, Time.deltaTime);

			if(Mathf.Abs(currentBlackoutValue - targetBlackoutValue) < epsilon){
				currentBlackoutValue = targetBlackoutValue;
				blackoutSpeed = 0;
			}

			SetMeshRendererVisibility();

			blackoutMaterial.color = new Color(color.r, color.g, color.b,currentBlackoutValue);
		}
	}

	///This is an optimization so we aren't drawing a full screen pass of nothing 
	void SetMeshRendererVisibility(){
		if(currentBlackoutValue == 0){
			meshRenderer.enabled = false;
		}
		else if(meshRenderer.enabled == false){
			meshRenderer.enabled = true;
		}
	}



}
