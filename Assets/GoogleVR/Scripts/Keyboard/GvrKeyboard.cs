// Copyright 2017 Google Inc. All rights reserved.
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
using UnityEngine.EventSystems;
using Gvr.Internal;
using System;
using System.Collections;
using System.Collections.Generic;

// Events to update the keyboard.
// These values depend on C API keyboard values
public enum GvrKeyboardEvent {
  /// Unknown error.
  GVR_KEYBOARD_ERROR_UNKNOWN = 0,
  /// The keyboard service could not be connected. This is usually due to the
  /// keyboard service not being installed.
  GVR_KEYBOARD_ERROR_SERVICE_NOT_CONNECTED = 1,
  /// No locale was found in the keyboard service.
  GVR_KEYBOARD_ERROR_NO_LOCALES_FOUND = 2,
  /// The keyboard SDK tried to load dynamically but failed. This is usually due
  /// to the keyboard service not being installed or being out of date.
  GVR_KEYBOARD_ERROR_SDK_LOAD_FAILED = 3,
  /// Keyboard becomes visible.
  GVR_KEYBOARD_SHOWN = 4,
  /// Keyboard becomes hidden.
  GVR_KEYBOARD_HIDDEN = 5,
  /// Text has been updated.
  GVR_KEYBOARD_TEXT_UPDATED = 6,
  /// Text has been committed.
  GVR_KEYBOARD_TEXT_COMMITTED = 7,
};

// These values depend on C API keyboard values.
public enum GvrKeyboardError {
  UNKNOWN = 0,
  SERVICE_NOT_CONNECTED = 1,
  NO_LOCALES_FOUND = 2,
  SDK_LOAD_FAILED = 3
};

// These values depend on C API keyboard values.
public enum GvrKeyboardInputMode {
  DEFAULT = 0,
  NUMERIC = 1
};

// Handles keyboard state management such as hiding and displaying
// the keyboard, directly modifying text and stereoscopic rendering.
public class GvrKeyboard : MonoBehaviour {

  private static GvrKeyboard instance;
  private static IKeyboardProvider keyboardProvider;
  private KeyboardState keyboardState = new KeyboardState();
  private IEnumerator keyboardUpdate;

  // Keyboard delegate types.
  public delegate void StandardCallback();
  public delegate void EditTextCallback(string edit_text);
  public delegate void ErrorCallback(GvrKeyboardError err);
  public delegate void KeyboardCallback(IntPtr closure, GvrKeyboardEvent evt);

  // Private data and callbacks.
  private ErrorCallback errorCallback = null;
  private StandardCallback showCallback = null;
  private StandardCallback hideCallback = null;
  private EditTextCallback updateCallback = null;
  private EditTextCallback enterCallback = null;

#if UNITY_ANDROID
  // Which eye is currently being rendered.
  private bool isRight = false;
#endif // UNITY_ANDROID

  private bool isKeyboardHidden = false;
  private const float kExecuterWait = 0.01f;
  private static List<GvrKeyboardEvent> threadSafeCallbacks =
    new List<GvrKeyboardEvent>();
  private static System.Object callbacksLock = new System.Object();

  // Public parameters.
  public GvrKeyboardDelegateBase keyboardDelegate = null;
  public GvrKeyboardInputMode inputMode = GvrKeyboardInputMode.DEFAULT;
  public bool useRecommended = true;
  public float distance = 0;

  public string EditorText {
    get { return instance != null ? instance.keyboardState.editorText : string.Empty; }
    set { keyboardProvider.EditorText = value; }
  }

  public GvrKeyboardInputMode Mode {
    get { return instance != null ? instance.keyboardState.mode : GvrKeyboardInputMode.DEFAULT; }
  }

  public bool IsValid {
    get { return instance != null ? instance.keyboardState.isValid : false; }
  }

  public bool IsReady {
    get { return instance != null ? instance.keyboardState.isReady : false; }
  }

  public Matrix4x4 WorldMatrix {
    get { return instance != null ? instance.keyboardState.worldMatrix : Matrix4x4.zero; }
  }

  void Awake() {
    if (instance != null) {
      Debug.LogError("More than one GvrKeyboard instance was found in your scene. "
          + "Ensure that there is only one GvrKeyboard.");
      enabled = false;
      return;
    }
    instance = this;
    if (keyboardProvider == null) {
      keyboardProvider = KeyboardProviderFactory.CreateKeyboardProvider(this);
    }
  }

