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
/// This scene unfortunately controls the entire hike.  This clearly needs a different architecture, a sub-state.
/// </summary>
public class RedwoodScene : DemoSceneState {

	[SerializeField] GazeTarget menu;
	[SerializeField] GazeTarget next; 
	[SerializeField] GazeTarget restart;  
	[SerializeField] MovementGazeTarget[] orderedList;

	[SerializeField] Lighting lighting;

	[SerializeField] int part1Spawn;
	[SerializeField] int part2Spawn;
	[SerializeField] int part3Spawn;
	[SerializeField] int part4Spawn;
	[SerializeField] int part5Spawn;




	[SerializeField] RedwoodSign[] signs;
	[SerializeField] MeshRenderer lightBeam;

	private int currentTarget;
	AmbientAudioTarget ambientTrack1 = new AmbientAudioTarget();
	AmbientAudioTarget ambientTrack2 = new AmbientAudioTarget();

	AmbientAudioTarget previousAmbientTrack1 = new AmbientAudioTarget();
	AmbientAudioTarget previousAmbientTrack2 = new AmbientAudioTarget();

	float time = 0;
	float currentTime = 0;
	float previousTime = 0;

	enum Section{Part1, Part2, Part3, Part4, Part5};

	Section currentSection = Section.Part1;

	[SerializeField] AudioSource stars1;
	[SerializeField] AudioSource stars2;
	[SerializeField] AudioSource stars3;
	[SerializeField] Sound3D owl;
	[SerializeField] Sound3D owlScreech;
	[SerializeField] int owlTrigger;
	[SerializeField] GazeTarget owlGazeTarget; 
	[SerializeField] int owlNode;
	bool owlHasScreeched = false;
	[SerializeField] Animation owlAnimation;
	[SerializeField] GameObject owlObject;

	AnimationState owlBlink;
	AnimationState owlHoot;
	AnimationState owlLook;
	AnimationState owlLook2;
	float owlBlinkTimer = 0;
	float owlLookTimer = 0;
	bool justHooted = false;

	[SerializeField] OptimizationGroup[] optimizations;

	[SerializeField] GazeTarget bigDipperTarget; 
	[SerializeField] GazeTarget littleDipperTarget; 
	[SerializeField] GazeTarget androidTarget; 

	[SerializeField] GameObject nextButton;

	Vector3 arrowLocalPos;
	[SerializeField] MeshRenderer arrow;
	[SerializeField] MeshRenderer ctaBack;
	[SerializeField] MeshRenderer ctaText;

	[SerializeField] MeshRenderer[] flares;

	[SerializeField] MeshRenderer advancedTitle;
	[SerializeField] MeshRenderer advancedIcon;
	[SerializeField] Transform splashTransform;

	[SerializeField] MeshRenderer starsArrow;
	[SerializeField] Transform starsArrowTransform;
	Vector3 starsArrowTransformPosition;
	float frequency=6;
	float amplitude = 0.018f;
	float ctaTimer = 0;

	bool hasLookedConstellation = false;

	public override void ClearBlackout(){
		//do nothing
		
	}


	void Awake(){
		starsArrowTransformPosition = starsArrowTransform.localPosition;
		arrowLocalPos = arrow.transform.localPosition;
	}

	/// <summary>
	/// When entering the hike from the last lab, we need to trigger a splash screen to indicate that the user has entered the hike
	/// </summary>
	IEnumerator Splash(){
		advancedTitle.enabled = true;
		advancedIcon.enabled = true;
		splashTransform.rotation = Quaternion.AngleAxis(VRInput.Instance.Yaw, Vector3.up);
		splashTransform.position = VRInput.Instance.Position;
		float timer = 0;
		while(timer< 0.5f){
			splashTransform.position = VRInput.Instance.Position;
			timer += Time.deltaTime;
			float alpha = 2*timer;

			Color c = advancedTitle.material.color;
			c.a = alpha;
			advancedTitle.material.color = c;

			c = advancedIcon.material.color;
			c.a = alpha;
			advancedIcon.material.color = c;
			yield return new WaitForSeconds(0);
		}

		yield return new WaitForSeconds(2);

		timer = 0;
		while(timer< 0.5f){
			timer += Time.deltaTime;
			float alpha = 1-2*timer;

			Color c = advancedTitle.material.color;
			c.a = alpha;
			advancedTitle.material.color = c;

			c = advancedIcon.material.color;
			c.a = alpha;
			advancedIcon.material.color = c;
			yield return new WaitForSeconds(0);
		}
		advancedTitle.enabled = false;
		advancedIcon.enabled = false;
		Blackout.Instance.TargetBlackoutValue = 0;
	}

