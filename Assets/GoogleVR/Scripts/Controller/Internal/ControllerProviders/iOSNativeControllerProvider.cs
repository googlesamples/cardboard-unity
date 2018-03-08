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
#if UNITY_IOS && !UNITY_EDITOR && GVR_IOS_DAYDREAM_CONTROLLER_ENABLED
using UnityEngine;

using System;
using System.Runtime.InteropServices;
using System.Timers;

/// @cond
namespace Gvr.Internal {

  /// Controller Provider that uses a native iOS plugin to communicate with a controller.
  /// In the iOS player settings, "XR Settings" -> "Virtual Reality SDKs" does not list "Daydream" as an option.
  /// So, to enable iOS Daydream controller support, include "Cardboard" in "Virtual Reality SDKs" and add the flag
  /// GVR_IOS_DAYDREAM_CONTROLLER_ENABLED in "Other Settings" -> "Scripting Define Symbols". This will make it so
  /// that the XCode project includes CoreBluetooth as a dependency and includes NSBluetoothPeripheralUsageDescription
  /// in its Info.plist.
  class iOSNativeControllerProvider : IControllerProvider {
    // enum gvr_controller_connection_state:
    private const int GVR_CONTROLLER_DISCONNECTED = 0;
    private const int GVR_CONTROLLER_SCANNING = 1;
    private const int GVR_CONTROLLER_CONNECTING = 2;
    private const int GVR_CONTROLLER_CONNECTED = 3;

