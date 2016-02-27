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

/// <summary>
/// The splash creen...
/// </summary>
public class SplashScene : DemoSceneState {




	public override void EnterState(SceneManager context, int idx = 0){
		base.EnterState(context);
		Blackout.Instance.SetColor(Color.white);
		VRInput.Instance.ReticleColor = new Color(0,0,0,0);
		VRInput.Instance.MaxReticleDistance = 5;

	}

	public override void ExitState(SceneManager context){

		base.ExitState(context);
	}

	public override void DoUpdate(SceneManager context){
		
		if(VRInput.Instance.PrimaryButton.Pressed){
			//StopTracking();
			SceneManager.Instance.StateTransition(SceneManager.Instance.menuScene);
		}
		
		/// Reorient the scene if the user looks up or down 35 degrees.   A clever trick to ensure the user is usually facing forward when the bring cardboard up to their face.
		if( Mathf.Abs(Mathf.DeltaAngle(0, VRInput.Instance.Pitch)) > 35){
			
			SceneManager.Instance.StateTransition(SceneManager.Instance.splashScene);
		}
	}



}