	/// <summary>
/// Mostly clearing state, and ensuring everything is back to normal
/// </summary>
	public override void EnterState(SceneManager context, int idx = 0){

		hasLookedConstellation = false;
		VRInput.Instance.ForceReticleColor(new Color(1,1,1,0.5f) );
		advancedTitle.enabled = false;
		advancedIcon.enabled = false;
		ctaTimer = 0;
		switch(idx){

			case -1:
				currentSection = Section.Part1;
				currentTarget = part1Spawn;
				
				break;

			case 0:
			default:
				currentSection = Section.Part1;
				currentTarget = part1Spawn;
				Blackout.Instance.TargetBlackoutValue = 0;
				break;

			case 1:
				currentSection = Section.Part2;
				currentTarget = part2Spawn;
				Blackout.Instance.TargetBlackoutValue = 0;
				break;

			case 2:
				currentSection = Section.Part3;
				currentTarget = part3Spawn;
				Blackout.Instance.TargetBlackoutValue = 0;
				break;

			case 3:
				currentSection = Section.Part4;
				currentTarget = part4Spawn;
				Blackout.Instance.TargetBlackoutValue = 0;
				break;

			case 4:
				currentSection = Section.Part5;
				currentTarget = part5Spawn;
				Blackout.Instance.TargetBlackoutValue = 0;
				break;

		}
		spawnPoint = orderedList[currentTarget];
		previousAmbientTrack1 = ambientTrack1 = orderedList[currentTarget].AmbientTrack1;
		previousAmbientTrack2 = ambientTrack2 = orderedList[currentTarget].AmbientTrack2;
		SoundManager.Instance.SetAmbientTrack(0, ambientTrack1.data,ambientTrack1.targetVolume );
		SoundManager.Instance.SetAmbientTrack(1, ambientTrack2.data,ambientTrack2.targetVolume );
		
		base.EnterState(context);
		if(idx ==-1){
			StartCoroutine(Splash());
		}
		time = currentTime = previousTime = orderedList[currentTarget].TimeOfDay;
		lighting.SetTime(time);
		UpdateTarget(true);
		UpdateSigns(true);
		owlObject.SetActive(false);
		owlBlink = owlAnimation["Blink"];
		owlBlink.layer = 5;
		//owlBlink.blendMode =AnimationBlendMode.Additive;
		owlHoot = owlAnimation["Hoot"];
		owlHoot.layer = 5;
		//owlHoot.blendMode =AnimationBlendMode.Additive;

		owlLook = owlAnimation["Idle_Look"];
		owlLook.layer = 1;
		owlLook2 = owlAnimation["Idle_Look_2"];
		owlLook2.layer = 1;

		Color ctaCol = arrow.material.color;
		ctaCol.a = 0;
		arrow.material.color = ctaCol;

		ctaCol = ctaText.material.color;
		ctaCol.a = 0;
		ctaText.material.color = ctaCol;

		ctaCol = ctaBack.material.color;
		ctaCol.a = 0;
		ctaBack.material.color = ctaCol;
	}


	/// <summary>
/// Each node may have a different starting orientation point, so we can ensure the user faces forward when they enter
/// </summary>
	public override float GetOrientationOffset(int idx){
		switch(idx){
			case 0:
			default:
				return orderedList[part1Spawn].DefaultOrientation;
				

			case 1:
				return orderedList[part2Spawn].DefaultOrientation;
				

			case 2:
				return orderedList[part3Spawn].DefaultOrientation;
				

			case 3:
				return orderedList[part4Spawn].DefaultOrientation;
				

			case 4:
				return orderedList[part5Spawn].DefaultOrientation;
				

		}
	}

	public override void ExitState(SceneManager context){
		base.ExitState(context);

		if(SoundManager.Instance != null){
			SoundManager.Instance.SetAmbientTrack(0, null,0);
			SoundManager.Instance.SetAmbientTrack(1, null,0);
		}
		if(StarRendering.Instance != null){
			StarRendering.Instance.Color = Color.black;
		}
	}


