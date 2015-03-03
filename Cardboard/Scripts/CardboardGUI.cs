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

// A utility class for redirecting OnGUI()-based UI elements onto a texture,
// which can be drawn on a surface in the scene and thus appear in stereo.
// The auxiliary CardboardGUIWindow class handles displaying the texture
// in the scene.  CardboardGUI captures the OnGUI() calls, which need to be
// modified slightly for this to work, and handles feeding back fake mouse
// events into the Unity player based on the user's head and trigger actions.
//
// If the option is available, it is probably better to use a real 3D
// GUI such as the new Unity4.6+ system.  But if you just want to get
// existing projects based on OnGUI() to work in Cardboard, this class
// can help.
public class CardboardGUI : MonoBehaviour {

  // The type of an OnGUI() method.
  public delegate void OnGUICallback();

  // Add a gameobject's OnGUI() to this event, which is called during the
  // redirection phase to capture the UI in a texture.
  public static event OnGUICallback onGUICallback;

  // Unity is going to call OnGUI() methods as usual, in addition to them
  // being called by the onGUICallback event.  To prevent this double
  // call from affecting game logic, modify the OnGUI() method.  At the
  // very top, add "if (!CardboardGUI.OKToDraw(this)) return; ".
  // This will ensure only one OnGUI() call sequence occurs per frame,
  // whether in VR Mode or not.
  private static bool okToDraw;
  public static bool OKToDraw(MonoBehaviour mb) {
    return okToDraw && (mb == null || mb.enabled && mb.gameObject.activeInHierarchy);
  }

  // Unlike on a plain 2D screen, in Cardboard the edges of the screen are
  // very hard to see.  UI elements will likely have to be moved towards
  // the center for the user to be able to interact with them.  As this would
  // obscure the view of the rest of the scene, the class provides this flag to
  // hide or show the entire in-scene rendering of the GUI, so it can be shown
  // only when the user needs it.
  private static bool isGUIVisible;
  public static bool IsGUIVisible {
    get {
      return isGUIVisible && Cardboard.SDK.VRModeEnabled && SystemInfo.supportsRenderTextures;
    }
    set {
      isGUIVisible = value;
    }
  }

  // A wrapper around the trigger event that lets the GUI, when it
  // is visible, steal the trigger for mouse clicking.  Call this
  // instead of Cardboard.SDK.CardboardTriggered when this behavior
  // is desired.
  public static bool CardboardTriggered {
    get {
      return !IsGUIVisible && Cardboard.SDK.CardboardTriggered;
    }
  }

  [Tooltip("The color and transparency of the overall GUI screen.")]
  public Color background = Color.clear;

  // Texture that captures the OnGUI rendering.
  private RenderTexture guiScreen;

  void Awake() {
    if (!SystemInfo.supportsRenderTextures) {
      Debug.LogWarning("CardboardGUI disabled.  RenderTextures are not supported, either due to license or platform.");
      enabled = false;
    }
  }

  void Start() {
    Create();
  }

  // Sets up the render texture and all the windows that will show parts of it.
  public void Create() {
    if (guiScreen != null) {
      guiScreen.Release();
    }
    RenderTexture stereoScreen = Cardboard.SDK.StereoScreen;
    int width = stereoScreen ? stereoScreen.width : Screen.width;
    int height = stereoScreen ? stereoScreen.height : Screen.height;
    guiScreen = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32);
    guiScreen.Create();
    foreach (var guiWindow in GetComponentsInChildren<CardboardGUIWindow>(true)) {
      guiWindow.Create(guiScreen);
    }
  }

  void LateUpdate() {
    // When not in VR Mode, let normal OnGUI() calls proceed.
    okToDraw = !Cardboard.SDK.VRModeEnabled;
  }

  void OnGUI() {
    if (!IsGUIVisible) {
      return;  // Nothing to do.
    }
    // OnGUI() is called multiple times per frame: once with event type "Layout",
    // then once for each user input (key, mouse, touch, etc) that occurred in this
    // frame, then finally once with event type "Repaint", during which the actual
    // GUI drawing is done.  This last phase is the one we have to capture.
    RenderTexture prevRT = null;
    if (Event.current.type == EventType.Repaint) {
      // Redirect rendering to our texture.
      prevRT = RenderTexture.active;
      RenderTexture.active = guiScreen;
      GL.Clear(false, true, background);
    }
    if (onGUICallback != null) {
      // Call all the OnGUI methods that have registered.
      okToDraw = true;
      onGUICallback();
      okToDraw = false;
    }
    if (Event.current.type == EventType.Repaint) {
      CardboardGUIMouse cgm = GetComponent<CardboardGUIMouse>();
      if (cgm != null) {
        cgm.DrawPointerImage();
      }
      // Clean up.
      RenderTexture.active = prevRT;
    }
  }
}