  void OnDestroy() {
    instance = null;
  }

  // Use this for initialization.
  void Start() {
    if (keyboardDelegate != null) {
      errorCallback  = keyboardDelegate.OnKeyboardError;
      showCallback   = keyboardDelegate.OnKeyboardShow;
      hideCallback   = keyboardDelegate.OnKeyboardHide;
      updateCallback = keyboardDelegate.OnKeyboardUpdate;
      enterCallback  = keyboardDelegate.OnKeyboardEnterPressed;
      keyboardDelegate.KeyboardHidden += KeyboardDelegate_KeyboardHidden;
      keyboardDelegate.KeyboardShown += KeyboardDelegate_KeyboardShown;
    }
    keyboardProvider.ReadState(keyboardState);

    if (IsValid) {
      if (keyboardProvider.Create(OnKeyboardCallback)) {
        keyboardProvider.SetInputMode(inputMode);
      }
    } else {
      Debug.LogError("Could not validate keyboard");
    }
  }

  // Update per-frame data.
  void Update() {
    if (keyboardProvider == null) {
      return;
    }
    keyboardProvider.ReadState(keyboardState);
    if (IsReady) {
      // Reset position of keyboard.
      if (transform.hasChanged) {
        Show();
        transform.hasChanged = false;
      }

      keyboardProvider.UpdateData();
    }
  }

  // Use this function for procedural rendering
  // Gets called twice per frame, once for each eye.
  // On each frame, left eye renders before right eye so
  // we keep track of a boolean that toggles back and forth
  // between each eye.
  void OnRenderObject() {
    if (keyboardProvider == null || !IsReady) {
      return;
    }
#if UNITY_ANDROID
    Camera camera = Camera.current;
    if (camera && camera == Camera.main) {
      // Get current eye.
      Camera.StereoscopicEye camEye = isRight ? Camera.StereoscopicEye.Right : Camera.StereoscopicEye.Left;

      // Camera matrices.
      Matrix4x4 proj = camera.GetStereoProjectionMatrix(camEye);
      Matrix4x4 modelView = camera.GetStereoViewMatrix(camEye);

      // Camera viewport.
      Rect viewport = camera.pixelRect;

      // Render keyboard.
      keyboardProvider.Render((int) camEye, modelView, proj, viewport);

      // Swap.
      isRight = !isRight;
    }
#endif  // !UNITY_ANDROID
  }

  // Resets keyboard text.
  public void ClearText() {
    if (keyboardProvider != null) {
      keyboardProvider.EditorText = string.Empty;
    }
  }

  public void Show() {
    if (keyboardProvider == null) {
      return;
    }

    // Get user matrix.
    Quaternion fixRot = new Quaternion(transform.rotation.x * -1, transform.rotation.y * -1,
      transform.rotation.z, transform.rotation.w);
    // Need to convert from left handed to right handed for the Keyboard coordinates.
    Vector3 fixPos = new Vector3(transform.position.x, transform.position.y,
      transform.position.z * -1);
    Matrix4x4 modelMatrix = Matrix4x4.TRS(fixPos, fixRot, Vector3.one);
    Matrix4x4 mat = Matrix4x4.identity;
    Vector3 position = gameObject.transform.position;
    if (position.x == 0 && position.y == 0 && position.z == 0 && !useRecommended) {
      // Force use recommended to be true, otherwise keyboard won't show up.
      keyboardProvider.Show(mat, true, distance, modelMatrix);
      return;
    }

    // Matrix needs to be set only if we're not using the recommended one.
    // Uses the values of the keyboard gameobject transform as reported by Unity. If this is
    // the zero vector, parent it under another gameobject instead.
    if (!useRecommended) {
      mat = GetKeyboardObjectMatrix(position);
    }

    keyboardProvider.Show(mat, useRecommended, distance, modelMatrix);
  }

  public void Hide() {
    if (keyboardProvider != null) {
      keyboardProvider.Hide();
    }
  }

  public void OnPointerClick(BaseEventData data) {
    if (isKeyboardHidden) {
      Show();
    }
  }

  void OnEnable() {
    keyboardUpdate = Executer();
    StartCoroutine(keyboardUpdate);
  }

  void OnDisable() {
    StopCoroutine(keyboardUpdate);
  }

