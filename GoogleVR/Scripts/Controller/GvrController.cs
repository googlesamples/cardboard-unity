// Copyright 2016 Google Inc. All rights reserved.
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

using System.Collections;
using UnityEngine;

using Gvr.Internal;

/// Represents the controller's current connection state.
public enum GvrConnectionState {
  /// Indicates that the controller is disconnected.
  Disconnected,
  /// Indicates that the device is scanning for controllers.
  Scanning,
  /// Indicates that the device is connecting to a controller.
  Connecting,
  /// Indicates that the device is connected to a controller.
  Connected,
  /// Indicates that an error has occurred.
  Error,
};

/// Main entry point for the Daydream controller API.
///
/// To use this API, add this behavior to a GameObject in your scene, or use the
/// GvrControllerMain prefab. There can only be one object with this behavior on your scene.
///
/// This is a singleton object.
///
/// To access the controller state, simply read the static properties of this class. For example,
/// to know the controller's current orientation, use GvrController.Orientation.
public class GvrController : MonoBehaviour {
  private ControllerState controllerState = new ControllerState();
  private static GvrController instance;
  private static IControllerProvider controllerProvider;

  /// If true, enable gyroscope on the controller.
  [Tooltip("If enabled, the controller will report gyroscope readings.")]
  public bool enableGyro = false;

  /// If true, enable accelerometer on the controller.
  [Tooltip("If enabled, the controller will report accelerometer readings.")]
  public bool enableAccel = false;

  public enum EmulatorConnectionMode {
    OFF,
    USB,
    WIFI,
  }
  /// Indicates how we connect to the controller emulator.
  [Tooltip("How to connect to the emulator: USB cable (recommended) or WIFI.")]
  public EmulatorConnectionMode emulatorConnectionMode = EmulatorConnectionMode.USB;

  /// Returns the controller's current connection state.
  public static GvrConnectionState State {
    get {
      return instance != null ? instance.controllerState.connectionState : GvrConnectionState.Error;
    }
  }

  /// Returns the controller's current orientation in space, as a quaternion.
  /// The space in which the orientation is represented is the usual Unity space, with
  /// X pointing to the right, Y pointing up and Z pointing forward. Therefore, to make an
  /// object in your scene have the same orientation as the controller, simply assign this
  /// quaternion to the GameObject's transform.rotation.
  public static Quaternion Orientation {
    get {
      return instance != null ? instance.controllerState.orientation : Quaternion.identity;
    }
  }

  /// Returns the controller's gyroscope reading. The gyroscope indicates the angular
  /// about each of its local axes. The controller's axes are: X points to the right,
  /// Y points perpendicularly up from the controller's top surface and Z lies
  /// along the controller's body, pointing towards the front. The angular speed is given
  /// in radians per second, using the right-hand rule (positive means a right-hand rotation
  /// about the given axis).
  public static Vector3 Gyro {
    get {
      return instance != null ? instance.controllerState.gyro : Vector3.zero;
    }
  }

  /// Returns the controller's accelerometer reading. The accelerometer indicates the
  /// effect of acceleration and gravity in the direction of each of the controller's local
  /// axes. The controller's local axes are: X points to the right, Y points perpendicularly
  /// up from the controller's top surface and Z lies along the controller's body, pointing
  /// towards the front. The acceleration is measured in meters per second squared. Note that
  /// gravity is combined with acceleration, so when the controller is resting on a table top,
  /// it will measure an acceleration of 9.8 m/s^2 on the Y axis. The accelerometer reading
  /// will be zero on all three axes only if the controller is in free fall, or if the user
  /// is in a zero gravity environment like a space station.
  public static Vector3 Accel {
    get {
      return instance != null ? instance.controllerState.accel : Vector3.zero;
    }
  }

  /// If true, the user is currently touching the controller's touchpad.
  public static bool IsTouching {
    get {
      return instance != null ? instance.controllerState.isTouching : false;
    }
  }

  /// If true, the user just started touching the touchpad. This is an event flag (it is true
  /// for only one frame after the event happens, then reverts to false).
  public static bool TouchDown {
    get {
      return instance != null ? instance.controllerState.touchDown : false;
    }
  }

