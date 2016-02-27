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

public class Toaster : MonoBehaviour {

	static Toaster instance;
	public static Toaster Instance{
		get{
			return instance;
		}
	}

	public float startPitch;
	public float endPitch;

	float currentPitch;

	public float toastTime = 1;


	public FixedMenu menu;
	public TextMesh textMesh;
	public MeshRenderer backplate;
	public MeshRenderer arrow;
	public Transform arrowTransform;
	Vector3 initialArrowPosition;

	public float frequency=1;
	public float amplitude = 1;

	float val;
	float valSpeed = 0;


	float time = 0;

	void Start(){
		time = toastTime;
		menu.Pitch = currentPitch = startPitch;
		Color c = textMesh.color;
		c.a = 0;
		textMesh.color = c;
		val = 0;

		c = backplate.material.color;
		c.a = 0;
		backplate.material.color = c;

		c = arrow.material.color;
		c.a = 0;
		arrow.material.color = c;
		initialArrowPosition = arrowTransform.localPosition;
	}

	void Awake(){
		instance = this;
	}

	public void Toast(){
		UISounds.Instance.PlayComplete();
		menu.Pitch = currentPitch = startPitch;
		Color c = textMesh.color;
		c.a = 0;
		textMesh.color = c;
		val = 0;
		time = 0;


		c = backplate.material.color;
		c.a = 0;
		backplate.material.color = c;

		c = arrow.material.color;
		c.a = 0;
		arrow.material.color = c;
	}
	public void ClearToast(){
		if(time < toastTime){
			time = toastTime;
		}
	}
	void Update(){
		// if(Input.GetMouseButtonDown(0)){
		// 	Toast();
		// }
		time += Time.deltaTime;

		arrowTransform.localPosition = initialArrowPosition + new Vector3(0,amplitude*Mathf.Sin(frequency*Time.time),0);
		if(time < toastTime){
			val = Smoothing.SpringSmooth(val, 1, ref valSpeed, 0.25f, Time.deltaTime);
			Color c = textMesh.color;
			c.a = val;
			textMesh.color = c;
			c = backplate.material.color;
			c.a = val;
			backplate.material.color = c;

			c = arrow.material.color;
			c.a = val;
			arrow.material.color = c;
			currentPitch = (1-val)*startPitch + val * endPitch;
			menu.Pitch = currentPitch;
		}
		else{
			if(val != 0){
				val = Smoothing.SpringSmooth(val, 0, ref valSpeed, 0.1f, Time.deltaTime);
				if(Mathf.Abs(val) < 0.01f){
					val = 0;
				}
				Color c = textMesh.color;
				c.a = val;
				textMesh.color = c;

				c = backplate.material.color;
				c.a = val;
				backplate.material.color = c;

				c = arrow.material.color;
				c.a = val;
				arrow.material.color = c;
			}
		}


	}

}
