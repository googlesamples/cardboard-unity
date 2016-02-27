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
/// The minecart scene is a bunch of coroutines controlled by two animations, as well as the blackout.  Audio needs to kind of sync.  This isn't great - it's fragile, but there wasn't a lot to be done.  Sequence A and B are virtually identical functions, but they are left as separate code paths, since it seemed very likely they would not remain similar.
/// Script execution order ensures this script moves the player before the camera updates.
/// </summary>
public class MinecartScene : DemoSceneState {

	//public Minecart cartA;
	//public Minecart cartB;
	[SerializeField] GazeTarget menu; 
	[SerializeField] GazeTarget restart;
	[SerializeField] GazeTarget nextLab; 

	[SerializeField] GazeTarget minecartA; 
	[SerializeField] GazeTarget minecartB; 
	[SerializeField] GazeTarget minecartCartA; 
	[SerializeField] GazeTarget minecartCartB; 

	[SerializeField] AudioSource minecartASound;
	[SerializeField] AudioSource minecartBSound;
	//[SerializeField] AudioSource minecartAStopSound;
	//[SerializeField] AudioSource minecartBStopSound;

	[SerializeField] AmbientAudioTarget ambientTrack = new AmbientAudioTarget();

	enum MinecartSequence{None, A,AMoving, B, BMoving};
	private MinecartSequence currentSequence = MinecartSequence.None;

	public Animation minecartA_Animation;
	public Animation minecartB_Animation;

	public float endAStart = 0.6f;
	public float endBStart = 0.7f;

	bool rodeA;
	bool rodeB;


	public override void EnterState(SceneManager context, int idx = 0){
		base.EnterState(context);
		SoundManager.Instance.SetAmbientTrack(0, ambientTrack.data,ambientTrack.targetVolume );
		currentSequence = MinecartSequence.None;
		ResetMinecarts();
		VRInput.Instance.ReticleColor = new Color(0,0,0,0.5f);
		rodeA = false;
		rodeB = false;
	}

	public override void ExitState(SceneManager context){
		currentSequence = MinecartSequence.None;
		base.ExitState(context);
	}

	public override void DoUpdate(SceneManager context){
		//if((VRInput.Instance.Pitch) < -40){
				
		//	VRInput.Instance.ReticleColor = new Color(0.2f,0.2f,0.2f,1);
		//}
		//else{
		//	VRInput.Instance.ReticleColor = new Color(0,0,0,0);
		//}

		if(menu.IsClicked){
			SceneManager.Instance.StateTransition(SceneManager.Instance.menuScene);
		}
		else if(restart.IsClicked){
			SceneManager.Instance.StateTransition(SceneManager.Instance.minecartScene);
		}
		else if(nextLab.IsClicked){
			SceneManager.Instance.StateTransition(SceneManager.Instance.relativityScene);
		}

		if(currentSequence == MinecartSequence.None){
			if(minecartA.IsClicked || minecartCartA.IsClicked){
				currentSequence = MinecartSequence.A;
				StartCoroutine(MinecartSequenceA());
			}
			else if(minecartB.IsClicked || minecartCartB.IsClicked){
				currentSequence = MinecartSequence.B;
				StartCoroutine(MinecartSequenceB());
			}
		} 


	}

	/// <summary>
/// Move the player to the current minecart position, if riding a minecart.  Script execution order must ensure this happens prior to the camera's update loop.
/// </summary>
	void LateUpdate(){
		if(currentSequence == MinecartSequence.AMoving){
			PlayerMover.Instance.Position = minecartA_Animation.transform.position + new Vector3(0,1.5f,0);
			PlayerMover.Instance.Rotation = minecartA_Animation.transform.localRotation;
		}
		if(currentSequence == MinecartSequence.BMoving){
			PlayerMover.Instance.Position = minecartB_Animation.transform.position + new Vector3(0,1.5f,0);
			PlayerMover.Instance.Rotation = minecartB_Animation.transform.localRotation;
		}

		if(rodeA && rodeB){
			StartCoroutine(Popup());
			rodeB = false;
			rodeA = false;
		}
	}


	/// <summary>
/// Trigger the popup complete toast after a delay
/// </summary>
	IEnumerator Popup(){
		yield return new WaitForSeconds(1.25f);
		if(Toaster.Instance != null){
			Toaster.Instance.Toast();
		}
	}


