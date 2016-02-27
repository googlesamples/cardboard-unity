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

public class TargetScene : DemoSceneState {

	[SerializeField] GazeTarget menu; 
	[SerializeField] GazeTarget restart;
	[SerializeField] GazeTarget nextLab; 

	enum TargetScenePhase{None, Part1, Part2 }

	TargetScenePhase phase = TargetScenePhase.None;

	//[SerializeField] GameObject introTitle;

	[SerializeField] TextMesh[] introTexts;
	[SerializeField] TextMesh secondText;
	[SerializeField] MeshRenderer introCard;
	[SerializeField] MeshRenderer secondCard;
	//[SerializeField] GameObject introCopy;

	[SerializeField] float cardOpacity = 0.4f;

	[SerializeField] GazeTarget initialBalloon;

	[SerializeField] GazeTarget secondBalloon;
	[SerializeField] GazeTarget[] randomBalloon;

	[SerializeField] AmbientAudioTarget ambientTrack = new AmbientAudioTarget();

	int rBalloonIdx = 0;

	[SerializeField] Color[] colors;
	int lastColorIdx = 0;
	//[SerializeField] GameObject part2Copy;

	Vector3 initialPosition;

//	public Toaster toaster;

	int balloonPopCount = 0;


	float lastToastAlert = -1000;
	bool complete = false;

	void Awake(){
		initialPosition = randomBalloon[0].transform.localPosition;
	}


	public override void EnterState(SceneManager context, int idx = 0){
		base.EnterState(context);

		SoundManager.Instance.SetAmbientTrack(0, ambientTrack.data,ambientTrack.targetVolume );

		SetPhase(TargetScenePhase.Part1);
		VRInput.Instance.ReticleColor = new Color(0,0,0,0);
		VRInput.Instance.MaxReticleDistance = 8;
		initialBalloon.GetComponent<Balloon>().Restore();
		secondBalloon.gameObject.SetActive(false);
		for(int i=0; i<randomBalloon.Length; i++){
			randomBalloon[i].gameObject.SetActive(false);
		}
		rBalloonIdx = 0;
		balloonPopCount = 0;
		complete = false;

		for(int i=0; i<introTexts.Length; i++){
			Color c = introTexts[i].color;
			c.a = 1;
			introTexts[i].color = c;
		}
		Color c3 = introCard.material.color;
		c3.a = 1 * cardOpacity;
		introCard.material.color = c3;

		Color c4 = secondCard.material.color;
		c4.a = 0 * cardOpacity;
		secondCard.material.color = c4;

		Color c2 = secondText.color;
		c2.a = 0;
		secondText.color = c2;

		
	}

	public override void ExitState(SceneManager context){
		base.ExitState(context);
	}

