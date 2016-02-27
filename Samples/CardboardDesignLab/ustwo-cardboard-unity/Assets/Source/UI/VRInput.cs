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

public class VRInput : MonoBehaviour {

	static VRInput instance;
	public static VRInput Instance{get {return instance;}}

	[SerializeField] Reticle3D reticle;
	[SerializeField] Cardboard cardboard;
	[SerializeField] CardboardHead cardboardHead;

	Ray currentRay;
	float raycastLength = 100;

	string UILayer = "VRUI";
	string GeometryLayer = "VRGEO";
	int layerMask;

	string message = "Gaze";

	GazeTarget target = null;
	Collider targetCollider = null;

	VRButton primaryButton = new VRButton();

	float yaw;
	float pitch;
	Quaternion rotation;


	public float reticlePointAlpha;
	public float reticleRingAlpha;
	public float reticleRotationPercent;

	float reticleTransition = 0;
	float reticleTransitionSpeed = 0;
	float reticleTransitionTarget = 0;

	public float MaxReticleDistance{
		get{
			return reticle.MaxReticleDistance;
		}
		set{
			reticle.MaxReticleDistance = value;
		}
	}

	public float Yaw{
		get{
			return yaw;
		}
	}

	public float Pitch{
		get{
			return pitch;
		}
	}
	public Quaternion Rotation{
		get{
			return rotation;
		}
	}
	public Vector3 Position{
		get{
			return transform.position;
		}
	}
	public Ray Ray{
		get{
			return currentRay;
		}
		//set{
		//	currentRay = value;
		//}
	}



	public GazeTarget Target{
		get{return target;}
		set{
			target = value;
		}
	}

	public Collider TargetCollider{
		get{
			return targetCollider;
		}
	}

	public VRButton PrimaryButton{
		get{
			return primaryButton;
		}
	}

	void Awake(){
		instance = this;
		layerMask = 1 << LayerMask.NameToLayer(UILayer) | 1 << LayerMask.NameToLayer(GeometryLayer);
		//UpdateReticleColor();
	}


	void Update(){
		UpdateAxes();
		UpdateButtons();
		UpdateGaze();
		UpdateReticle();

	}

	void UpdateAxes(){
		Vector3 direction = cardboardHead.Gaze.direction;
		rotation = cardboardHead.transform.rotation;
		yaw = (Mathf.Rad2Deg*Mathf.Atan2(direction.x, direction.z) + 360)%360;
		pitch = Mathf.Rad2Deg*Mathf.Asin(direction.y);
	}

	void UpdateButtons(){

		primaryButton.UpdateButton(cardboard.Triggered);
	}


	void UpdateGaze(){
		currentRay = cardboardHead.Gaze;
		Debug.DrawRay(currentRay.origin, 10*currentRay.direction,Color.red);

		target = null;
		targetCollider = null;
		RaycastHit hit;

		
		if (Physics.Raycast(currentRay,out hit, raycastLength, layerMask)){
			//Debug.Lo
			hit.transform.SendMessage(message,this, SendMessageOptions.DontRequireReceiver);
        	targetCollider = hit.collider;

        	reticle.SetTarget(hit.point);
		}	
		else{
			reticle.SetDistance(100f);
		}
		///We're using a spherecast instead of a ray so this doesn't jump around too much
        //if (Physics.SphereCast(currentRay, 0.1f,out hit, raycastLength, layerMask)){
        //	reticle.SetTarget(hit.point);
        	
        //}
       // else{
       	
        //}
	}

	Color reticleColor = Color.white;
	public Color ReticleColor{
		get{
			return reticleColor;
		}
		set{
			reticleColor = value;
			UpdateReticleColor();
		}
	}

	public void ForceReticleColor(Color c){
		reticleColor = c;
		reticle.SetColor(c);
		reticle.SetAndApplyColor(c);
	}
	public void SetReticleColor(Color c){
		reticle.SetAndApplyColor(c);
	}

	void UpdateReticleColor(){
		reticle.SetColor(reticleColor);
	}

	void UpdateReticle(){
		//if( target!= null && reticle != null){
			//reticle.SetTarget(target.Position);
			reticle.UpdateReticlePosition();

			if(target == null || target.Invisible){
				reticlePointAlpha = 1;
				reticleRingAlpha = 0;
				reticleTransitionTarget = 0;
			}
			else{
				reticlePointAlpha = 0;
				reticleRingAlpha = 1;
				
				reticleTransitionTarget = 1;
			}
			reticleRotationPercent = 1;
			reticleTransition = Smoothing.SpringSmooth(reticleTransition, reticleTransitionTarget, ref reticleTransitionSpeed, 0.1f, Time.deltaTime);

			Shader.SetGlobalVector("_Reticle", new Vector4(Mathf.Clamp01(1-3f*reticleTransition),reticleTransition, reticleRotationPercent,(1.0f-reticleTransition)*0.7f ) );
		//}

	}


}

public struct VRButton{

	public enum VRButtonState{None, Down, Up, Held}

	private bool pressed;
	private float buttonDownTime ;
	private float buttonUpTime;
	VRButtonState state;

	/*public VRButton(){
		pressed = false;
		buttonDownTime = -1;
		buttonUpTime = -1;
		state = VRButtonState.None;
	}*/


	public bool Pressed{
		get{
			return pressed;
		}
	}
	public float HeldTime{
		get{
			if(pressed){
				return Time.time - buttonDownTime;
			}
			return -1;
		}
	}
	public VRButtonState State{
		get{
			return state;
		}
	}
	public float ReleaseTime{
		get{
			return buttonUpTime;
		}
	}

	//returns true if the button has changed state, and we need to signal an 
	public VRButtonState UpdateButton(bool buttonState){

		if(pressed != buttonState){
			pressed = buttonState;
			if(pressed){
				buttonDownTime = Time.time;
				state = VRButtonState.Down;

				return state;
			}
			else{
				buttonUpTime = Time.time;
				state = VRButtonState.Up;

				return state;
			}
			
		}
		else if(pressed){
			state = VRButtonState.Held;

			return state;
		}
		else{
			state = VRButtonState.None;

			return state;
		}
		
			
	}

}