	Color constellationColor = new Color(0, 34/255.0f, 111f/255.0f,1);
	Vector4 starSpeed = Vector4.zero;
	Color targetColor = Color.black;
	Color currentColor = Color.black;

	Vector4 starSpeed2 = Vector4.zero;
	Color targetColor2 = Color.black;
	Color currentColor2 = Color.black;

	Vector4 starSpeed3 = Vector4.zero;
	Color targetColor3 = Color.black;
	Color currentColor3 = Color.black;


	/// <summary>
/// If we are at a specific node, draw the constellations if the player is looking at a specific place in the sky.  The stars and constellations are procedural meshes, created at startup.  This is mostly color fades and transitions to make them fade in/out nicely.
/// </summary>
	void Constellations(){
		if(currentTarget == 14 ){
			if(!PlayerMover.Instance.Moving){
				if(hasLookedConstellation){

					Color c = starsArrow.material.color;
					c.a = Mathf.Clamp01(c.a - Time.deltaTime);
					starsArrow.material.color = c;
				}
				else{
					Color c = starsArrow.material.color;
					c.a = Mathf.Clamp01(c.a + Time.deltaTime);
					starsArrow.material.color = c;
				}
				
			}

			starsArrowTransform.localPosition =  starsArrowTransformPosition + new Vector3(0,amplitude*Mathf.Sin(frequency*Time.time),0);
			if(bigDipperTarget.IsTargetThisFrame){
				stars1.Play();
				hasLookedConstellation = true;
			}

			if(bigDipperTarget.IsTarget){

				//StarRendering.Instance.constellations[1].Color
				targetColor2 = constellationColor;
			}
			else{
				//StarRendering.Instance.constellations[1].Color
				targetColor2 = Color.black;
			}
			if(littleDipperTarget.IsTargetThisFrame){
				stars2.Play();
				hasLookedConstellation = true;
			}
			if(littleDipperTarget.IsTarget){
				//StarRendering.Instance.constellations[0].Color
				targetColor = constellationColor;
			}
			else{
				//StarRendering.Instance.constellations[0].Color
				targetColor = Color.black;
			}
			if(androidTarget.IsTargetThisFrame){
				stars3.Play();
				hasLookedConstellation = true;
			}
			if(androidTarget.IsTarget){
				//StarRendering.Instance.constellations[0].Color
				targetColor3 = constellationColor;
			}
			else{
				//StarRendering.Instance.constellations[0].Color
				targetColor3 = Color.black;
			}
		}
		else{
			Color c = starsArrow.material.color;
			if(c.a > 0){
				c.a = Mathf.Clamp01(c.a - Time.deltaTime);
				starsArrow.material.color = c;
			}
			//StarRendering.Instance.constellations[1].Color
			targetColor2 = Color.black;
			
			
			//StarRendering.Instance.constellations[0].Color
			targetColor = Color.black;

			targetColor3 = Color.black;

		}
		if( Mathf.Abs(targetColor.r - currentColor.r) > 0.001f ){
			currentColor.r = Smoothing.SpringSmooth(currentColor.r, targetColor.r, ref starSpeed.x, 0.25f, Time.deltaTime);
		}
		if( Mathf.Abs(targetColor.g - currentColor.g) > 0.001f ){
			currentColor.g = Smoothing.SpringSmooth(currentColor.g, targetColor.g, ref starSpeed.y, 0.25f, Time.deltaTime);
		}
		if( Mathf.Abs(targetColor.b - currentColor.b) > 0.001f ){
			currentColor.b = Smoothing.SpringSmooth(currentColor.b, targetColor.b, ref starSpeed.z, 0.25f, Time.deltaTime);
		}
		if( Mathf.Abs(targetColor.a - currentColor.a) > 0.001f ){
			currentColor.a = Smoothing.SpringSmooth(currentColor.a, targetColor.a, ref starSpeed.w, 0.25f, Time.deltaTime);
		}

		if( Mathf.Abs(targetColor2.r - currentColor2.r) > 0.001f ){
			currentColor2.r = Smoothing.SpringSmooth(currentColor2.r, targetColor2.r, ref starSpeed2.x, 0.25f, Time.deltaTime);
		}
		if( Mathf.Abs(targetColor2.g - currentColor2.g) > 0.001f ){
			currentColor2.g = Smoothing.SpringSmooth(currentColor2.g, targetColor2.g, ref starSpeed2.y, 0.25f, Time.deltaTime);
		}
		if( Mathf.Abs(targetColor2.b - currentColor2.b) > 0.001f ){
			currentColor2.b = Smoothing.SpringSmooth(currentColor2.b, targetColor2.b, ref starSpeed2.z, 0.25f, Time.deltaTime);
		}
		if( Mathf.Abs(targetColor2.a - currentColor2.a) > 0.001f ){
			currentColor2.a = Smoothing.SpringSmooth(currentColor2.a, targetColor2.a, ref starSpeed2.w, 0.25f, Time.deltaTime);
		}

		if( Mathf.Abs(targetColor3.r - currentColor3.r) > 0.001f ){
			currentColor3.r = Smoothing.SpringSmooth(currentColor3.r, targetColor3.r, ref starSpeed3.x, 0.25f, Time.deltaTime);
		}
		if( Mathf.Abs(targetColor3.g - currentColor3.g) > 0.001f ){
			currentColor3.g = Smoothing.SpringSmooth(currentColor3.g, targetColor3.g, ref starSpeed3.y, 0.25f, Time.deltaTime);
		}
		if( Mathf.Abs(targetColor3.b - currentColor3.b) > 0.001f ){
			currentColor3.b = Smoothing.SpringSmooth(currentColor3.b, targetColor3.b, ref starSpeed3.z, 0.25f, Time.deltaTime);
		}
		if( Mathf.Abs(targetColor3.a - currentColor3.a) > 0.001f ){
			currentColor3.a = Smoothing.SpringSmooth(currentColor3.a, targetColor3.a, ref starSpeed3.w, 0.25f, Time.deltaTime);
		}
		StarRendering.Instance.constellations[0].Color = currentColor;
		StarRendering.Instance.constellations[1].Color = currentColor2;
		StarRendering.Instance.constellations[2].Color = currentColor3;
		
	}


