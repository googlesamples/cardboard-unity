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
/// The main menu scene.  This has undergone a number of major revisions, and probably is a bit messy
/// There is a lot of legacy code that 'does nothing'.  There is a spring menu, which is clamped in one place, for example.  We switched very quickly between features, and this menu took a long time to rewrite, so it was easier to adjust parameters.
/// </summary>
public class MenuScene : DemoSceneState {

	[SerializeField] GazeTarget labs;
	[SerializeField] GazeTarget hike;

	[SerializeField] TextScaleTarget motion;
	[SerializeField] TextScaleTarget targeting;
	[SerializeField] TextScaleTarget tracking;
	[SerializeField] TextScaleTarget text;
	[SerializeField] TextScaleTarget relativity;
	[SerializeField] GazeTarget back;

	[SerializeField] TextScaleTarget hike1;
	[SerializeField] TextScaleTarget hike2;
	[SerializeField] TextScaleTarget hike3;
	[SerializeField] TextScaleTarget hike4;
	[SerializeField] TextScaleTarget hike5;
	[SerializeField] GazeTarget hikeBack;



	[SerializeField] Transform menu;

	[SerializeField] Transform hikeMenu;

	[SerializeField] SimpleSpringMenu springMenu;
	[SerializeField] MeshRenderer[] labsRenderers;
	[SerializeField] MeshRenderer[] redwoodsRenderers;

	[SerializeField] TextMesh[] texts;
	[SerializeField] MeshRenderer[] icons;

	[SerializeField] TextMesh labTitle;
	[SerializeField] TextMesh hikeTitle;

	[SerializeField] MeshRenderer labIcon;
	[SerializeField] MeshRenderer hikeIcon;


	[SerializeField] MeshRenderer[] labsListItems;
	[SerializeField] MeshRenderer[] hikeListItems;

	[SerializeField] AmbientAudioTarget ambientTrack = new AmbientAudioTarget();

	Color uiColor = Color.black;


	float fadeValue = 0;  ///goes from -1 to 1
	float fadeSpeed = 0;
	float targetFade = 0;
	//int realTarget = 0;

	float spring = 0.25f;

	float labsTitleAlpha = 1;
	float hikeTitleAlpha = 1;

	float labsTitleAlphaTarget = 1;
	float hikeTitleAlphaTarget = 1;

	float labsTitleAlphaSpeed = 1;
	float hikeTitleAlphaSpeed = 1;


	enum MenuMode{
		Main, 
		Labs, 
		Hike
	}

	MenuMode mode = MenuMode.Main;

	public override void EnterState(SceneManager context, int idx = 0){

		/// If for some reason a toast made it's way here, get rid of it!
		if(Toaster.Instance != null){
			Toaster.Instance.ClearToast();
		}



		base.EnterState(context);
		SoundManager.Instance.SetAmbientTrack(0, ambientTrack.data,ambientTrack.targetVolume );

		/// Setup the reticle
		VRInput.Instance.MaxReticleDistance = 6;

		/// Clear all visible display information first
		for(int i=0; i<labsRenderers.Length; i++){
			labsRenderers[i].enabled = false;
		}

		for(int i=0; i<redwoodsRenderers.Length; i++){
			if(redwoodsRenderers[i] != null){
				redwoodsRenderers[i].enabled = false;
			}
		}
		fadeValue = 0;

		///make sure we clear any internal menu state that may have been lingering
		MainMenu(true);

		/// mode will always be == MenuMode.Main
		/// we just cleared the state, so this code should be depricated, but after 8 RCs, I'm not messing with it right now.  Still, this is legacy - only here in case we decide we want the menu to return to a submenu from whereever you were...
		if(mode == MenuMode.Labs){
			for(int i=0; i<labsRenderers.Length; i++){
				labsRenderers[i].enabled = true;
				Color c = labsRenderers[i].material.color;
				c.a = fadeValue;
				labsRenderers[i].material.color = c;
			}
		}

		if(mode == MenuMode.Hike){
			for(int i=0; i<redwoodsRenderers.Length; i++){
				if(redwoodsRenderers[i] != null){
					redwoodsRenderers[i].enabled = true;
					Color c = redwoodsRenderers[i].material.color;
					c.a = -fadeValue;
					redwoodsRenderers[i].material.color = c;
				}
			}

		}

		////This is the only part that matters - more graphics initialization
		else{
			targetFade = fadeValue = 0;
			
			for(int i=0; i<redwoodsRenderers.Length; i++){
				if(redwoodsRenderers[i] != null){
					redwoodsRenderers[i].enabled = false;
					Color c = redwoodsRenderers[i].material.color;
					c.a = -fadeValue;
					redwoodsRenderers[i].material.color = c;
				}
			}
			for(int i=0; i<labsRenderers.Length; i++){
					labsRenderers[i].enabled = false;
					Color c = labsRenderers[i].material.color;
					c.a = fadeValue;
					labsRenderers[i].material.color = c;
				}
			}
		
		ResetUIColor();

	}

