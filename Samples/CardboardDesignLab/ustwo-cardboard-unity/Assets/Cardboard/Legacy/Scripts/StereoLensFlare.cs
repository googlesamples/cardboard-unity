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

/// @ingroup LegacyScripts
///
/// This script can be attached to a Lens Flare to make it stereo-aware in Directional mode.
///
/// Unity 4's built-in lens flares do not work correctly for stereo rendering when in
/// Directional mode, for similar reasons as the skybox.
///
/// To use it, add the script to a Lens Flare and clear the flare's Directional flag
/// so that the flare is actually positional.  This script keeps the flare at a
/// distance well away from the mono camera along the flare's own forward vector,
/// thus recreating the directional behavior, but with proper stereo parallax.  The
/// flare is repositioned relative to each camera that renders it.
[RequireComponent(typeof(LensFlare))]
public class StereoLensFlare : MonoBehaviour {
#if UNITY_5
  void Awake() {
    Debug.Log("StereoLensFlare is not needed in Unity 5.");
    Component.Destroy(this);
  }
#else
  /// Sets the distance to the flare as a fraction of the camera's far clipping distance.
  [Tooltip("Fraction of the camera's far clip distance " +
           "at which to position the flare.")]
  [Range(0,1)]
  public float range = 0.75f;

  // Position the flare relative to the camera about to render it.
  void OnWillRenderObject() {
    transform.position = Camera.current.transform.position -
                         range * Camera.current.farClipPlane * transform.forward;
  }
#endif
}