  void OnApplicationPause(bool paused) {
    if (null == keyboardProvider) return;
    if (paused) {
      keyboardProvider.OnPause();
    } else {
      keyboardProvider.OnResume();
    }
  }

  IEnumerator Executer() {
    while (true) {
      yield return new WaitForSeconds(kExecuterWait);

      while (threadSafeCallbacks.Count > 0) {
        GvrKeyboardEvent keyboardEvent = threadSafeCallbacks[0];
        PoolKeyboardCallbacks(keyboardEvent);
        lock (callbacksLock) {
          threadSafeCallbacks.RemoveAt(0);
        }
      }
    }
  }

  private void PoolKeyboardCallbacks(GvrKeyboardEvent keyboardEvent) {
    switch (keyboardEvent) {
      case GvrKeyboardEvent.GVR_KEYBOARD_ERROR_UNKNOWN:
        errorCallback(GvrKeyboardError.UNKNOWN);
        break;
      case GvrKeyboardEvent.GVR_KEYBOARD_ERROR_SERVICE_NOT_CONNECTED:
        errorCallback(GvrKeyboardError.SERVICE_NOT_CONNECTED);
        break;
      case GvrKeyboardEvent.GVR_KEYBOARD_ERROR_NO_LOCALES_FOUND:
        errorCallback(GvrKeyboardError.NO_LOCALES_FOUND);
        break;
      case GvrKeyboardEvent.GVR_KEYBOARD_ERROR_SDK_LOAD_FAILED:
        errorCallback(GvrKeyboardError.SDK_LOAD_FAILED);
        break;
      case GvrKeyboardEvent.GVR_KEYBOARD_SHOWN:
        showCallback();
        break;
      case GvrKeyboardEvent.GVR_KEYBOARD_HIDDEN:
        hideCallback();
        break;
      case GvrKeyboardEvent.GVR_KEYBOARD_TEXT_UPDATED:
        updateCallback(keyboardProvider.EditorText);
        break;
      case GvrKeyboardEvent.GVR_KEYBOARD_TEXT_COMMITTED:
        enterCallback(keyboardProvider.EditorText);
        break;
    }
  }

  private static void OnKeyboardCallback(IntPtr closure, GvrKeyboardEvent keyboardEvent) {
    lock (callbacksLock) {
      threadSafeCallbacks.Add(keyboardEvent);
    }
  }

  private void KeyboardDelegate_KeyboardShown(object sender, System.EventArgs e) {
    isKeyboardHidden = false;
  }

  private void KeyboardDelegate_KeyboardHidden(object sender, System.EventArgs e) {
    isKeyboardHidden = true;
  }

  // Returns a matrix populated by the keyboard's gameobject position. If the position is not
  // zero, but comes back as zero, parent this under another gameobject instead.
  private Matrix4x4 GetKeyboardObjectMatrix(Vector3 position) {
    // Set keyboard position based on this gameObject's position.
    float angleX = Mathf.Atan2(position.y, position.x);
    float kTanAngleX = Mathf.Tan(angleX);
    float newPosX = kTanAngleX * position.x;

    float angleY = Mathf.Atan2(position.x, position.y);
    float kTanAngleY = Mathf.Tan(angleY);
    float newPosY = kTanAngleY * position.y;

    float angleZ = Mathf.Atan2(position.y, position.z);
    float kTanAngleZ = Mathf.Tan(angleZ);
    float newPosZ = kTanAngleZ * position.z;

    Vector3 keyboardPosition = new Vector3(newPosX, newPosY, newPosZ);
    Vector3 lookPosition = Camera.main.transform.position;

    Quaternion rotation = Quaternion.LookRotation(lookPosition);
    Matrix4x4 mat = new Matrix4x4();
    mat.SetTRS(keyboardPosition, rotation, position);

    // Set diagonal to identity if any of them are zero.
    if (mat[0, 0] == 0) {
      Vector4 row0 = mat.GetRow(0);
      mat.SetRow(0, new Vector4(1, row0.y, row0.z, row0.w));
    }
    if (mat[1, 1] == 0) {
      Vector4 row1 = mat.GetRow(1);
      mat.SetRow(1, new Vector4(row1.x, 1, row1.z, row1.w));
    }
    if (mat[2, 2] == 0) {
      Vector4 row2 = mat.GetRow(2);
      mat.SetRow(2, new Vector4(row2.x, row2.y, 1, row2.w));
    }
    return mat;
  }
}
