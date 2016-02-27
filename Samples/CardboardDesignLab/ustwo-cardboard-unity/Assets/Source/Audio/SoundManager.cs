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
/// A better name for this class is AmbientTrackManager.  This controls N (really, 2) ambient tracks.  For performance reasons, we limit to 2, and since most mobile devices can only play one compressed track, I've left everything uncompressed, but sampled at 22khz instead of 44.1 (which improved CPU performance immensely)
/// This class was meant to be smart, but ended up being 'dumb', controlling scripts just tell it what to do, it just interfaces with the audioSources
/// </summary>
public class SoundManager : MonoBehaviour {

	static SoundManager instance;
	public static SoundManager Instance{
		get{
			return instance;
		}
	}

	void Awake(){
		instance = this;

	}

	[SerializeField] AudioSource[] ambientSource;

	AudioClipData[] ambientTracks = new AudioClipData[2];
	float[] ambientVolume = new float[2];

	/// <summary>
	/// Set one of the ambient tracks.  valid values for idx are 0 or 1, AudioClipData can be set to null to stop playing a clip
	/// </summary>
	public void SetAmbientTrack(int idx, AudioClipData data, float targetVolume){
		ambientTracks[idx] = data;
		ambientVolume[idx] = targetVolume;
	}
	/*AudioClipData currentAmbientTrack;
	AudioClipData targetAmbientTrack;

	int currentAmbientIdx = 0;
	int targetAmbientIdx = 1;

	float ambientBlendSpeed = 1;
	bool trackTransition = false;
	float ambientTransition = 0;

	public void SetTargetAmbientTrack(AudioClipData data, float blendSpeed =1){
		targetAmbientTrack = data;
		ambientBlendSpeed = Mathf.Clamp(blendSpeed, 0.1f, 20f);
		if( currentAmbientTrack != targetAmbientTrack){
			trackTransition = true;
			ambientTransition = 0;
			if(targetAmbientTrack != null){
				ambientSource[targetAmbientIdx] = targetAmbientTrack;
			}
		}

	}*/

	/// <summary>
	/// Update the ambient sources.  A bit wasteful, but it was fast...  We could just do all this in SetAmbientTrack - but I was intending to do additional volume easing here.  I ended up doing it in the controlling script, so this should only be called on Set, not every frame
	/// </summary>
	void Update(){
		for(int i=0; i<ambientTracks.Length; i++){
			if(ambientTracks[i] == null){
				if(ambientSource[i].isPlaying){
					ambientSource[i].Stop();
				}
			}
			else{
				if(ambientSource[i].clip != ambientTracks[i].clip && ambientTracks[i].clip != null){
					ambientSource[i].clip = ambientTracks[i].clip;

				}
				if(!ambientSource[i].isPlaying){
					ambientSource[i].Play();
				}
				ambientSource[i].volume = ambientTracks[i].volume * ambientVolume[i];
			}
			
		}
	}


}