    private enum controller_connection_state : int {
      DISCONNECTED = 0,
      SCANNING = 1,
      CONNECTING = 2,
      CONNECTED = 3
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct gvr_quat {
      internal float x;
      internal float y;
      internal float z;
      internal float w;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct gvr_vec3 {
      internal float x;
      internal float y;
      internal float z;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct gvr_vec2 {
      internal float x;
      internal float y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct daydream_controller_state {
      internal controller_connection_state connectionState;
      internal gvr_quat orientation;
      internal gvr_vec3 accel;
      internal gvr_vec3 gyro;
      internal gvr_vec2 touchPos;
      [MarshalAs(UnmanagedType.U1)]
      internal bool isTouching;
      [MarshalAs(UnmanagedType.U1)]
      internal bool appButtonState;
      [MarshalAs(UnmanagedType.U1)]
      internal bool homeButtonState;
      [MarshalAs(UnmanagedType.U1)]
      internal bool clickButtonState;
      [MarshalAs(UnmanagedType.U1)]
      internal bool plusButtonState;
      [MarshalAs(UnmanagedType.U1)]
      internal bool minusButtonState;
      [MarshalAs(UnmanagedType.U1)]
      internal bool supportsBatteryStatus;
      internal byte batteryLevelPercentage;
    }

    [DllImport ("__Internal")]
    private static extern daydream_controller_state DaydreamControllerPlugin_getState();

    [DllImport ("__Internal")]
    private static extern void DaydreamControllerPlugin_pause();

    [DllImport ("__Internal")]
    private static extern void DaydreamControllerPlugin_resume();

    [DllImport ("__Internal")]
    private static extern void DaydreamControllerPlugin_start();

    private bool supportsBatteryStatus = false;

    private MutablePose3D pose3d = new MutablePose3D();

    private bool lastTouchState;
    private bool lastButtonState;
    private bool lastAppButtonState;
    private bool lastHomeButtonState;

    // Y axis rotation offset for recentering.
    private float yAxisRotationOffset = 0;
    private Timer recenterTimer = new Timer();
    private bool recenterOnNextReadState = false;

    public iOSNativeControllerProvider() {
      // Timer to wait for the home button to be held down 1 second before recentering.
      recenterTimer.Interval = 1000;
      recenterTimer.AutoReset = false;
      recenterTimer.Elapsed += RecenterTimer_Elapsed;
      DaydreamControllerPlugin_start();
    }

    public bool SupportsBatteryStatus { get { return supportsBatteryStatus; }}

    /// Notifies the controller provider that the application has paused.
    public void OnPause() {

      DaydreamControllerPlugin_pause();
    }

    /// Notifies the controller provider that the application has resumed.
    public void OnResume() {

      DaydreamControllerPlugin_resume();
    }

    /// Reads the controller's current state and stores it in outState.
    public void ReadState(ControllerState outState) {
      outState.apiStatus = GvrControllerApiStatus.Ok;
      daydream_controller_state newState = DaydreamControllerPlugin_getState();
      outState.connectionState = ConvertConnectionState(newState.connectionState);

      gvr_quat rawOri = newState.orientation;
      gvr_vec3 rawAccel = newState.accel;
      gvr_vec3 rawGyro = newState.gyro;

      // Convert GVR API orientation (right-handed) into Unity axis system (left-handed).
      pose3d.Set(Vector3.zero, new Quaternion(rawOri.x, rawOri.y, rawOri.z, rawOri.w));
      pose3d.SetRightHanded(pose3d.Matrix);

      // The orientation without the Y rotation offset from the last recenter event.
      var orientationBeforeYAxisOffset = pose3d.Orientation.eulerAngles;

      // For accelerometer, we have to flip Z because the GVR API has Z pointing backwards
      // and Unity has Z pointing forward.
      outState.accel = new Vector3(rawAccel.x, rawAccel.y, -rawAccel.z);

      outState.gyro = new Vector3(-rawGyro.x, -rawGyro.y, rawGyro.z);

      outState.isTouching = newState.isTouching;

      outState.touchPos = new Vector2(newState.touchPos.x, newState.touchPos.y);

      outState.appButtonState = newState.appButtonState;
      outState.homeButtonState = newState.homeButtonState;
      outState.clickButtonState = newState.clickButtonState;

      UpdateInputEvents(outState.isTouching, ref lastTouchState,
        ref outState.touchUp, ref outState.touchDown);
      UpdateInputEvents(outState.clickButtonState, ref lastButtonState,
        ref outState.clickButtonUp, ref outState.clickButtonDown);
      UpdateInputEvents(outState.appButtonState, ref lastAppButtonState,
        ref outState.appButtonUp, ref outState.appButtonDown);
      UpdateInputEvents(outState.homeButtonState, ref lastHomeButtonState,
        ref outState.homeButtonUp, ref outState.homeButtonDown);

      if (outState.homeButtonDown) {
        recenterTimer.Start();
      }
      if (outState.homeButtonUp) {
        recenterTimer.Stop();
      }

      outState.recentered = recenterOnNextReadState;
      if (outState.recentered) {
        recenterOnNextReadState = false;
        GvrCardboardHelpers.Recenter();
        yAxisRotationOffset = orientationBeforeYAxisOffset.y;
      }
      outState.orientation = Quaternion.Euler(orientationBeforeYAxisOffset.x, orientationBeforeYAxisOffset.y - yAxisRotationOffset, orientationBeforeYAxisOffset.z);

      supportsBatteryStatus = newState.supportsBatteryStatus;
      if (supportsBatteryStatus) {
        outState.batteryLevel = ConvertBatteryLevel(newState.batteryLevelPercentage);
      }
    }

    private void RecenterTimer_Elapsed(object sender, ElapsedEventArgs e) {
      // The user has been holding down the home button for long enough to trigger a recenter.
      recenterOnNextReadState = true;
    }

    private static void UpdateInputEvents(bool currentState, ref bool previousState, ref bool up, ref bool down) {

      down = !previousState && currentState;
      up = previousState && !currentState;

      previousState = currentState;
    }

    private GvrControllerBatteryLevel ConvertBatteryLevel(byte batteryLevelPercentage) {
      if (batteryLevelPercentage >= 80) {
        return GvrControllerBatteryLevel.Full;
      } else if (batteryLevelPercentage >= 60) {
        return GvrControllerBatteryLevel.AlmostFull;
      } else if (batteryLevelPercentage >= 40) {
        return GvrControllerBatteryLevel.Medium;
      } else if (batteryLevelPercentage >= 20) {
        return GvrControllerBatteryLevel.Low;
      } else {
        return GvrControllerBatteryLevel.CriticalLow;
      }
    }

    private GvrConnectionState ConvertConnectionState(controller_connection_state connectionState) {
      switch (connectionState) {
        case controller_connection_state.CONNECTED:
          return GvrConnectionState.Connected;
        case controller_connection_state.CONNECTING:
          return GvrConnectionState.Connecting;
        case controller_connection_state.SCANNING:
          return GvrConnectionState.Scanning;
        default:
          return GvrConnectionState.Disconnected;
      }
    }
  }
}
/// @endcond

#endif // UNITY_IOS && !UNITY_EDITOR && GVR_IOS_DAYDREAM_CONTROLLER_ENABLED