	///background color stuff
	void ResetUIColor(){
		float colorValue = Mathf.Clamp01(-fadeValue);
			colorValue = Mathf.Sqrt(colorValue);
			uiColor = (1-colorValue) * Color.black + (colorValue)*Color.white;
			VRInput.Instance.ForceReticleColor(new Color(uiColor.r, uiColor.g, uiColor.b, 0.5f));
			for(int i=0; i<texts.Length; i++){
				Color uicol = uiColor;
				uicol.a = texts[i].color.a;
				texts[i].color = uicol;
			}
			for(int i=0; i<icons.Length; i++){
				Color uicol = uiColor;
				uicol.a = icons[i].material.color.a;
				icons[i].material.color = uicol;
			}
	}

	void ResetMaterialColors(){

	}

	void MainMenu(bool instant = false){
		if(mode == MenuMode.Labs){
			
			hike.Invisible = false;
			mode = MenuMode.Main;
			//menu.gameObject.SetActive(false);
			springMenu.minYaw = -7;
			springMenu.maxYaw = 7;
			labsTitleAlphaTarget = 1;
			hikeTitleAlphaTarget = 1;
			if(instant){
				labsTitleAlpha = 1;
				hikeTitleAlpha = 1;
				
			}
			//Debug.Log("labs");
		}
		if(mode == MenuMode.Hike){
			
			labs.Invisible = false;
			mode = MenuMode.Main;
			//hikeMenu.gameObject.SetActive(false);
			springMenu.minYaw = -7;
			springMenu.maxYaw = 7;
			labsTitleAlphaTarget = 1;
			hikeTitleAlphaTarget = 1;
			if(instant){
				labsTitleAlpha = 1;
				hikeTitleAlpha = 1;

			}
			//Debug.Log("labs");
		}
	}

	public override void ExitState(SceneManager context){
		base.ExitState(context);
	}



