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

/// Clears the entire screen.  This script and CardboardPostRender work together
/// to draw the whole screen in VR Mode.  There should be exactly one of each
/// component in any Cardboard scene.  It is part of the _CardboardCamera_
/// prefab, which is included in _CardboardMain_.  The Cardboard script will
/// create one at runtime if the scene doesn't already have it, so generally
/// it is not necessary to manually add it unless you wish to edit the _Camera_
/// component that it controls.
[RequireComponent(typeof(Camera))]
[AddComponentMenu("Cardboard/CardboardPreRender")]
public class CardboardPreRender : MonoBehaviour {

  public Camera cam { get; private set; }

  void Awake() {
    cam = GetComponent<Camera>();
  }

  void Reset() {
#if UNITY_EDITOR
    // Member variable 'cam' not always initialized when this method called in Editor.
    // So, we'll just make a local of the same name.
    var cam = GetComponent<Camera>();
#endif
    cam.clearFlags = CameraClearFlags.SolidColor;
    cam.backgroundColor = Color.black;
    cam.cullingMask = 0;
    cam.useOcclusionCulling = false;
    cam.depth = -100;
  }

  void OnPreCull() {
    Cardboard.SDK.UpdateState();
    if (Cardboard.SDK.ProfileChanged) {
      SetShaderGlobals();
    }
    cam.clearFlags = Cardboard.SDK.VRModeEnabled ?
        CameraClearFlags.SolidColor : CameraClearFlags.Nothing;
    var stereoScreen = Cardboard.SDK.StereoScreen;
    if (stereoScreen != null && !stereoScreen.IsCreated()) {
      stereoScreen.Create();
    }
  }

  private void SetShaderGlobals() {
    // For any shaders that want to use these numbers for distortion correction.
    CardboardProfile p = Cardboard.SDK.Profile;
    Shader.SetGlobalVector("_Undistortion",
        new Vector4(p.device.inverse.k1, p.device.inverse.k2));
    Shader.SetGlobalVector("_Distortion",
        new Vector4(p.device.distortion.k1, p.device.distortion.k2));
    float[] rect = new float[4];
    p.GetLeftEyeVisibleTanAngles(rect);
    float r = CardboardProfile.GetMaxRadius(rect);
    Shader.SetGlobalFloat("_MaxRadSq", r*r);
  }
}
