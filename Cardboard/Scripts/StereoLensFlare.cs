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

// Replacement for directional lens flares in stereo, which like skyboxes
// (see SkyboxMesh.cs) do not consider the separation of the eyes.
// Attach this script to a positional lens flare to create a stereo-
// aware directional flare.  It keeps the flare positioned away well from
// the camera along its own forward vector.
[RequireComponent(typeof(LensFlare))]
public class StereoLensFlare : MonoBehaviour {
  [Tooltip("Fraction of the camera's far clip distance " +
           "at which to position the flare.")]
  [Range(0,1)]
  public float range = 0.75f;

  // Position the flare relative to the camera about to render it.
  void OnWillRenderObject() {
    transform.position = Camera.current.transform.position -
                         range * Camera.current.farClipPlane * transform.forward;
  }
}
