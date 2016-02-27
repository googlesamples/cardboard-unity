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

public class LookDownMenu : MonoBehaviour {

	[SerializeField] LookDownElement[] lookDownElements;

	

	float targetMenuAlpha;
	float currentMenuAlpha;
	float alphaMenuSpeed;
	FixedMenu menu;

	public void HideNext(){

	}
	public void ShowNext(){

	}

	void Start(){
		for(int i=0; i<lookDownElements.Length; i++){
			LookDownElement el = lookDownElements[i];
			Color c = el.text.color;
			c.a = el.currentAlpha * currentMenuAlpha;
			el.text.color = c;
			el.initialScale = el.iconTransform.localScale;
			el.iconTransform.localScale = el.initialScale + 0.2f*el.currentAlpha* el.initialScale;

			Color col = el.plane.material.color;
			col.a = currentMenuAlpha;
			el.plane.material.color = col;
		}

		menu = GetComponent<FixedMenu>();
	}

	void Update(){

		if(VRInput.Instance != null){
			if(VRInput.Instance.Pitch < -15){
				if(Toaster.Instance != null){
					Toaster.Instance.ClearToast();
				}
			}
			if(VRInput.Instance.Pitch < -22){
				targetMenuAlpha = 1;
				if(Toaster.Instance != null){
					Toaster.Instance.ClearToast();
				}
			}
			else{
				targetMenuAlpha = 0;

			}
		}

		bool updateAll = false;
		if(currentMenuAlpha != targetMenuAlpha){
			float menuSpringSpeed = 0.2f;
			if(targetMenuAlpha < currentMenuAlpha){
				menuSpringSpeed = 0.1f;
			}

			currentMenuAlpha = Smoothing.SpringSmooth(currentMenuAlpha, targetMenuAlpha, ref alphaMenuSpeed, menuSpringSpeed, Time.deltaTime);
			if(Mathf.Abs(currentMenuAlpha - targetMenuAlpha) < 0.005f){
				currentMenuAlpha = targetMenuAlpha;
			}
			updateAll = true;
		}
		if(currentMenuAlpha ==0){
			menu.Reset();
		}

		for(int i=0; i<lookDownElements.Length; i++){
			LookDownElement el = lookDownElements[i];
			if(el.target.IsTarget){
				el.targetAlpha = 1;
			}
			else{
				el.targetAlpha = 0;
			}

			

			if(el.currentAlpha != el.targetAlpha || updateAll){

				float springSpeed = 0.2f;
				if(el.targetAlpha < el.currentAlpha){
					springSpeed = 0.1f;
				}


				el.currentAlpha = Smoothing.SpringSmooth(el.currentAlpha, el.targetAlpha, ref el.alphaSpeed, springSpeed, Time.deltaTime);
				if(Mathf.Abs(el.currentAlpha - el.targetAlpha) < 0.005f){
					el.currentAlpha = el.targetAlpha;
				}
				Color c = el.text.color;
				c.a = el.currentAlpha * currentMenuAlpha;
				el.text.color = c;

				el.iconTransform.localScale = el.initialScale + 0.2f*el.currentAlpha* el.initialScale;
				el.textTransform.localPosition = el.currentAlpha * el.endTextPosition + (1-el.currentAlpha)* el.initialTextPosition;
			}
			Color col = el.plane.material.color;
			col.a = currentMenuAlpha;
			el.plane.material.color = col;
		}

	}

	[System.Serializable]
	public class LookDownElement{
		public TextMesh text;
		public Transform textTransform;
		public GazeTarget target; 
		public MeshRenderer plane;
		public Transform iconTransform;
		public float targetAlpha;
		public float currentAlpha;
		public float alphaSpeed;
		public Vector3 initialScale;

		public readonly Vector3 initialTextPosition = new Vector3(0,0.19f, 0);
		public readonly Vector3 endTextPosition = new Vector3(0,0.23f, 0);



	}
}