	public override void DoUpdate(SceneManager context){



		///Detect which section we are in, selectively disable some parts of the lookdown menu where appropriate
		if(currentTarget < part2Spawn){
			currentSection = Section.Part1;
			nextButton.SetActive(true);
		}
		else if(currentTarget < part3Spawn){
			currentSection = Section.Part2;
			nextButton.SetActive(true);
		}
		else if(currentTarget < part4Spawn){
			currentSection = Section.Part3;
			nextButton.SetActive(true);
		}
		else if(currentTarget < part5Spawn){
			currentSection = Section.Part4;
			nextButton.SetActive(true);
		}
		else{
			currentSection = Section.Part5;
			nextButton.SetActive(false);
		}

		///check the menu buttons
		if(menu.IsClicked){
			SceneManager.Instance.StateTransition(SceneManager.Instance.menuScene, Color.black);
			Debug.Log("menu");
		}
		else if(restart.IsClicked){


			SceneManager.Instance.StateTransition(SceneManager.Instance.redwoodScene,Color.black, (int)currentSection);
		}
		else if(next.IsClicked){
			SceneManager.Instance.StateTransition(SceneManager.Instance.redwoodScene,Color.black, (int)currentSection+1);
		}
		else {
			////Check to see if we are trying to move to a new node
			if(currentTarget > 0){
				int previousTarget = currentTarget-1;
				if(orderedList[previousTarget].IsClicked){

					PlayerMover.Instance.SetTargetPosition(orderedList[previousTarget].transform.position, orderedList[previousTarget]);
					if(PlayerMover.Instance.CurrentTarget == orderedList[previousTarget]){
						
						currentTime = orderedList[previousTarget].TimeOfDay;
						ambientTrack1 = orderedList[previousTarget].AmbientTrack1;
						ambientTrack2 = orderedList[previousTarget].AmbientTrack2;

						previousTime = orderedList[currentTarget].TimeOfDay;
						previousAmbientTrack1 = orderedList[currentTarget].AmbientTrack1;
						previousAmbientTrack2 = orderedList[currentTarget].AmbientTrack2;

						currentTarget = previousTarget;
						UpdateTarget(false);
					}
				}
			}
			if(currentTarget < orderedList.Length-1){
				int nextTarget = currentTarget+1;
				if(orderedList[nextTarget].IsClicked){
					PlayerMover.Instance.SetTargetPosition(orderedList[nextTarget].transform.position, orderedList[nextTarget]);
					if(PlayerMover.Instance.CurrentTarget == orderedList[nextTarget]){
						currentTime = orderedList[nextTarget].TimeOfDay;
						ambientTrack1 = orderedList[nextTarget].AmbientTrack1;
						ambientTrack2 = orderedList[nextTarget].AmbientTrack2;

						previousTime = orderedList[currentTarget].TimeOfDay;
						previousAmbientTrack1 = orderedList[currentTarget].AmbientTrack1;
						previousAmbientTrack2 = orderedList[currentTarget].AmbientTrack2;

						currentTarget = nextTarget;
						UpdateTarget(false);
					}
					
				}

			}

			///draw these
			Constellations();
			
			///Display the reticle if we look down, and everywhere except the last node, unless you are looking backward (ugh...)
			if((VRInput.Instance.Pitch) < -30){
				VRInput.Instance.ReticleColor = new Color(1,1,1,0.5f);
			}
			else{
				if (currentTarget== orderedList.Length-1){
					if( Mathf.Abs(Mathf.DeltaAngle(VRInput.Instance.Yaw, OrientationYaw + orderedList[currentTarget].DefaultOrientation - 90)) > 90){
						VRInput.Instance.ReticleColor = new Color(1,1,1,0.5f);
					}
					else{
						VRInput.Instance.ReticleColor = new Color(1,1,1,0f);
					}
					
				}
				else{
					VRInput.Instance.ReticleColor = new Color(1,1,1,0.5f);
				}
			}

			///draw these, too
			UpdateSigns();
			UpdateFlare();


			/// If we're moving between nodes, adjust volume of the ambient audio tracks between the current node and target node.  If the tracks arent the same (e.g. are different clips), we fade one out, and then the other in
			time = PlayerMover.Instance.SmoothedBlend * currentTime + (1-PlayerMover.Instance.SmoothedBlend) * previousTime;
			if(SoundManager.Instance != null){
				if(ambientTrack1.data == previousAmbientTrack1.data){
					SoundManager.Instance.SetAmbientTrack(0, ambientTrack1.data,ambientTrack1.targetVolume *  PlayerMover.Instance.SmoothedBlend + previousAmbientTrack1.targetVolume *  (1-PlayerMover.Instance.SmoothedBlend) );
				}
				else{
					if(PlayerMover.Instance.SmoothedBlend < 0.5f){
						SoundManager.Instance.SetAmbientTrack(0, previousAmbientTrack1.data,previousAmbientTrack1.targetVolume *  2*(0.5f-PlayerMover.Instance.SmoothedBlend) );
					}
					else{
						SoundManager.Instance.SetAmbientTrack(0, ambientTrack1.data,ambientTrack1.targetVolume *  2*(PlayerMover.Instance.SmoothedBlend-0.5f) );
					}
					
				}
				if(ambientTrack2.data == previousAmbientTrack2.data){
					SoundManager.Instance.SetAmbientTrack(1, ambientTrack2.data,ambientTrack2.targetVolume *  PlayerMover.Instance.SmoothedBlend + previousAmbientTrack2.targetVolume *  (1-PlayerMover.Instance.SmoothedBlend) );
				}
				else{
					if(PlayerMover.Instance.SmoothedBlend < 0.5f){
						SoundManager.Instance.SetAmbientTrack(1, previousAmbientTrack2.data,previousAmbientTrack2.targetVolume *  2*(0.5f-PlayerMover.Instance.SmoothedBlend) );
					}
					else{
						SoundManager.Instance.SetAmbientTrack(1, ambientTrack2.data,ambientTrack2.targetVolume *  2*(PlayerMover.Instance.SmoothedBlend-0.5f) );

					}
				}
			}

			/// fade in the lightbeam if we are at the first or second node, otherwise fade it out
			Color lightCol = lightBeam.material.color;
			if(currentTarget <= 1){
				if(lightCol.a < 1){
					lightCol.a = Mathf.Clamp01(lightCol.a + Time.deltaTime);
					lightBeam.material.color = lightCol;
				}
			}
			else{
				if(lightCol.a > 0){
					lightCol.a = Mathf.Clamp01(lightCol.a - Time.deltaTime);
					lightBeam.material.color = lightCol;
				}
			}

			//// if we are at the first node, and the player hasn't moved in a while, fade in a tutorial message to guide them along
			if(currentTarget == 0){
				ctaTimer += Time.deltaTime;
				if(ctaTimer > 30){
					Color ctaCol = arrow.material.color;
					ctaCol.a = Mathf.Clamp01(ctaCol.a + Time.deltaTime);
					arrow.material.color = ctaCol;

					arrow.transform.localPosition = arrowLocalPos + new Vector3(0,0.6f*amplitude*Mathf.Sin(frequency*Time.time),0);

					ctaCol = ctaText.material.color;
					ctaCol.a = Mathf.Clamp01(ctaCol.a + Time.deltaTime);
					ctaText.material.color = ctaCol;

					ctaCol = ctaBack.material.color;
					ctaCol.a = Mathf.Clamp01(ctaCol.a + Time.deltaTime);
					ctaBack.material.color = ctaCol;
				}
			}
			else{
				ctaTimer = 0;
				Color ctaCol = arrow.material.color;
				ctaCol.a = Mathf.Clamp01(ctaCol.a - Time.deltaTime);
				arrow.material.color = ctaCol;

				ctaCol = ctaText.material.color;
				ctaCol.a = Mathf.Clamp01(ctaCol.a - Time.deltaTime);
				ctaText.material.color = ctaCol;

				ctaCol = ctaBack.material.color;
				ctaCol.a = Mathf.Clamp01(ctaCol.a - Time.deltaTime);
				ctaBack.material.color = ctaCol;
			}


			/// just like we blended audio, also blend the lighting from a to b.  lighting takes care of this on it's own
			lighting.SetTime(time);


			/// Owl logic, if we are at the owl place, and it hasn't screeched, and we are looking at the fire, and we aren't transitioning to another node, make the owl appear behind the player and screech - we also want this to keep happening over and over
			/// a bunch more owl logic, too, for all kinds of requests - randomized animation selection, hooting, etc
			if(currentTarget == owlNode && !owlHasScreeched && owlGazeTarget.IsTarget && !PlayerMover.Instance.Moving){
				owlObject.SetActive(true);
				owlAnimation.Play("Idle");
				owlHasScreeched = true;
				owlScreech.Play();
				owlHoot.time = 0;
				owlHoot.weight = 1;
				owlHoot.speed = 1f;
				owlHoot.enabled = true;
			}
			else if(currentTarget == owlNode && owlHasScreeched && owlGazeTarget.IsTargetThisFrame && !PlayerMover.Instance.Moving){
				owlScreech.Play();
				owlHoot.time = 0;
				owlHoot.weight = 1;
				owlHoot.speed = 1f;
				owlHoot.enabled = true;
			}
			if(owlHasScreeched){
				if(Time.time > owlBlinkTimer){
					if(owlBlink.enabled == false){
						//Debug.Log("blink");
						owlBlink.time = 0;
						owlBlink.weight = 1;
						owlBlink.speed = 1.5f;
						owlBlink.enabled = true;
						owlBlinkTimer = Time.time + 0.5f + 9*Random.value;
					}
				}
				if(Time.time > owlLookTimer){
					owlLookTimer = Time.time + 3f + 10*Random.value;
					if(owlHoot.enabled == false && owlLook.enabled == false && owlLook2.enabled == false){
						float rando = Random.value;
						if(!justHooted){
							justHooted =true;
							owlHoot.time = 0;
							owlHoot.weight = 1;
							owlHoot.speed = 1f;
							owlHoot.enabled = true;
							owlScreech.Play();
						}
						else if(rando > 0.5f){
							justHooted = false;
							owlLook.time = 0;
							owlLook.weight = 1;
							owlLook.speed = 1f;
							owlLook.enabled = true;
						}
						else{
							justHooted = false;
							owlLook2.time = 0;
							owlLook2.weight = 1;
							owlLook2.speed = 1f;
							owlLook2.enabled = true;
						}
					}

				}
			}
		}
	}

