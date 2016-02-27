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

public class TrackingScene : DemoSceneState {

	[SerializeField] GazeTarget menu; 
	[SerializeField] GazeTarget restart;
	[SerializeField] GazeTarget nextLab; 

	[SerializeField] AudioSource src; 

	public CardboardHead head;

	[SerializeField] Windmill[] windmills;
	public float windmillSpeed = 30;

	enum TrackingMode{On, Off}
	TrackingMode tracking = TrackingMode.On;

	float trackingTimer = 0;


	float windmillTargetSpeed;
	float windmillCurrentSpeed;
	float windmillAcceleration;

	[SerializeField] TextMesh warningText;
	[SerializeField] MeshRenderer warningBacking;

	float targetWarningAlpha;
	float currentWarningAlpha;
	float alphaSpeed;

	bool canStopHeadTracking = false;


	[SerializeField] AmbientAudioTarget ambientTrack = new AmbientAudioTarget();

	public float disabledTime = 3;
	public override void EnterState(SceneManager context, int idx = 0){
		base.EnterState(context);
		SoundManager.Instance.SetAmbientTrack(0, ambientTrack.data,ambientTrack.targetVolume );
		VRInput.Instance.ReticleColor = new Color(0,0,0,0);
		VRInput.Instance.MaxReticleDistance = 6;
		tracking = TrackingMode.On;
		windmillTargetSpeed = windmillCurrentSpeed = windmillSpeed;
		windmillAcceleration = 0;
		targetWarningAlpha = currentWarningAlpha = 0;
		StartCoroutine(AllowDisableHeadTracking(0.6f));

	}

	IEnumerator AllowDisableHeadTracking(float timer){
		yield return new WaitForSeconds(timer);
		canStopHeadTracking = true;
	}


	public override void ExitState(SceneManager context){
		head.trackRotation = true;
		base.ExitState(context);
	}

	public override void DoUpdate(SceneManager context){
		if((VRInput.Instance.Pitch) < -30){
				
			VRInput.Instance.ReticleColor = new Color(0,0,0,0.5f);
		}
		else{
			VRInput.Instance.ReticleColor = new Color(0,0,0,0);
		}
		if(menu.IsClicked){
			SceneManager.Instance.StateTransition(SceneManager.Instance.menuScene);
		}
		else if(restart.IsClicked){
			SceneManager.Instance.StateTransition(SceneManager.Instance.trackingScene);
		}
		else if(nextLab.IsClicked){
			SceneManager.Instance.StateTransition(SceneManager.Instance.redwoodScene, Color.black, -1);
		}
		else{
			if(VRInput.Instance.PrimaryButton.Pressed){
				StopTracking();
			}
		}
		
		

		if(tracking == TrackingMode.Off){
			targetWarningAlpha = 1;
			trackingTimer += Time.deltaTime;
			if(trackingTimer >disabledTime){
				head.trackRotation = true;
				tracking = TrackingMode.On;
				StartCoroutine(AllowDisableHeadTracking(1.5f));
				windmillTargetSpeed = windmillSpeed ;
				StartCoroutine(Popup());
			}
			src.volume += Time.deltaTime;
		}
		else{
			targetWarningAlpha = 0;
			if(src.volume > 0){
				src.volume -= 2f*Time.deltaTime;
			}
			else{
				src.Stop();
			}
		}

		currentWarningAlpha = Smoothing.SpringSmooth(currentWarningAlpha, targetWarningAlpha, ref alphaSpeed, 0.2f, Time.deltaTime);
		Color c = warningText.color;
		c.a = currentWarningAlpha;
		warningText.color = c;

		c = warningBacking.material.color;
		c.a = currentWarningAlpha;
		warningBacking.material.color = c;

		windmillCurrentSpeed = Smoothing.SpringSmooth(windmillCurrentSpeed, windmillTargetSpeed, ref windmillAcceleration, 0.25f, Time.deltaTime);
		for(int i=0; i<windmills.Length; i++){
			Windmill windmill = windmills[i];
			windmill.RotationSpeed = windmillCurrentSpeed;
		}
	}

	Quaternion initialRotation;

	IEnumerator Popup(){
		yield return new WaitForSeconds(1.3f);
		if(Toaster.Instance != null){
			Toaster.Instance.Toast();
		}
	}


	void StopTracking(){
		if(canStopHeadTracking){
			canStopHeadTracking = false;
			src.Play();
			windmillTargetSpeed = 0;
			tracking = TrackingMode.Off;
			head.trackRotation = false;
			trackingTimer = 0;
		}
	}


}
