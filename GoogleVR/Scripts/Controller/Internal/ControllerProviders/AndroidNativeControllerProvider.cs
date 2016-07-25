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
// See the License for the specific language governing permissioßns and
// limitations under the License.

#if UNITY_ANDROID
using UnityEngine;

using System;
using System.Runtime.InteropServices;

/// @cond
namespace Gvr.Internal {
  /// Controller Provider that uses the native GVR C API to communicate with controllers
  /// via Google VR Services on Android.
  class AndroidNativeControllerProvider : IControllerProvider {
    // Note: keep structs and function signatures in sync with the C header file (gvr_controller.h).
    [StructLayout(LayoutKind.Sequential)]
    private struct gvr_controller_api_options {
      internal byte enable_orientation;
      internal byte enable_touch;
      internal byte enable_gyro;
      internal byte enable_accel;
      internal byte enable_gestures;
    };

    // enum gvr_controller_button:
    private const int GVR_CONTROLLER_BUTTON_NONE = 0;
    private const int GVR_CONTROLLER_BUTTON_CLICK = 1;
    private const int GVR_CONTROLLER_BUTTON_HOME = 2;
    private const int GVR_CONTROLLER_BUTTON_APP = 3;
    private const int GVR_CONTROLLER_BUTTON_VOLUME_UP = 4;
    private const int GVR_CONTROLLER_BUTTON_VOLUME_DOWN = 5;
    private const int GVR_CONTROLLER_BUTTON_COUNT = 6;

    // enum gvr_controller_connection_state:
    private const int GVR_CONTROLLER_DISCONNECTED = 0;
    private const int GVR_CONTROLLER_SCANNING = 1;
    private const int GVR_CONTROLLER_CONNECTING = 2;
    private const int GVR_CONTROLLER_CONNECTED = 3;

    // enum gvr_controller_api_status
    private const int GVR_CONTROLLER_API_OK = 0;

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
    private struct gvr_controller_state {
      internal int api_status;
      internal int connection_state;
      internal gvr_quat orientation;
      internal gvr_vec3 gyro;
      internal gvr_vec3 accel;
      internal byte is_touching;
      internal gvr_vec2 touch_pos;
      internal byte touch_down;
      internal byte touch_up;
      internal byte recentered;
      internal byte recentering;
      internal byte button_state_none;
      internal byte button_state_click;
      internal byte button_state_home;
      internal byte button_state_app;
      internal byte button_state_volume_up;
      internal byte button_state_volume_down;
      internal byte button_down_none;
      internal byte button_down_click;
      internal byte button_down_home;
      internal byte button_down_app;
      internal byte button_down_volume_up;
      internal byte button_down_volume_down;
      internal byte button_up_none;
      internal byte button_up_click;
      internal byte button_up_home;
      internal byte button_up_app;
      internal byte button_up_volume_up;
      internal byte button_up_volume_down;
      internal long last_orientation_timestamp;
      internal long last_gyro_timestamp;
      internal long last_accel_timestamp;
      internal long last_touch_timestamp;
      internal long last_button_timestamp;
    }

    private const string dllName = "gvrunity";

    [DllImport(dllName)]
    private static extern void gvr_controller_init_default_options(
        ref gvr_controller_api_options options);

    [DllImport(dllName)]
    private static extern IntPtr gvr_controller_create_and_init_android(
        IntPtr jniEnv, IntPtr androidContext, IntPtr classLoader,
        ref gvr_controller_api_options options, IntPtr context);

    [DllImport(dllName)]
    private static extern void gvr_controller_destroy(ref IntPtr api);

    [DllImport(dllName)]
    private static extern void gvr_controller_pause(IntPtr api);

    [DllImport(dllName)]
    private static extern void gvr_controller_resume(IntPtr api);

    [DllImport(dllName)]
    private static extern void gvr_controller_read_state(
        IntPtr api, ref gvr_controller_state out_state);

    private const string UNITY_PLAYER_CLASS = "com.unity3d.player.UnityPlayer";

    private IntPtr api;

    private gvr_controller_api_options options;

    private AndroidJavaObject androidContext;
    private AndroidJavaObject classLoader;

    private bool error;
    private String errorDetails;

    private gvr_controller_state state = new gvr_controller_state();