  /// If true, the user just stopped touching the touchpad. This is an event flag (it is true
  /// for only one frame after the event happens, then reverts to false).
  public static bool TouchUp {
    get {
      return instance != null ? instance.controllerState.touchUp : false;
    }
  }

  public static Vector2 TouchPos {
    get {
      return instance != null ? instance.controllerState.touchPos : Vector2.zero;
    }
  }

  /// If true, the user is currently performing the recentering gesture. Most apps will want
  /// to pause the interaction while this remains true.
  public static bool Recentering {
    get {
      return instance != null ? instance.controllerState.recentering : false;
    }
  }

  /// If true, the user just completed the recenter gesture. The controller's orientation is
  /// now being reported in the new recentered coordinate system (the controller's orientation
  /// when recentering was completed was remapped to mean "forward"). This is an event flag
  /// (it is true for only one frame after the event happens, then reverts to false).
  /// The headset is recentered together with the controller.
  public static bool Recentered {
    get {
      return instance != null ? instance.controllerState.recentered : false;
    }
  }

  /// If true, the click button (touchpad button) is currently being pressed. This is not
  /// an event: it represents the button's state (it remains true while the button is being
  /// pressed).
  public static bool ClickButton {
    get {
      return instance != null ? instance.controllerState.clickButtonState : false;
    }
  }

  /// If true, the click button (touchpad button) was just pressed. This is an event flag:
  /// it will be true for only one frame after the event happens.
  public static bool ClickButtonDown {
    get {
      return instance != null ? instance.controllerState.clickButtonDown : false;
    }
  }

  /// If true, the click button (touchpad button) was just released. This is an event flag:
  /// it will be true for only one frame after the event happens.
  public static bool ClickButtonUp {
    get {
      return instance != null ? instance.controllerState.clickButtonUp : false;
    }
  }

  /// If true, the app button (touchpad button) is currently being pressed. This is not
  /// an event: it represents the button's state (it remains true while the button is being
  /// pressed).
  public static bool AppButton {
    get {
      return instance != null ? instance.controllerState.appButtonState : false;
    }
  }

  /// If true, the app button was just pressed. This is an event flag: it will be true for
  /// only one frame after the event happens.
  public static bool AppButtonDown {
    get {
      return instance != null ? instance.controllerState.appButtonDown : false;
    }
  }

  /// If true, the app button was just released. This is an event flag: it will be true for
  /// only one frame after the event happens.
  public static bool AppButtonUp {
    get {
      return instance != null ? instance.controllerState.appButtonUp : false;
    }
  }

  /// If State == GvrConnectionState.Error, this contains details about the error.
  public static string ErrorDetails {
    get {
      if (instance != null) {
        return instance.controllerState.connectionState == GvrConnectionState.Error ?
            instance.controllerState.errorDetails : "";
      } else {
        return "GvrController instance not found in scene. It may be missing, or it might "
            + "not have initialized yet.";
      }
    }
  }

  void Awake() {
    if (instance != null) {
      Debug.LogError("More than one GvrController instance was found in your scene. "
          + "Ensure that there is only one GvrController.");
      this.enabled = false;
      return;
    }
    instance = this;
    if (controllerProvider == null) {
      controllerProvider = ControllerProviderFactory.CreateControllerProvider(this);
    }
  }

  void OnDestroy() {
    instance = null;
  }

  private void UpdateController() {
    controllerProvider.ReadState(controllerState);

    // If the controller was recentered, also recenter the headset.
    if (controllerState.recentered) {
      GvrViewer sdk = GvrViewer.Instance;
      if (sdk) {
        sdk.Recenter();
      }
    }
  }

  void OnApplicationPause(bool paused) {
    Debug.Log("GvrController: application " + (paused ? "paused" : "resumed"));
    if (null == controllerProvider) return;
    if (paused) {
      controllerProvider.OnPause();
    } else {
      controllerProvider.OnResume();
    }
  }

  void OnEnable() {
    StartCoroutine("EndOfFrame");
  }

  void OnDisable() {
    StopCoroutine("EndOfFrame");
  }

  IEnumerator EndOfFrame() {
    object eof = new WaitForEndOfFrame();
    while (true) {
      // This must be done at the end of the frame to ensure that all GameObjects had a chance
      // to read transient controller state (e.g. events, etc) for the current frame before
      // it gets reset.
      UpdateController();
      yield return eof;
    }
  }
}
