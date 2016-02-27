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

/// @ingroup LegacyScripts
/// A utility class for redirecting OnGUI()-based UI elements onto a texture,
/// which can be drawn on a surface in the scene and thus appear in stereo.
///
/// The auxiliary CardboardOnGUIWindow class handles displaying the texture
/// in the scene.  CardboardOnGUI captures the OnGUI() calls, which need to be
/// modified slightly for this to work, and handles feeding back fake mouse
/// events into the Unity player based on the user's head and trigger actions.
///
/// If the option is available, it is probably better to use a real 3D
/// GUI such as the new Unity4.6+ system.  But if you just want to get
/// existing projects based on OnGUI() to work in Cardboard, this class
/// can help.
///
/// Position the GUI mesh in 3D space using the gameobject's transform.  Note: do
/// not place the object as a child of the CardboardHead, or the user will not be
/// able to look at different parts of the UI because it will move with them.  It is
/// better to place it as a sibling of the Head object.
public class CardboardOnGUI : MonoBehaviour {

  /// The .NET type of an `OnGUI()` method.
  public delegate void OnGUICallback();

  /// The list of `OnGUI()` calls that will be captured in the texture.  Add your
  /// script's OnGUI method to enable the script's GUI in the CardboardOnGUI window.
  public static event OnGUICallback onGUICallback;

  /// Unity is going to call OnGUI() methods as usual, in addition to them
  /// being called by the onGUICallback event.  To prevent this double
  /// call from affecting game logic, modify the OnGUI() method.  At the
  /// very top, add "if (!CardboardOnGUI.OKToDraw(this)) return;".
  /// This will ensure only one OnGUI() call sequence occurs per frame,
  /// whether in VR Mode or not.
  public static bool OKToDraw(MonoBehaviour mb) {
    return okToDraw && (mb == null || mb.enabled && mb.gameObject.activeInHierarchy);
  }
  private static bool okToDraw;

  /// This is your global visibility control for whether CardboardOnGUI shows anything
  /// in the scene.  Set it to _true_ to allow the GUI to draw, and _false_ to hide
  /// it.  The purpose is so you can move GUI elements away from the edges of the
  /// screen (which are hard to see in VR Mode), and instead pop up the GUI when it
  /// is needed, and dismiss it when not.
  public static bool IsGUIVisible {
    get {
      return isGUIVisible && Cardboard.SDK.VRModeEnabled && SystemInfo.supportsRenderTextures;
    }
    set {
      isGUIVisible = value;
    }
  }
  private static bool isGUIVisible;

  /// A wrapper around Cardboard#Triggered that hides the trigger event
  /// when the GUI is visible (i.e. when you set #IsGUIVisible to true).
  /// By using this flag in your scripts rather than Cardboard#Triggered,
  /// you allow the GUI to take the trigger events when it is visible.
  /// This is when the trigger is needed for clicking buttons.
  /// When the GUI is hidden, the trigger events pass through.
  public static bool Triggered {
    get {
      return !IsGUIVisible && Cardboard.SDK.Triggered;
    }
  }

  /// The GUI texture is cleared to this color before any OnGUI methods are called
  /// during the capture phase.
  [Tooltip("The color and transparency of the overall GUI screen.")]
  public Color background = Color.clear;

  // Texture that captures the OnGUI rendering.
  private RenderTexture guiScreen;

  void Awake() {
    if (!SystemInfo.supportsRenderTextures) {
      Debug.LogWarning("CardboardOnGUI disabled.  RenderTextures are not supported, "
                       + "either due to license or platform.");
      enabled = false;
    }
  }

  void Start() {
    Create();
  }

  /// Sets up the render texture and all the windows that will show parts of it.
  public void Create() {
    if (guiScreen != null) {
      guiScreen.Release();
    }
    RenderTexture stereoScreen = Cardboard.SDK.StereoScreen;
    int width = stereoScreen ? stereoScreen.width : Screen.width;
    int height = stereoScreen ? stereoScreen.height : Screen.height;
    guiScreen = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32);
    guiScreen.Create();
    foreach (var guiWindow in GetComponentsInChildren<CardboardOnGUIWindow>(true)) {
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
      CardboardOnGUIMouse cgm = GetComponent<CardboardOnGUIMouse>();
      if (cgm != null) {
        cgm.DrawPointerImage();
      }
      // Clean up.
      RenderTexture.active = prevRT;
    }
  }
}