	/// <summary>
/// Turn off objects as we progress through the hike - important in the final section when we extend the far clip plane.  Howerver, decided to leave everything on at the start, since turning things on is pricey.  Could consider a 'warmup' in EnterState
/// </summary>
	void UpdateOptimizations(){
		for(int i=0; i<optimizations.Length; i++){
			OptimizationGroup op = optimizations[i];
			if(currentTarget < op.firstNode || currentTarget > op.lastNode){
				for(int j=0; j<op.objects.Length; j++){
					op.objects[j].SetActive(false);
				}
			}
			else{
				for(int j=0; j<op.objects.Length; j++){
					op.objects[j].SetActive(true);
				}
			}
		}
	}

	/// <summary>
	/// An owl screech on a delay
	/// </summary>
	IEnumerator PlayFirstOwl(){
		yield return new WaitForSeconds(0.5f);
		owl.Play();
	}

	/// <summary>
	/// show the next node on a delay
	/// </summary>
	IEnumerator ShowNextMenuItem(int idx, bool immediate){
		float timer = 0;
		while (PlayerMover.Instance.Moving && timer < 10){
			timer += Time.deltaTime;
			yield return new WaitForSeconds(0);
		}
		orderedList[idx].Show(immediate);
	}

	/// <summary>
	/// on target change, hide the previous target, show the next one 
	/// </summary>
	void UpdateTarget(bool immediate){
		UpdateOptimizations();

		if(currentTarget == owlTrigger){
			StartCoroutine( PlayFirstOwl() );
		}
		if(currentTarget == owlNode){
			owlHasScreeched = false;
		}
		if(currentTarget == 0){
			ctaTimer = 0;
		}
		for(int i=0; i<orderedList.Length; i++){
			if(i != currentTarget-1 && i != currentTarget+1 ){
				orderedList[i].Hide(immediate);
			}
			else{
				StartCoroutine(ShowNextMenuItem(i, immediate));
			}
		}
	}

