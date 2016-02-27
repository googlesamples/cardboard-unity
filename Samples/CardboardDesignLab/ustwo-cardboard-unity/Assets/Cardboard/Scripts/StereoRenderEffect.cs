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

/// @cond
[RequireComponent(typeof(Camera))]
[AddComponentMenu("Cardboard/StereoRenderEffect")]
public class StereoRenderEffect : MonoBehaviour {
  private Material material;

#if UNITY_5
  private new Camera camera;

  void Awake() {
    camera = GetComponent<Camera>();
  }
#endif

  void Start() {
    material = new Material(Shader.Find("Cardboard/SkyboxMesh"));
  }

  void OnRenderImage(RenderTexture source, RenderTexture dest) {
    GL.PushMatrix();
    int width = dest ? dest.width : Screen.width;
    int height = dest ? dest.height : Screen.height;
    GL.LoadPixelMatrix(0, width, height, 0);
    // Camera rects are in screen coordinates (bottom left is origin), but DrawTexture takes a
    // rect in GUI coordinates (top left is origin).
    Rect blitRect = camera.pixelRect;
    blitRect.y = height - blitRect.height - blitRect.y;
    RenderTexture oldActive = RenderTexture.active;
    RenderTexture.active = dest;
    Graphics.DrawTexture(blitRect, source, material);
    RenderTexture.active = oldActive;
    GL.PopMatrix();
  }
}
/// @endcond

