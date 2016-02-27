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
/// Simple state machine for handling states, state transitions and input
/// </summary>

public class SceneManager : MonoBehaviour {

	static SceneManager instance;
	public static SceneManager Instance{
		get{return instance;}
	}

	[SerializeField] public DemoSceneState splashScene;
	[SerializeField] public DemoSceneState menuScene;

	[SerializeField] public DemoSceneState targetScene;
	[SerializeField] public DemoSceneState minecartScene;
	[SerializeField] public DemoSceneState trackingScene;
	[SerializeField] public DemoSceneState relativityScene;
	[SerializeField] public DemoSceneState visionScene;

	[SerializeField] public DemoSceneState redwoodScene;

	[SerializeField] PlayerMover player;
	//[SerializeField] CardboardHead cardboardHead;

	private DemoSceneState currentState;
	private DemoSceneState nextState;
	private bool pendingTransition = false;

	float transitionTime = 0.75f;

	/// <summary>
	/// Are we transitioning to another state?
	/// </summary>
	public bool PendingTransition{
		get{
			return pendingTransition;
		}
	}

	/// <summary>
	/// What is the current 'forward' direction, based on the current state
	/// </summary>
	public float OrientationYaw{
		get{
			if(currentState != null){
				return currentState.OrientationYaw;
			}
			return 0;
		}
	}



	int transitionIdx = 0;

	float transitionTimer = 0;

	/// <summary>
	/// Transition to another state.  Re-entry IS permitted to allow for resetting state.  Optional parameter IDX allows for some flags to be passed to the state from the UI (for example entering a specific section of a state)
	/// </summary>
	public void StateTransition(DemoSceneState targetState, int idx = 0){
		StateTransition(targetState, Color.white, idx);
	}


	/// <summary>
	/// Transition to another state.  Re-entry IS permitted to allow for resetting state. Takes a transition color to control the fade color.  Optional parameter IDX allows for some flags to be passed to the state from the UI (for example entering a specific section of a state)
	/// </summary>
	public void StateTransition(DemoSceneState targetState,Color c, int idx = 0){

		///Make sure we aren't mid-transition - primarily because of graphical glitches
		if(!pendingTransition ){

			///First we begein exiting the current state.
			currentState.BeginExitState(this);

			///Update the target indices
			transitionIdx = idx;
			nextState = targetState;

			///we are transitioning, so yeah, flag that, too
			pendingTransition = true;

			///start our fade to (color c)
			Blackout.Instance.SetColor(c);
			Blackout.Instance.TargetBlackoutValue = 1;

			///start a timer to end the transition.  This used to wait on blackout, but that actually caused a seriously scary bug when there was a feature request to fade to black at other times.  You were able to start a transition, and get stuck - so instead we maintain our own separate timer.
			///if the blackout speed changes later, this time will need to be adjusted.  Ideally, it should read from a public blackout time, or better yet - blackout could accept a transition speed variable - but that still can be risky for multiple entry on the blackout side
			transitionTimer = transitionTime;
			
		}
	}

	void Awake(){
		instance = this;
		currentState = splashScene;
		currentState.EnterState(this);
		Blackout.Instance.TargetBlackoutValue = 0;
	}

	////activate blackout between scenes
	///activate, deactivate, reset messages
	///use onEnable, onDisable?
	//// need to know when we are ready to transition, maybe call a start cleanup, as fades begin




	void Update(){

		
		if(pendingTransition){
			
			transitionTimer -= Time.deltaTime;

			///To smoothly transition audio, we fade the listener down while transitioning
			AudioListener.volume = Mathf.Clamp01(transitionTimer / transitionTime);

			/// We're ready to actually transition - the screen should be 100% opaque (white, black, or some other color)
			if(transitionTimer <= 0){

			/// WARNING - this was the previous way of handling this - but waiting on UI is bad practice, and it stayed in the code too long.  I leave this here as a reminder to whoever looks at this - please, don't wait on UI transitions that can be controlled from multiple sources
			//if(Blackout.Instance.BlackoutValue == 1){

				pendingTransition = false;

				/// Exit the current state
				currentState.ExitState(this);
				currentState = nextState;

				/// since the screen is opaque, we can rotate the entire scene so that forward is the direction the user is currently facing
				/// certain scenes may have orientation offsets based on the transition index (in the hike, certain nodes have you spawn at different angles)
				float orientationOffset = currentState.GetOrientationOffset(transitionIdx);
				currentState.OrientateScene(orientationOffset + VRInput.Instance.Yaw);//+ VRInput.Instance.Yaw

				/// we're good to load up the next scene, at the correct angle
				currentState.EnterState(this, transitionIdx);

				/// if we have a spawn point, move the player there
				if(currentState.SpawnPoint != null){
					player.SetTarget(currentState.SpawnPoint);
				}
				
				/// start fading the scene back in
				currentState.ClearBlackout();

			}
		}
		else{

			///To smoothly transition audio, we fade the listener back up
			if(AudioListener.volume <1){
				AudioListener.volume = Mathf.Clamp01(AudioListener.volume + 2*Time.deltaTime);
			}
			//#if UNITY_EDITOR
				//StateTransitionDebug();

			//#endif
		}
		if(currentState != null){
			currentState.DoUpdate(this);
		}

		if(Cardboard.SDK.BackButtonPressed){
			Application.Quit ();
		}
	}


	////Perform UI stuff last, since cardboard updates in late update - we need to script execution order the priority of this after everything else

	void LateUpdate(){
		

		///	record the time we've been staring at an element, gazeIn, gazeOut, gazeDuration, onClick (begin / release? plz?)
		///modals?  Yes / No  up, side to side

		///hot 'regions'?  up / down?  screen sides?  Hot corners?

		///Current relative center, in yaw, or spherical coordinates?  Lying down in bed?

	}
	
	/*
	void StateTransitionDebug(){

		int transition = -1;
		if(Input.GetKeyDown(KeyCode.Alpha1)){
			transition = 1;
			StateTransition(menuScene);
		}
		else if(Input.GetKeyDown(KeyCode.Alpha2)){
			transition = 2;
			StateTransition(targetScene);
		}
		else if(Input.GetKeyDown(KeyCode.Alpha3)){
			transition = 3;
			StateTransition(minecartScene);
		}
		else if(Input.GetKeyDown(KeyCode.Alpha4)){
			transition = 4;
			StateTransition(trackingScene);
		}
		else if(Input.GetKeyDown(KeyCode.Alpha5)){
			transition = 5;
			StateTransition(relativityScene);
		}
		
		else if(Input.GetKeyDown(KeyCode.Alpha7)){
			transition = 7;
			StateTransition(visionScene);

		}
		//Debug.Log(transition);
	}*/
}