    internal AndroidNativeControllerProvider(bool enableGyro, bool enableAccel) {
      Debug.Log("Initializing Daydream controller API.");

      options = new gvr_controller_api_options();
      gvr_controller_init_default_options(ref options);
      options.enable_accel = enableAccel ? (byte) 1 : (byte) 0;
      options.enable_gyro = enableGyro ? (byte) 1 : (byte) 0;

      // Get a hold of the activity, context and class loader.
      AndroidJavaObject activity = GetActivity();
      if (activity == null) {
        error = true;
        errorDetails = "Failed to get Activity from Unity Player.";
        return;
      }
      androidContext = GetApplicationContext(activity);
      if (androidContext == null) {
        error = true;
        errorDetails = "Failed to get Android application context from Activity.";
        return;
      }
      classLoader = GetClassLoaderFromActivity(activity);
      if (classLoader == null) {
        error = true;
        errorDetails = "Failed to get class loader from Activity.";
        return;
      }

      Debug.Log("Creating and initializing GVR API controller object.");
      api = gvr_controller_create_and_init_android(IntPtr.Zero, androidContext.GetRawObject(),
          classLoader.GetRawObject(), ref options, IntPtr.Zero);
      if (IntPtr.Zero == api) {
        Debug.LogError("Error creating/initializing Daydream controller API.");
        error = true;
        errorDetails = "Failed to initialize Daydream controller API.";
        return;
      }
      Debug.Log("GVR API successfully initialized. Now resuming it.");
      gvr_controller_resume(api);
      Debug.Log("GVR API resumed.");
    }

    public void ReadState(ControllerState outState) {
      if (error) {
        outState.connectionState = GvrConnectionState.Error;
        outState.errorDetails = errorDetails;
        return;
      }
      gvr_controller_read_state(api, ref state);

      outState.connectionState = ConvertConnectionState(state.connection_state);

      // Note: for accelerometer, gyro and orientation, we have to convert from the space used by
      // the GVR API to Unity space. They are different.
      //    GVR API:   X = right, Y = up, Z = back, right-handed.
      //    Unity:     X = right, Y = up, Z = forward, left-handed
      //
      // So for orientation and gyro, we must invert the signs of X, Y, and Z due to chiral
      // conversion, and then must flip Z because of the difference in the Z axis direction.
      // So, in the end, the conversion is: -x, -y, z.
      //
      // For the accelerometer, there is no chirality conversion because it doesn't express
      // a rotation. But we still need to flip Z.
      outState.accel = new Vector3(state.accel.x, state.accel.y, -state.accel.z);
      outState.gyro = new Vector3(-state.gyro.x, -state.gyro.y, state.gyro.z);
      outState.orientation = new Quaternion(
          -state.orientation.x, -state.orientation.y, state.orientation.z, state.orientation.w);

      outState.isTouching = 0 != state.is_touching;
      outState.touchPos = new Vector2(state.touch_pos.x, state.touch_pos.y);
      outState.touchDown = 0 != state.touch_down;
      outState.touchUp = 0 != state.touch_up;

      outState.appButtonDown = 0 != state.button_down_app;
      outState.appButtonState = 0 != state.button_state_app;
      outState.appButtonUp = 0 != state.button_up_app;
      outState.clickButtonDown = 0 != state.button_down_click;
      outState.clickButtonState = 0 != state.button_state_click;
      outState.clickButtonUp = 0 != state.button_up_click;
      outState.recentering = 0 != state.recentering;
      outState.recentered = 0 != state.recentered;
    }

    public void OnPause() {
      if (IntPtr.Zero != api) {
        gvr_controller_pause(api);
      }
    }

    public void OnResume() {
      if (IntPtr.Zero != api) {
        gvr_controller_resume(api);
      }
    }

    private GvrConnectionState ConvertConnectionState(int connectionState) {
      switch (connectionState) {
        case GVR_CONTROLLER_CONNECTED:
          return GvrConnectionState.Connected;
        case GVR_CONTROLLER_CONNECTING:
          return GvrConnectionState.Connecting;
        case GVR_CONTROLLER_SCANNING:
          return GvrConnectionState.Scanning;
        default:
          return GvrConnectionState.Disconnected;
      }
    }

    private static AndroidJavaObject GetActivity() {
      AndroidJavaClass jc = new AndroidJavaClass(UNITY_PLAYER_CLASS);
      if (jc == null) {
        Debug.LogErrorFormat("Failed to get Unity Player class, {0}", UNITY_PLAYER_CLASS);
        return null;
      }
      AndroidJavaObject activity = jc.GetStatic<AndroidJavaObject>("currentActivity");
      if (activity == null) {
        Debug.LogError("Failed to obtain Android Activity from Unity Player class.");
        return null;
      }
      return activity;
    }

    private static AndroidJavaObject GetApplicationContext(AndroidJavaObject activity) {
      AndroidJavaObject context = activity.Call<AndroidJavaObject>("getApplicationContext");
      if (context == null) {
        Debug.LogErrorFormat("Failed to get application context from Activity.");
        return null;
      }
      return context;
    }

    private static AndroidJavaObject GetClassLoaderFromActivity(AndroidJavaObject activity) {
      AndroidJavaObject result = activity.Call<AndroidJavaObject>("getClassLoader");
      if (result == null) {
        Debug.LogErrorFormat("Failed to get class loader from Activity.");
        return null;
      }
      return result;
    }
  }
}
/// @endcond

#endif
