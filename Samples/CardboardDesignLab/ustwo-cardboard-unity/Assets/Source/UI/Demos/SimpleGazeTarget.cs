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

public class SimpleGazeTarget : GazeTarget{

	//private new Transform transform;

	//float scale = 1;
	//float scaleTarget = 1;
	//float scaleSpeed = 0;

	//Vector3 baseScale;

	void Awake(){
		//transform = GetComponent<Transform>();
		//baseScale = transform.localScale;
	}

	protected override void OnGazeEnter(){
		//scaleTarget = 1.1f;
	}


	protected override void OnGaze(float gazeDuration){
		
	}


	protected override void OnGazeExit(float gazeDuration){
		//scaleTarget = 1;
	}

	protected override void OnButtonDown(VRButton button){
		//scaleTarget = 1.2f;

	}
	protected override void OnButtonUp(VRButton button){
		
	}
	protected override void OnButtonHeld(VRButton button){
		
	}

	protected override void DoUpdate(){
		base.DoUpdate();
		//scale = Smoothing.SpringSmooth(scale, scaleTarget, ref scaleSpeed, 0.25f, Time.deltaTime);
		//transform.localScale = scale*baseScale;
	}

}