	/// <summary>
	/// There is a lot of code in here, but it's really just controlling the visual effect in the menu, and a little bit of logic.  There are also responders for click events in if blocks.  Easy enough.
	/// </summary>
	public override void DoUpdate(SceneManager context){



		if(mode == MenuMode.Main){
			if(labs.IsClicked){
				hike.Invisible = true;
				mode = MenuMode.Labs;
				menu.gameObject.SetActive(true);
				hikeMenu.gameObject.SetActive(false);
				hikeTitleAlphaTarget = -1;
				labsTitleAlphaTarget = -1f;
				springMenu.minYaw = springMenu.maxYaw = -7;
				//Debug.Log("labs");
			}

			if(hike.IsClicked){
				labs.Invisible = true;
				mode = MenuMode.Hike;
				hikeMenu.gameObject.SetActive(true);
				menu.gameObject.SetActive(false);
				labsTitleAlphaTarget = -1;
				hikeTitleAlphaTarget = -1f;
				springMenu.minYaw = springMenu.maxYaw = 7;
				//Debug.Log("labs");
			}
		}
		
		if(mode == MenuMode.Labs  || (mode == MenuMode.Main && labs.IsTarget ) ){//|| (mode == MenuMode.Main && (labs.IsTarget ||  (mode == MenuMode.Main && springMenu.Direction == -1 ) ) )

		//	realTarget = 1;
			if(fadeValue < 0){
				spring = 0.75f;
				//targetFade = 0;
			}
			else{
				spring = 0.25f;
				//targetFade = 1;
			}
			targetFade = 1;
		}
		else if(mode == MenuMode.Hike|| (mode == MenuMode.Main && hike.IsTarget)    ){//|| (mode == MenuMode.Main && ( hike.IsTarget || (mode == MenuMode.Main && springMenu.Direction == 1) ) )
			//realTarget = -1;
			if(fadeValue > 0){
				spring = 0.75f;
				//targetFade = 0;
			}
			else{
				spring = 0.25f;
				//targetFade = -1;
			}
			targetFade = -1;
		}
		else{
			targetFade = 0;
			//realTarget = 0;
		}


		if(mode == MenuMode.Labs){
			if(back.IsClicked){
				hike.Invisible = false;
				mode = MenuMode.Main;
				//menu.gameObject.SetActive(false);
				springMenu.minYaw = -7;
				springMenu.maxYaw = 7;
				labsTitleAlphaTarget = 1;
				hikeTitleAlphaTarget = 1;
				//Debug.Log("labs");
			}
			else if(targeting.IsClicked){
				SceneManager.Instance.StateTransition(SceneManager.Instance.targetScene);
				//Debug.Log("labs");
			}
			else if(motion.IsClicked){
				SceneManager.Instance.StateTransition(SceneManager.Instance.minecartScene);
				//Debug.Log("labs");
			}
			else if(text.IsClicked){
				SceneManager.Instance.StateTransition(SceneManager.Instance.visionScene);
				//Debug.Log("labs");
			}
			else if(relativity.IsClicked){
				SceneManager.Instance.StateTransition(SceneManager.Instance.relativityScene);
				//Debug.Log("labs");
			}
			else if(tracking.IsClicked){
				SceneManager.Instance.StateTransition(SceneManager.Instance.trackingScene);
				//Debug.Log("labs");
			}
		}
		
		if(mode == MenuMode.Hike){
			if(hikeBack.IsClicked){
				labs.Invisible = false;
				mode = MenuMode.Main;
				//hikeMenu.gameObject.SetActive(false);
				springMenu.minYaw = -7;
				springMenu.maxYaw = 7;
				labsTitleAlphaTarget = 1;
				hikeTitleAlphaTarget = 1;
				//Debug.Log("labs");
			}
			else if(hike1.IsClicked){

				SceneManager.Instance.StateTransition(SceneManager.Instance.redwoodScene,Color.black, 0);
			}
			else if(hike2.IsClicked){
				SceneManager.Instance.StateTransition(SceneManager.Instance.redwoodScene,Color.black,1 );
			}
			else if(hike3.IsClicked){
				SceneManager.Instance.StateTransition(SceneManager.Instance.redwoodScene,Color.black, 2);
			}
			else if(hike4.IsClicked){
				SceneManager.Instance.StateTransition(SceneManager.Instance.redwoodScene,Color.black, 3);
			}
			else if(hike5.IsClicked){
				SceneManager.Instance.StateTransition(SceneManager.Instance.redwoodScene,Color.black, 4);
			}
		}

		//if( Mathf.Abs(fadeValue) < 0.05f ){
		//	targetFade = realTarget;
		//}

		hikeTitleAlpha = Smoothing.SpringSmooth(hikeTitleAlpha, hikeTitleAlphaTarget, ref hikeTitleAlphaSpeed, 0.4f, Time.deltaTime);
		labsTitleAlpha = Smoothing.SpringSmooth(labsTitleAlpha, labsTitleAlphaTarget, ref labsTitleAlphaSpeed, 0.4f, Time.deltaTime);

		Color col = labTitle.color;
		col.a = labsTitleAlpha;
		labTitle.color = col;

		
		col = hikeTitle.color;
		col.a = hikeTitleAlpha;
		hikeTitle.color = col;
		

		col = labIcon.material.color;
		col.a = labsTitleAlpha;
		labIcon.material.color = col;

		col = hikeIcon.material.color;
		col.a = hikeTitleAlpha;
		hikeIcon.material.color = col;

		if(hikeMenu.gameObject.activeSelf){
			if(mode != MenuMode.Hike && -hikeTitleAlpha < 0){
				hikeMenu.gameObject.SetActive(false);
			}
			else{
				for(int i=0; i<hikeListItems.Length; i++){
					col = hikeListItems[i].material.color;
					col.a = Mathf.Clamp01(-hikeTitleAlpha);
					hikeListItems[i].material.color = col;
				}
			}
			hike1.PlateAlpha = Mathf.Clamp01(-hikeTitleAlpha);
			hike2.PlateAlpha = Mathf.Clamp01(-hikeTitleAlpha);
			hike3.PlateAlpha = Mathf.Clamp01(-hikeTitleAlpha);
			hike4.PlateAlpha = Mathf.Clamp01(-hikeTitleAlpha);
			hike5.PlateAlpha = Mathf.Clamp01(-hikeTitleAlpha);

		}
		if(menu.gameObject.activeSelf){
			if(mode != MenuMode.Labs && -labsTitleAlpha < 0){
				menu.gameObject.SetActive(false);
			}
			else{
				for(int i=0; i<labsListItems.Length; i++){
					col = labsListItems[i].material.color;
					col.a = Mathf.Clamp01(-labsTitleAlpha);
					labsListItems[i].material.color = col;
				}
			}

			motion.PlateAlpha = Mathf.Clamp01(-labsTitleAlpha);
			targeting.PlateAlpha = Mathf.Clamp01(-labsTitleAlpha);
			tracking.PlateAlpha = Mathf.Clamp01(-labsTitleAlpha);
			text.PlateAlpha = Mathf.Clamp01(-labsTitleAlpha);
			relativity.PlateAlpha = Mathf.Clamp01(-labsTitleAlpha);



		}
		
		

		if(fadeValue != targetFade){
			fadeValue = Smoothing.SpringSmooth(fadeValue, targetFade, ref fadeSpeed, spring, Time.deltaTime);
			ResetUIColor();

			if(fadeValue > 0){
				for(int i=0; i<labsRenderers.Length; i++){
					labsRenderers[i].enabled = true;
					Color c = labsRenderers[i].material.color;
					c.a = fadeValue;
					labsRenderers[i].material.color = c;
				}
				for(int i=0; i<redwoodsRenderers.Length; i++){
					if(redwoodsRenderers[i] != null){
						redwoodsRenderers[i].enabled = false;
						//Color c = redwoodsRenderers[i].material.color;
						//c.a = 0;
						//redwoodsRenderers[i].material.color = c;
					}
				}
			}
			else if(fadeValue < 0){
				for(int i=0; i<labsRenderers.Length; i++){
					labsRenderers[i].enabled = false;
					//Color c = labsRenderers[i].material.color;
					//c.a = 0;
					//labsRenderers[i].material.color = c;
				}
				for(int i=0; i<redwoodsRenderers.Length; i++){
					if(redwoodsRenderers[i] != null){
						redwoodsRenderers[i].enabled = true;
						Color c = redwoodsRenderers[i].material.color;
						c.a = -fadeValue;
						redwoodsRenderers[i].material.color = c;
					}
				}
			}
			else{
				for(int i=0; i<labsRenderers.Length; i++){
					labsRenderers[i].enabled = false;
				}
				for(int i=0; i<redwoodsRenderers.Length; i++){
					if(redwoodsRenderers[i] != null){
						redwoodsRenderers[i].enabled = false;
					}
				}
			}
		}

		//// If the user rotates the screen too far away from the sub-menu, fade back to the splash screen
		if( Mathf.Abs(Mathf.DeltaAngle(OrientationYaw, VRInput.Instance.Yaw)) > 50){
			MainMenu();
			SceneManager.Instance.StateTransition(SceneManager.Instance.splashScene);
		}
		if( Mathf.Abs(Mathf.DeltaAngle(0, VRInput.Instance.Pitch)) > 35){
			MainMenu();
			SceneManager.Instance.StateTransition(SceneManager.Instance.splashScene);
		}
	}
}
