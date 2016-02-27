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


namespace ustwo{
/// <summary>
/// General application startup  
/// </summary>

public class VRUtilities  : MonoBehaviour{

	static VRUtilities instance;
	public static VRUtilities Instance{
		get{
			return instance;
		}
	}
	private bool enableHapticFeedback = true;
	/*
	public float _BarrelPower2;
	public float _BarrelPower4;

	


	private void Awake(){
		instance = this;
		Application.targetFrameRate = 60;
		#if UNITY_EDITOR
		Shader.SetGlobalFloat("_BarrelPower2", _BarrelPower2 );
		Shader.SetGlobalFloat("_BarrelPower4", _BarrelPower4 );
		#else
		Shader.SetGlobalFloat("_BarrelPower2", 0);//_BarrelPower2 );
		Shader.SetGlobalFloat("_BarrelPower4", 0);//_BarrelPower4 );
		#endif
	}
	*/

	void Awake(){
		instance = this;
	}

	/// <summary>
	/// set screens to not fall asleep or dim, also selectively disable drift correction based on the device - currently only iOS disables drift correction
	/// Why disable drift correction?  Correction inherently relies on smoothing past data and extrapolating, but on a good device, it often cannot distinguish between head movement, and drift - so it will always make good data worse - on bad devices, it makes bad data much less bad, so the tradeoff may be worth it.
	/// </summary>
	void Start(){
		Screen.sleepTimeout = SleepTimeout.NeverSleep;

		#if UNITY_IOS
			Cardboard.SDK.AutoDriftCorrection = false;
		#endif

		#if UNITY_ANDROID

		#endif

	}


	/// <summary>
	/// an abstracted call to trigger haptic interface feedback, currently not in use
	/// </summary>
	public void InterfaceFeedback(){

		///Audio?

		///Vibrate device
		if(enableHapticFeedback){
			#if UNITY_IOS || UNITY_ANDROID
				Handheld.Vibrate();
			#endif
		}
	}

}
}
