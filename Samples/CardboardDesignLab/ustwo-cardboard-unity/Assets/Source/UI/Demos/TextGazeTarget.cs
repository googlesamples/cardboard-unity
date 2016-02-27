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

public class TextGazeTarget : GazeTarget{

	[SerializeField] GameObject text;

	void Awake(){
		//initialScale = frame.localScale;
		///material.color = new Color(1,1,1, current);
		//frame.localScale = (1f +0.2f* current)* initialScale;
		text.SetActive( false );
	}

	protected override void OnGazeEnter(){
		//target = 1;
		text.SetActive( true);

	}


	protected override void OnGaze(float gazeDuration){
		
	}


	protected override void OnGazeExit(float gazeDuration){
		//target = 0;
		text.SetActive( false);
	}

	protected override void OnButtonDown(VRButton button){


	}
	protected override void OnButtonUp(VRButton button){
		
	}
	protected override void OnButtonHeld(VRButton button){
		
	}

	//protected override void LateUpdate(){
		/*base.LateUpdate();
		if(current != target){
			current = Smoothing.SpringSmooth(current, target, ref speed, 0.1f, Time.deltaTime);
			material.color = new Color(1,1,1, current);
			frame.localScale = (1f +0.2f* current)* initialScale;
		}*/
	//}

}