	IEnumerator MinecartSequenceA(){

		///Fade out the scene
		Blackout.Instance.TargetBlackoutValue = 1;
		while( Blackout.Instance.BlackoutValue != 1){
			yield return 0;
		}

		///Move the player into the minecart
		PlayerMover.Instance.ClearTarget();
		PlayerMover.Instance.Position = minecartA_Animation.transform.position + new Vector3(0,1.5f,0);
		currentSequence = MinecartSequence.AMoving;
		
		///Fade the scene back in
		Blackout.Instance.TargetBlackoutValue = 0;
		while( Blackout.Instance.BlackoutValue != 0){
			yield return 0;
		}

		///Start the minecart animation, and sound
		minecartA_Animation.Play();
		minecartASound.loop = true;
		minecartASound.Play();

		///track last position so we can calculate speed, for audio pitch shifting
		lastPosition = PlayerMover.Instance.Position;

		///while the animation is still playing, twiddle the sound
		while(minecartA_Animation.IsPlaying(minecartA_Animation.clip.name)){
			float distance = (PlayerMover.Instance.Position - lastPosition).magnitude;
			float speed = distance / Time.deltaTime;
			//Debug.Log(speed);
			minecartASound.pitch = 0.75f  + 0.5f*Mathf.Clamp01(speed/20f);
			lastPosition = PlayerMover.Instance.Position;

			float tiempo = minecartA_Animation[minecartA_Animation.clip.name].normalizedTime;
			if(tiempo > endAStart){
				//if(minecartAStopSound.isPlaying == false){
				//	minecartAStopSound.Play();
				//}
			}
			yield return 0;
		}

		///Once we are done moving, stop the sound and fade the scene out
		minecartASound.Stop();
		Blackout.Instance.TargetBlackoutValue = 1;
		while( Blackout.Instance.BlackoutValue != 1){
			yield return 0;
		}
		
		///Once we've faded out, reset the minecart, and move the player back
		currentSequence = MinecartSequence.None;
		//minecartA_Animation.Rewind(minecartA_Animation.clip.name);
		PlayerMover.Instance.SetTargetPosition(new Vector3(0,1.5f,0), spawnPoint, true);

		minecartA_Animation[minecartA_Animation.clip.name].time = 0f;
		minecartA_Animation[minecartA_Animation.clip.name].weight = 1;
		minecartA_Animation[minecartA_Animation.clip.name].enabled = true;
		minecartA_Animation.Sample();
		minecartA_Animation[minecartA_Animation.clip.name].enabled = false;
		PlayerMover.Instance.Rotation = Quaternion.identity;
		
		/// fade us back in
		Blackout.Instance.TargetBlackoutValue = 0;
		while( Blackout.Instance.BlackoutValue != 0){
			yield return 0;
		}

		///yeah, and mark a completion flag
		rodeA = true;
	}


	Vector3 lastPosition;

	IEnumerator MinecartSequenceB(){
		
		Blackout.Instance.TargetBlackoutValue = 1;
		while( Blackout.Instance.BlackoutValue != 1){
			yield return 0;
		}
		PlayerMover.Instance.ClearTarget();
		PlayerMover.Instance.Position = minecartB_Animation.transform.position + new Vector3(0,1.5f,0);
		currentSequence = MinecartSequence.BMoving;
		
		Blackout.Instance.TargetBlackoutValue = 0;
		while( Blackout.Instance.BlackoutValue != 0){
			yield return 0;
		}
		minecartB_Animation.Play();
		minecartBSound.loop = true;
		minecartBSound.Play();
		lastPosition = PlayerMover.Instance.Position;
		while(minecartB_Animation.IsPlaying(minecartB_Animation.clip.name)){

			float distance = (PlayerMover.Instance.Position - lastPosition).magnitude;
			float speed = distance / Time.deltaTime;
			minecartBSound.pitch = 0.75f + 0.5f*Mathf.Clamp01(speed/20f);
			//Debug.Log(speed);
			lastPosition = PlayerMover.Instance.Position;
			float tiempo = minecartB_Animation[minecartB_Animation.clip.name].normalizedTime;
			if(tiempo > endBStart){
				//if(minecartBStopSound.isPlaying == false){
				//	minecartBStopSound.Play();
				//}
			}
			yield return 0;
		}
		minecartBSound.Stop();
		Blackout.Instance.TargetBlackoutValue = 1;
		while( Blackout.Instance.BlackoutValue != 1){
			yield return 0;
		}
		
		currentSequence = MinecartSequence.None;
		//minecartB_Animation.Rewind(minecartB_Animation.clip.name);
		PlayerMover.Instance.SetTargetPosition(new Vector3(0,1.5f,0),spawnPoint, true);
		minecartB_Animation[minecartB_Animation.clip.name].time = 0f;
		minecartB_Animation[minecartB_Animation.clip.name].weight = 1f;
		minecartB_Animation[minecartB_Animation.clip.name].enabled = true;
		minecartB_Animation.Sample();
		minecartB_Animation[minecartB_Animation.clip.name].enabled = false;
		PlayerMover.Instance.Rotation = Quaternion.identity;

		Blackout.Instance.TargetBlackoutValue = 0;
		while( Blackout.Instance.BlackoutValue != 0){
			yield return 0;
		}
		rodeB = true;
	}

	/// <summary>
/// Reset the minecart animations
/// </summary>
	void ResetMinecarts(){

		minecartA_Animation[minecartA_Animation.clip.name].time = 0f;
		minecartA_Animation[minecartA_Animation.clip.name].weight = 1;
		minecartA_Animation[minecartA_Animation.clip.name].enabled = true;
		minecartA_Animation.Sample();
		minecartA_Animation[minecartA_Animation.clip.name].enabled = false;

		minecartB_Animation[minecartB_Animation.clip.name].time = 0f;
		minecartB_Animation[minecartB_Animation.clip.name].weight = 1f;
		minecartB_Animation[minecartB_Animation.clip.name].enabled = true;
		minecartB_Animation.Sample();
		minecartB_Animation[minecartB_Animation.clip.name].enabled = false;
	}



}