	public override void DoUpdate(SceneManager context){
		if(menu.IsClicked){
			SceneManager.Instance.StateTransition(SceneManager.Instance.menuScene);
		}
		else if(restart.IsClicked){
			SceneManager.Instance.StateTransition(SceneManager.Instance.targetScene);
		}
		else if(nextLab.IsClicked){
			SceneManager.Instance.StateTransition(SceneManager.Instance.visionScene);
		}

		if(initialBalloon.IsClicked){
			initialBalloon.GetComponent<Balloon>().Pop();
			secondBalloon.GetComponent<Balloon>().Restore();

		}
		else if(secondBalloon.IsClicked){
			secondBalloon.GetComponent<Balloon>().Pop();
			randomBalloon[0].GetComponent<Balloon>().Restore();
			SetPhase(TargetScenePhase.Part2);

		}
		else if (randomBalloon[rBalloonIdx].IsClicked){
			balloonPopCount +=1;
			randomBalloon[rBalloonIdx].GetComponent<Balloon>().Pop();
			rBalloonIdx = (rBalloonIdx + 1)%randomBalloon.Length;
			if(balloonPopCount >1){

				if( !complete){
					complete = true;
					
				}

				if( Time.time > lastToastAlert +15 ){


					lastToastAlert = Time.time;
					if(Toaster.Instance != null){
						Toaster.Instance.Toast();
					}
					//toaster.TargetAlpha = 1;
					//toaster.Timeout = 2;
				}
			}
			
		}


		if(phase == TargetScenePhase.Part1){

			if(!initialBalloon.IsClicked){
				if(!menu.IsClicked && !restart.IsClicked && ! nextLab.IsClicked){
					if(VRInput.Instance.PrimaryButton.Pressed){

						VRInput.Instance.SetReticleColor(new Color(1,0.2f,0.2f,1) );
					}
				}
			}

			if((VRInput.Instance.Pitch) < -30){
				
				VRInput.Instance.ReticleColor = new Color(0,0,0,0.5f);
			}
			else{
				VRInput.Instance.ReticleColor = new Color(0,0,0,0);
			}

			for(int i=0; i<introTexts.Length; i++){
				Color c = introTexts[i].color;
				c.a = Mathf.Clamp01(c.a + 4f*Time.deltaTime);
				introTexts[i].color = c;
			}

			Color c2 = secondText.color;
			c2.a = Mathf.Clamp01(c2.a - 4f*Time.deltaTime);
			secondText.color = c2;
			
			Color c3 = introCard.material.color;
			c3.a =  Mathf.Clamp(c3.a + cardOpacity*4f*Time.deltaTime,0,cardOpacity);
			introCard.material.color = c3;
			
			Color c4 = secondCard.material.color;
			c4.a =  Mathf.Clamp01(c4.a - 4f*Time.deltaTime);
			secondCard.material.color = c4;
		}
		else if(phase == TargetScenePhase.Part2){
			if(randomBalloon[rBalloonIdx].gameObject.activeSelf == false){
				randomBalloon[rBalloonIdx].transform.localPosition = initialPosition + new Vector3(3.5f*(Random.value -0.5f), 0, 3.5f*(Random.value -0.5f)) ;
				randomBalloon[rBalloonIdx].GetComponent<Balloon>().Restore(false);

				if(colors.Length > 2){
					int newIdx = lastColorIdx;
					while(newIdx == lastColorIdx){
						newIdx = Random.Range(0,colors.Length);
					}
					lastColorIdx = newIdx;
					randomBalloon[rBalloonIdx].GetComponent<Balloon>().SetColor(colors[newIdx] );
				}
				else{
					randomBalloon[rBalloonIdx].GetComponent<Balloon>().SetColor(colors[Random.Range(0,colors.Length)]);
				}
				
			}

			for(int i=0; i<introTexts.Length; i++){
				Color c = introTexts[i].color;
				c.a = Mathf.Clamp01(c.a - 4f*Time.deltaTime);
				introTexts[i].color = c;
			}

			Color c2 = secondText.color;
			c2.a = Mathf.Clamp01(c2.a + 4f*Time.deltaTime);
			secondText.color = c2;

			Color c3 = introCard.material.color;
			c3.a =  Mathf.Clamp01(c3.a - 4f*Time.deltaTime);
			introCard.material.color = c3;
			
			Color c4 = secondCard.material.color;
			c4.a =  Mathf.Clamp(c4.a + cardOpacity*4f*Time.deltaTime,0,cardOpacity);
			secondCard.material.color = c4;
		}
	}


	void SetPhase(TargetScenePhase p){
		if(phase != p){
			phase = p;
			switch(p){

				case TargetScenePhase.Part1:
					//introTitle.SetActive(true);
					//introCopy.SetActive(true);
					introCard.GetComponent<Collider>().enabled = true;
					secondCard.GetComponent<Collider>().enabled = false;
					//part2Copy.SetActive(false);
					//initialBalloon.gameObject.SetActive(true);
					//secondBalloon.gameObject.SetActive(false);
					//randomBalloon.gameObject.SetActive(false);

				break;

				case TargetScenePhase.Part2:
					VRInput.Instance.ReticleColor = new Color(0.2f,0.2f,0.2f,1);
					//introTitle.SetActive(false);
					//introCopy.SetActive(false);
					introCard.GetComponent<Collider>().enabled = false;
					secondCard.GetComponent<Collider>().enabled = true;
					//part2Copy.SetActive(true);
					//initialBalloon.gameObject.SetActive(false);
					//secondBalloon.gameObject.SetActive(false);
					//randomBalloon.gameObject.SetActive(true);
				break;
			}
		}
	}


}
