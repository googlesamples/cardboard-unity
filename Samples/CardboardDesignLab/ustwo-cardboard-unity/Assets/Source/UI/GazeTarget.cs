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

public abstract class GazeTarget : MonoBehaviour{



	bool isTarget = false;
	int gazeStartFrame = -1;
	float gazeStartTime = -1;
	int gazeExitFrame = -1;
	float gazeExitTime =-1;
	VRInput input;

	int clickedFrame = -1;

	int lastUpdateFrame = -1;

	///This is the ability to split entry and exit into separate colliders
	///TODO: figure out a way to do this intuitively
	[SerializeField] Collider entryTarget;
	[SerializeField] Collider exitTarget;
	[SerializeField] bool invisible = false; 
	[SerializeField] bool clickSound = false;

	//if the target hides the cursor, should be if it's clickable, but we got too far along, I don't want to find all the instances in this project.
	public bool Invisible{
		get{
			return invisible;
		}
		set{
			invisible = value;
		}
	}

	public Vector3 Position{
		get{
			return transform.position;
		}
	}

	public void Gaze(VRInput context){
		input = context;
		if(context.Target != this){
			context.Target = this;
			
		}
		
	}
	public int GazeStartFrame{
		get{
			return gazeStartFrame;
		}
	}
	public int GazeExitFrame{
		get{
			return gazeExitFrame;
		}
	}	
	public float GazeExitTime{
		get{
			return gazeExitTime;
		}
	}
	public bool IsTargetThisFrame{
		get{
			if(lastUpdateFrame != Time.frameCount){
				DoUpdate();
			}
			return gazeStartFrame == Time.frameCount;
		}
	}
	public bool IsTarget{
		get{
			if(lastUpdateFrame != Time.frameCount){
				DoUpdate();
			}
			return isTarget;
		}
	}
	public bool IsClicked{
		get{
			if(lastUpdateFrame != Time.frameCount){
				DoUpdate();
			}
			return clickedFrame == Time.frameCount;
		}
	}


	protected virtual void Update(){
		if(lastUpdateFrame != Time.frameCount){
			DoUpdate();
		}
	}

	///we need to make sure this is called after the VRInput updates...
	protected virtual void DoUpdate(){
		lastUpdateFrame = Time.frameCount;

		if(input != null){
			if(input.Target == this){
				if(isTarget){
					ContinueGaze();
				}
				else{
					if(input.TargetCollider == entryTarget){
						BeginGaze();
					}
					
				}

				VRButton btn = input.PrimaryButton;
				VRButton.VRButtonState buttonState = btn.State;
				switch(buttonState){
					case VRButton.VRButtonState.Down:
						Click(btn);
					break;

					case VRButton.VRButtonState.Up:
						OnButtonUp(btn);
					break;

					case VRButton.VRButtonState.Held:
						OnButtonHeld(btn);
					break;

					default:
					case VRButton.VRButtonState.None:

					break;
				}
			}
			else{
				if(isTarget){
					EndGaze();
				}
				///otherwise, we aren't the target and never were.
			}
			
		}
	}

	private void Click(VRButton btn){
		clickedFrame = Time.frameCount;
		if(clickSound){
			if(UISounds.Instance != null){
				UISounds.Instance.PlayClick();
			}
		}
		OnButtonDown(btn);
	}

	private void BeginGaze(){
		gazeStartFrame = Time.frameCount;
		gazeStartTime = Time.time;
		isTarget = true;
		OnGazeEnter();
	}

	private void EndGaze(){
		gazeExitFrame = Time.frameCount;
		gazeExitTime = Time.time;
		isTarget = false;
		OnGazeExit(Time.time - gazeStartTime);
	}

	private void ContinueGaze(){
		float gazeTime = Time.time - gazeStartTime;
		OnGaze(gazeTime);

	}
	protected virtual void OnGazeEnter(){

	}
	protected virtual void OnGaze(float gazeDuration){

	}
	protected virtual void OnGazeExit(float gazeDuration){
		
	}

	protected virtual void OnGazeActivate(){

	}

	protected virtual void OnButtonDown(VRButton button){
		
	}
	protected virtual void OnButtonUp(VRButton button){
		
	}
	protected virtual void OnButtonHeld(VRButton button){
		
	}
	//void OnEnable(){
		///just to be overly cautious, let's make sure we never ever double add, even though .Contains is O(n)
		//if( !targets.Contains(this) ){
		//	targets.Add(this);
		//}
	//}

	void OnDisable(){
		if(isTarget){
			EndGaze();
		}


		///It's O(n), but less than 100 elements, probably, so who cares
		//targets.Remove(this);
	}

}
