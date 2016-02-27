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

public class VisionScene : DemoSceneState {

	[SerializeField] GazeTarget menu; 
	[SerializeField] GazeTarget restart;
	[SerializeField] GazeTarget nextLab; 

	[SerializeField] Transform carousel;
	[SerializeField] CarouselElement[] elements;

	[SerializeField] AudioSource src;
	float targetAngle = 0;
	float currentAngle = 0;
	float angleSpeed = 0;

	int carouselIdx = 0;
	const float angleIncrement = -45;

	bool completedCycle = false;
	[SerializeField] AmbientAudioTarget ambientTrack = new AmbientAudioTarget();

	public override void EnterState(SceneManager context, int idx = 0){
		base.EnterState(context);
		SoundManager.Instance.SetAmbientTrack(0, ambientTrack.data,ambientTrack.targetVolume );
		targetAngle = 0;
		currentAngle = 0;
		angleSpeed = 0;
		carouselIdx = 0;
		carousel.localRotation = Quaternion.AngleAxis(0, Vector3.up);
		completedCycle = false;
		VRInput.Instance.ReticleColor = new Color(0,0,0,0);


		for(int i=0; i<elements.Length; i++){
			elements[i].targetAlpha = 0;
		}
		elements[carouselIdx].targetAlpha =1;
		
		for(int i=0; i<elements.Length; i++){
			CarouselElement e = elements[i];

				e.currentAlpha = e.targetAlpha;
				Color c = e.titleTextRenderer.color;
				c.a = e.currentAlpha;
				e.titleTextRenderer.color =c;
				e.bodyTextRenderer.color = c;
				if(e.lineMeshRenderer != null){
					c = e.lineMeshRenderer.material.color;
					c.a = e.currentAlpha;
					e.lineMeshRenderer.material.color = c;
				}

				if(e.ringMeshRenderer != null){
					c = e.ringMeshRenderer.material.color;
					c.a = e.currentAlpha;
					e.ringMeshRenderer.material.color = c;
				}

				if(e.dotMeshRenderer != null){
					c = e.dotMeshRenderer.material.color;
					c.a = e.currentAlpha;
					e.dotMeshRenderer.material.color = c;
				}
				if(e.otherRenderer != null){
					c = e.otherRenderer.color;
					c.a = e.currentAlpha;
					e.otherRenderer.color = c;
				}
				if(e.plateRenderer != null){
					c = e.plateRenderer.material.color;
					c.a = e.currentAlpha*e.plateAlpha;
					e.plateRenderer.material.color = c;
				}
		}
	}

	public override void ExitState(SceneManager context){
		base.ExitState(context);
	}

	public override void DoUpdate(SceneManager context){
		if(menu.IsClicked){
			SceneManager.Instance.StateTransition(SceneManager.Instance.menuScene);
		}
		else if(restart.IsClicked){
			SceneManager.Instance.StateTransition(SceneManager.Instance.visionScene);
		}
		else if(nextLab.IsClicked){
			SceneManager.Instance.StateTransition(SceneManager.Instance.minecartScene);
		}
		else{
			if(VRInput.Instance.PrimaryButton.Pressed){
				IncrementCarouselAngle();
			}
		}
		if((VRInput.Instance.Pitch) < -30){
				
			VRInput.Instance.ReticleColor = new Color(0,0,0,0.5f);
		}
		else{
			VRInput.Instance.ReticleColor = new Color(0,0,0,0);
		}
		currentAngle = Smoothing.SpringSmoothAngle(currentAngle, targetAngle, ref angleSpeed, 0.25f, Time.deltaTime);
		carousel.localRotation = Quaternion.AngleAxis(currentAngle, Vector3.up);

		if(!completedCycle){
			for(int i=0; i<elements.Length; i++){
				elements[i].targetAlpha = 0;
			}
			elements[carouselIdx].targetAlpha =1;
		}
		for(int i=0; i<elements.Length; i++){
			CarouselElement e = elements[i];
			if(e.targetAlpha != e.currentAlpha){
				e.currentAlpha = Smoothing.SpringSmooth(e.currentAlpha, e.targetAlpha, ref e.alphaSpeed, 0.25f, Time.deltaTime);
				Color c = e.titleTextRenderer.color;
				c.a = e.currentAlpha;
				e.titleTextRenderer.color =c;
				e.bodyTextRenderer.color = c;

				if(e.lineMeshRenderer != null){
					c = e.lineMeshRenderer.material.color;
					c.a = e.currentAlpha;
					e.lineMeshRenderer.material.color = c;
				}

				if(e.ringMeshRenderer != null){
					c = e.ringMeshRenderer.material.color;
					c.a = e.currentAlpha;
					e.ringMeshRenderer.material.color = c;
				}

				if(e.dotMeshRenderer != null){
					c = e.dotMeshRenderer.material.color;
					c.a = e.currentAlpha;
					e.dotMeshRenderer.material.color = c;
				}
				if(e.otherRenderer != null){
					c = e.otherRenderer.color;
					c.a = e.currentAlpha;
					e.otherRenderer.color = c;
				}
				if(e.plateRenderer != null){
					c = e.plateRenderer.material.color;
					c.a = e.currentAlpha*e.plateAlpha;
					e.plateRenderer.material.color = c;
				}

			}
		}

	}

	IEnumerator Popup(){
		yield return new WaitForSeconds(1.3f);
		if(Toaster.Instance != null){
			Toaster.Instance.Toast();
		}
	}

	void IncrementCarouselAngle(){
		carouselIdx = (carouselIdx +1);
		src.pitch = 0.9f + 0.2f*Random.value;
		src.Play();
		///separate out the modulus so we can check when we get back around

		
		if(carouselIdx ==8){

			StartCoroutine(Popup());
			completedCycle = true;
			for(int i=0; i<elements.Length; i++){
				elements[i].targetAlpha = 1;
			}
		}

		carouselIdx = carouselIdx%8;

		targetAngle = carouselIdx*angleIncrement;//(targetAngle + angleIncrement + 360)%360;

	}


	[System.Serializable]
	public class CarouselElement{



		public TextMesh titleTextRenderer;
		public TextMesh bodyTextRenderer;
		public MeshRenderer lineMeshRenderer;
		public MeshRenderer ringMeshRenderer;
		public MeshRenderer dotMeshRenderer;

		public MeshRenderer plateRenderer;
		public float plateAlpha;
		public TextMesh otherRenderer;
		[HideInInspector]
		public float targetAlpha;
		[HideInInspector]
		public float alphaSpeed;
		[HideInInspector]
		public float currentAlpha;
	}

}
