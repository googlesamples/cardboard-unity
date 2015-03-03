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

// Applies the inverse of the lens distortion to the image.  The image is "undistorted" so
// that when viewed through the lenses (which redistort), the image looks normal.  In the case
// of Cardboard, the lenses apply a pincushion distortion, so this effect applies a barrel
// distortion to counteract that.
[RequireComponent(typeof(Camera))]
public class RadialUndistortionEffect : MonoBehaviour {

  private Material material;

  void Awake() {
    if (!SystemInfo.supportsRenderTextures) {
      Debug.Log("Radial Undistortion disabled: render textures not supported.");
      return;
    }
    Shader shader = Shader.Find("Cardboard/Radial Undistortion");
    if (shader == null) {
      Debug.Log("Radial Undistortion disabled: shader not found.");
      return;
    }
    material = new Material(shader);
  }

  void OnRenderImage(RenderTexture source, RenderTexture dest) {
    // Check if we found our shader, and that native distortion correction is OFF.
    if (material == null || Cardboard.SDK.nativeDistortionCorrection) {
      // Pass through, no effect.
      Graphics.Blit(source, dest);
    } else {
      // Undistort the image.
      Graphics.Blit(source, dest, material);
    }
  }
}
