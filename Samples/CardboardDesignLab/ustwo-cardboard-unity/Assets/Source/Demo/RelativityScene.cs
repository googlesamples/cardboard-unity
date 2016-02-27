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
/// A very simple scene - infinite conveyor belt of boxes, and fade the background in and out on click
/// </summary>
public class RelativityScene : DemoSceneState {

	[SerializeField] GazeTarget menu; 
	[SerializeField] GazeTarget restart;
	[SerializeField] GazeTarget nextLab; 

	[SerializeField] Transform boxes;

	[SerializeField] Transform[] individualBoxes;

	[SerializeField] GameObject terrain;
	[SerializeField] GameObject plane;

	[SerializeField] AudioSource src;
	[SerializeField] MeshRenderer[] renderers;

	[SerializeField] AmbientAudioTarget ambientTrack = new AmbientAudioTarget();

	public float speed = 5;
	float currentZ= 82.5f;
	public float fadeSpeed = 2;

	int switchCount = 0;

	public override void EnterState(SceneManager context, int idx = 0){
		base.EnterState(context);
		SoundManager.Instance.SetAmbientTrack(0, ambientTrack.data,ambientTrack.targetVolume );
		timer = Time.time;
		VRInput.Instance.ReticleColor = new Color(0,0,0,0);
		VRInput.Instance.MaxReticleDistance = 6;
		terrain.SetActive(false);
		plane.SetActive(true);
		mode = Mode.NoReference;

		currentZ = 82.5f;
		Vector3 loc = boxes.localPosition;
		loc.z = currentZ;
		boxes.localPosition = loc;

		switchCount = 0;
		for(int i=0; i<renderers.Length; i++){
			renderers[i].material.color = new Color(1,1,1,0);
		}
	}

	public override void ExitState(SceneManager context){
		base.ExitState(context);
	}


	enum Mode{NoReference, Reference  }
	Mode mode = Mode.NoReference;



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
			SceneManager.Instance.StateTransition(SceneManager.Instance.relativityScene);
		}
		else if(nextLab.IsClicked){
			SceneManager.Instance.StateTransition(SceneManager.Instance.trackingScene);
		}
		else{
			if(mode == Mode.NoReference){
				if(VRInput.Instance.PrimaryButton.Pressed){
					switchCount ++;
					src.Play();
					Stage2();
				}
			}
			else{
				if(VRInput.Instance.PrimaryButton.Pressed){
					switchCount++;
					src.Play();
					Stage1();
				}
			
			}
		}
		currentZ -= speed *Time.deltaTime;
		if(currentZ < -82.5f){
			currentZ = currentZ + 165f;
		}
		Vector3 loc = boxes.localPosition;
		loc.z = currentZ;
		boxes.localPosition = loc;

		for(int i=0; i<individualBoxes.Length; i++){
			Transform box = individualBoxes[i];
			Vector3 worldP = box.position;
			Vector3 localP = box.localPosition;
			float absZ = Mathf.Sqrt(Mathf.Abs(worldP.z)*Mathf.Abs(worldP.z) + Mathf.Abs(worldP.x)*Mathf.Abs(worldP.x));
			if(absZ > 40){
				float d = (absZ-40)/40f;
				localP.y = -3.960891f - d*d*d*20;
			}
			else{
				localP.y = -3.960891f;
				
			}
			box.localPosition  = localP;
		}

		if(mode == Mode.Reference){
			for(int i=0; i<renderers.Length; i++){
				renderers[i].material.color = new Color(1,1,1, Mathf.Clamp01( fadeSpeed*(Time.time - timer) ) );
			}
		}
		else{
			for(int i=0; i<renderers.Length; i++){
				renderers[i].material.color = new Color(1,1,1, Mathf.Clamp01( 1-fadeSpeed*(Time.time - timer) ) );
			}
		}

		if(switchCount > 3){
			if(Toaster.Instance != null){
				Toaster.Instance.Toast();
			}
			switchCount = 0;
		}


	}


	float timer ;
	void Stage2(){
		timer = Time.time;
		mode = Mode.Reference;
		terrain.SetActive(true);
		//plane.SetActive(false);
	}
	void Stage1(){
		timer = Time.time;
		mode = Mode.NoReference;
		terrain.SetActive(true);
		//plane.SetActive(false);
	}
}