	/// <summary>
	/// controls the sun at the overlook
	/// </summary>
	void UpdateFlare(){
		if(currentTarget == orderedList.Length-1){
			for(int i=0;i<flares.Length; i++){
				Color c = flares[i].material.color;
				if(c.a <1){
					c.a += Time.deltaTime;
					flares[i].material.color = c;
					flares[i].enabled = true;
				}

			}
		}
		else{
			for(int i=0;i<flares.Length; i++){
				Color c = flares[i].material.color;
				if(c.a >0){
					c.a -= Time.deltaTime;
					flares[i].material.color = c;
				}
				else{
					flares[i].enabled = false;
				}
			}
		}
	}

	/// <summary>
	/// fade the signs in and out
	/// </summary>
	void UpdateSigns(bool force = false){
		for(int i=0; i<signs.Length; i++){
			RedwoodSign rs = signs[i];
			if(rs.node == currentTarget && !PlayerMover.Instance.Moving){
				rs.targetAlpha = 0.75f;
			}
			else{
				rs.targetAlpha = 0;
			}

			if(rs.targetAlpha != rs.currentAlpha || force){
				rs.currentAlpha = Smoothing.SpringSmooth(rs.currentAlpha, rs.targetAlpha, ref rs.alphaSpeed, 0.25f, Time.deltaTime);
				if(Mathf.Abs(rs.currentAlpha - rs.targetAlpha ) < 0.005){
					rs.currentAlpha = rs.targetAlpha;

				}

				Color c = rs.text.color;
				c.a = rs.currentAlpha;
				rs.text.color = c;

				c = rs.title.color;
				c.a = rs.currentAlpha;
				rs.title.color = c;

				c = rs.plate.material.color;
				c.a = rs.currentAlpha;
				rs.plate.material.color = c;
			}
		}
	}


	public override MovementGazeTarget SpawnPoint{
		get{
			switch(currentSection){
				case Section.Part1:
					return orderedList[part1Spawn];
					//break;	

				case Section.Part2:
					return orderedList[part2Spawn];
					//break;

				case Section.Part3:
					return orderedList[part3Spawn];
					//break;

				case Section.Part4:
					return orderedList[part4Spawn];
					//break;

				case Section.Part5:
					return orderedList[part5Spawn];
					//break;
				default:
					currentSection = Section.Part1;
					return orderedList[part1Spawn];
					//break;
			}
			
		}
	}

	[System.Serializable]
	public class OptimizationGroup{
		public int firstNode;
		public int lastNode;
		public GameObject[] objects;
	}

	[System.Serializable]
	public class RedwoodSign{
		public TextMesh title;
		public TextMesh text;
		public MeshRenderer plate;
		public int node;

		public float currentAlpha;
		public float targetAlpha;
		public float alphaSpeed;
	}

}
