// Copyright 2015 Google Inc. All rights reserved.
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

/// Cardboard audio listener component that enhances AudioListener to provide advanced spatial audio
/// features.
///
/// There should be only one instance of this which is attached to the AudioListener's game object.
[RequireComponent(typeof(AudioListener))]
[AddComponentMenu("Cardboard/Audio/CardboardAudioListener")]
public class CardboardAudioListener : MonoBehaviour {
  /// Global gain in decibels to be applied to the processed output.
  public float globalGainDb = 0.0f;

  /// Global scale of the real world with respect to the Unity environment.
  public float worldScale = 1.0f;

  /// Audio rendering quality of the system.
  [SerializeField]
  private CardboardAudio.Quality quality = CardboardAudio.Quality.Medium;

  void Awake () {
    CardboardAudio.Initialize(this, quality);
  }

  void OnEnable () {
    CardboardAudio.UpdateAudioListener(globalGainDb, worldScale);
  }

  void OnDestroy () {
    CardboardAudio.Shutdown(this);
  }

  void Update () {
    CardboardAudio.UpdateAudioListener(globalGainDb, worldScale);
  }

  void OnAudioFilterRead (float[] data, int channels) {
    CardboardAudio.ProcessAudioListener(data, data.Length);
  }
}
