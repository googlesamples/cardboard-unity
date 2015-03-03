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

// Works with CardboardGUI to show all or part of the OnGUI-rendered
// UI on a mesh in the scene.  There should be one or more of these
// as children of the CardboardGUI gameobject.  Each one specifies
// the region of the captured OnGUI screen to show on its own mesh.
// This allows you to place pieces of the GUI in the scene at different
// distances and angles to the user without having to change any of the
// OnGUI methods themselves.
//
// The rendering code here takes care of shader and texture setup.  The
// choice of mesh is yours, but a quad looks good.  The mesh is scaled to
// match the aspect ratio of the screen region shown.  The attached
// collider is used for ray instersection with the user's gaze, so it
// should return texture coordinates that match those of the meshfilter.
//
// Individual portions of the GUI can be turned on or off by enabling
// this component independently of the sibling gameobjects.
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(Collider))]
public class CardboardGUIWindow : MonoBehaviour {
  private MeshRenderer meshRenderer;

  void Reset() {
    rect = new Rect(0,0,1,1);  // Make window show the full GUI screen.
  }

  [Tooltip("The portion of the OnGUI screen to show in this window. " +
           "Use the same units as for Camera viewports.")]
  public Rect rect;

  void Awake() {
    meshRenderer = GetComponent<MeshRenderer>();
    if (!SystemInfo.supportsRenderTextures) {
      enabled = false;
    }
  }

  // Make a material that points to the target texture.  Texture scale
  // and offset will be controlled by rect.
  public void Create(RenderTexture target) {
    meshRenderer.material = new Material(Shader.Find("Cardboard/GUIScreen")) {
      mainTexture = target,
      mainTextureOffset = rect.position,
      mainTextureScale = rect.size
    };
  }

  // Turn off this window.
  void OnDisable() {
    meshRenderer.enabled = false;
  }

  void LateUpdate() {
    // Only render window when the overall GUI is enabled.
    meshRenderer.enabled = CardboardGUI.IsGUIVisible;
    // Keep material and object scale in sync with rect.
    meshRenderer.material.mainTextureOffset = rect.position;
    meshRenderer.material.mainTextureScale = rect.size;
    float aspect = (Screen.width * rect.width) /
                   (Screen.height * rect.height);
    transform.localScale = new Vector3(aspect, 1, 1);
  }
}
