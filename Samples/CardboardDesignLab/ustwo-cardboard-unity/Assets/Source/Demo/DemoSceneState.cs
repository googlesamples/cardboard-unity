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
/// The base state class.
/// Holds a reference to a spawn point, if any exists - as well as the current scene 'orientation'.  This allows the state machine to reorient the forward direction on scene changes, so the user is always facing 'scene forward' when we fade up.
/// </summary>
public abstract class DemoSceneState : MonoBehaviour {


	[SerializeField] Transform orientationRoot;
	[SerializeField] protected MovementGazeTarget spawnPoint;
	public virtual MovementGazeTarget SpawnPoint{
		get{
			return spawnPoint;
		}
	}

	float orientationYaw = 0;
	public float OrientationYaw{
		get{
			return orientationYaw;
		}
	}

	/// <summary>
	/// Allows the blackout clearing to be overriden by the individual states.  
	/// </summary>
	public virtual void ClearBlackout(){
		Blackout.Instance.TargetBlackoutValue = 0;
	}


/// <summary>
/// Set the current 'forward' direction to the specified yaw
/// </summary>
	public void OrientateScene(float yaw){
		if(orientationRoot != null){
			orientationYaw = yaw;
			orientationRoot.localRotation = Quaternion.AngleAxis(yaw, Vector3.up);
		}
	}

	public virtual float GetOrientationOffset(int idx){
		return 0;
	}

	public virtual void EnterState(SceneManager context, int idx = 0){
		gameObject.SetActive(true);
	}

	public virtual void BeginExitState(SceneManager context){

	}
	public virtual void ExitState(SceneManager context){
		gameObject.SetActive(false);
	}

	public abstract void DoUpdate(SceneManager context);
}
