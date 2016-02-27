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

public class PlayerMover : MonoBehaviour {

	private static PlayerMover instance;
	public static PlayerMover Instance{
		get{return instance;}
	}


	Vector3 previousPosition;
	Vector3 targetPosition;
	float distance;
	float blend = 0;
	float smoothedBlend=0;
	public float SmoothedBlend{
		get{
			return smoothedBlend ;
		}
	}
	bool moving = false;


	MovementGazeTarget currentTarget;
	[SerializeField] MovementGazeTarget spawnPoint;

	public MovementGazeTarget CurrentTarget{
		get{
			return currentTarget;
		}
	}

	public bool Moving{
		get{
			return moving;
		}
	}

	public void SetTarget(MovementGazeTarget target){
		SetTargetPosition(target.transform.position,target, true);
	}

	void Awake () {
		instance = this;
		if(spawnPoint != null){
			SetTargetPosition(spawnPoint.transform.position,spawnPoint, true);
		}
	}

	public void ClearTarget(){
		moving = false;
		currentTarget = null;
	}

	public Vector3 Position{
		get{
			return transform.position;
		}
		set{

			transform.position = value;
		}
	}
	public Quaternion Rotation{
		get{
			return transform.rotation;
		}
		set{

			transform.rotation = value;
		}
	}

	public void SetTargetPosition(Vector3 pos, MovementGazeTarget target, bool immediateUpdate = false){
		if(!moving || immediateUpdate){
			currentTarget = target;
			targetPosition = pos;// - new Vector3(0,1.8f,0);
			distance = (targetPosition - previousPosition).magnitude;
			if(immediateUpdate){
				moving = false;
				previousPosition = targetPosition;
				transform.position = targetPosition;
			}
			else{
				///these are poorly named - not really 'time' more like normalized time
				smoothInTime = Mathf.Clamp(smoothInPercent/distance,0,0.5f);
				smoothOutTime = Mathf.Clamp(smoothOutPercent/distance,0,0.5f);
				linearTime = 1f - smoothInTime - smoothOutTime;
				smoothedBlend = blend = 0;
				moving = true;
			}
		}
	}


	const float smoothInPercent = 0.5f;
	const float smoothOutPercent = 1.5f;
	float smoothInTime;
	float smoothOutTime;
	float linearTime ;//= 1f - smoothInTime - smoothOutTime;
	void Update () {
		if(moving){
			//velocity = Mathf.Clamp(velocity + 2f*Time.deltaTime, 0, maxVelocity);
			if(blend < 1){

				blend = Mathf.Clamp01(blend + currentTarget.MovementSpeed*Time.deltaTime/distance);
			}
			//float smoothedBlend = blend;//Smoothing.CubicEaseInOut(blend);
			if(blend < smoothInTime){
				//if(!SceneManager.Instance.PendingTransition){
				//	Blackout.Instance.TargetBlackoutValue = 1;
				//	Blackout.Instance.SetFogFade(true);
				//}
				//v = 3x^2
				//smoothedBlend = 0;
				smoothedBlend = smoothInTime*Smoothing.CubicEaseIn(blend/smoothInTime);
			}
			else if(blend > (1f-smoothOutTime)){
				
				smoothedBlend = smoothInTime + (linearTime)*3f + smoothOutTime*Smoothing.CubicEaseOut( (blend-(1-smoothOutTime))/smoothOutTime );
				//smoothedBlend = 0.9f + 0.1f*Smoothing.CubicEaseOut(10f*(blend-0.9f));
			}
			else{
				
				smoothedBlend = smoothInTime + (blend - smoothInTime)*3f;
			}

			if(blend > 1.5f* smoothInTime || blend > 0.5){
				//if(!SceneManager.Instance.PendingTransition){
				//	Blackout.Instance.TargetBlackoutValue = 0;
				//	Blackout.Instance.SetFogFade(true);
				//}
			}

			smoothedBlend = smoothedBlend / (smoothInTime + smoothOutTime + 3f*linearTime);

			if(smoothedBlend < 0.001f){
				smoothedBlend = 0;
			}
			else if (smoothedBlend > 0.999f){
				smoothedBlend = 1;
			}

			transform.position = (1-smoothedBlend)*previousPosition + smoothedBlend*targetPosition;

			if(blend >= 1){
				moving = false;
				previousPosition = targetPosition;
				//velocity = 0;
			}
		}
	}
}
