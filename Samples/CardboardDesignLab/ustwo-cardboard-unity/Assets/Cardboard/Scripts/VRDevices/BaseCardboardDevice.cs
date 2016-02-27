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

using AOT;
using System.Runtime.InteropServices;
using System.Collections.Generic;

public abstract class BaseCardboardDevice :
#if UNITY_ANDROID
BaseAndroidDevice
#else
BaseVRDevice
#endif
{
  // A relatively unique id to use when calling our C++ native render plugin.
  private const int kCardboardRenderEvent = 0x47554342;

  // Event IDs sent up from native layer.
  private const int kTriggered = 1;
  private const int kTilted = 2;
  private const int kProfileChanged = 3;
  private const int kVRBackButton = 4;

  private float[] headData = new float[16];
  private float[] viewData = new float[16 * 6 + 10];
  private float[] profileData = new float[13];

  private Matrix4x4 headView = new Matrix4x4();
  private Matrix4x4 leftEyeView = new Matrix4x4();
  private Matrix4x4 rightEyeView = new Matrix4x4();

  private Queue<int> eventQueue = new Queue<int>();

  protected bool debugDisableNativeProjections = false;
  protected bool debugDisableNativeDistortion = false;
  protected bool debugDisableNativeUILayer = false;

  public override bool SupportsNativeDistortionCorrection(List<string> diagnostics) {
    bool supported = base.SupportsNativeDistortionCorrection(diagnostics);
    if (debugDisableNativeDistortion) {
      supported = false;
      diagnostics.Add("Debug override");
    }
    return supported;
  }

  public override void SetDistortionCorrectionEnabled(bool enabled) {
    EnableDistortionCorrection(enabled);
  }

  public override void SetNeckModelScale(float scale) {
    SetNeckModelFactor(scale);
  }

  public override void SetAutoDriftCorrectionEnabled(bool enabled) {
    EnableAutoDriftCorrection(enabled);
  }

  public override void SetElectronicDisplayStabilizationEnabled(bool enabled) {
    EnableElectronicDisplayStabilization(enabled);
  }

  public override bool SetDefaultDeviceProfile(System.Uri uri) {
    byte[] profile = System.Text.Encoding.UTF8.GetBytes(uri.ToString());
    return SetDefaultProfile(profile, profile.Length);
  }

  public override void Init() {
    DisplayMetrics dm = GetDisplayMetrics();

    // Start will send a log event, so SetUnityVersion first.
    byte[] version = System.Text.Encoding.UTF8.GetBytes(Application.unityVersion);
    SetUnityVersion(version, version.Length);
    Start(dm.width, dm.height, dm.xdpi, dm.ydpi);
    SetEventCallback(OnVREvent);
  }

  public override void SetStereoScreen(RenderTexture stereoScreen) {
#if UNITY_5 || !UNITY_IOS
    SetTextureId(stereoScreen != null ? (int)stereoScreen.GetNativeTexturePtr() : 0);
#else
    // Using old API for Unity 4.x and iOS because Metal crashes on GetNativeTexturePtr()
    SetTextureId(stereoScreen != null ? stereoScreen.GetNativeTextureID() : 0);
#endif
  }

  public override void UpdateState() {
    ProcessEvents();
    GetHeadPose(headData);
    ExtractMatrix(ref headView, headData);
    headPose.SetRightHanded(headView.inverse);
  }

  public override void UpdateScreenData() {
    UpdateProfile();
    if (debugDisableNativeProjections) {
      ComputeEyesFromProfile();
    } else {
      UpdateView();
    }
    profileChanged = true;
  }

  public override void Recenter() {
    ResetHeadTracker();
  }

  public override void PostRender() {
    GL.IssuePluginEvent(kCardboardRenderEvent);
  }

  public override void OnPause(bool pause) {
    if (pause) {
      Pause();
    } else {
      Resume();
    }
  }

  public override void OnApplicationQuit() {
    Stop();
    base.OnApplicationQuit();
  }

  private void UpdateView() {
    GetViewParameters(viewData);
    int j = 0;

    j = ExtractMatrix(ref leftEyeView, viewData, j);
    j = ExtractMatrix(ref rightEyeView, viewData, j);
    leftEyePose.SetRightHanded(leftEyeView.inverse);
    rightEyePose.SetRightHanded(rightEyeView.inverse);

    j = ExtractMatrix(ref leftEyeDistortedProjection, viewData, j);
    j = ExtractMatrix(ref rightEyeDistortedProjection, viewData, j);
    j = ExtractMatrix(ref leftEyeUndistortedProjection, viewData, j);
    j = ExtractMatrix(ref rightEyeUndistortedProjection, viewData, j);

    leftEyeUndistortedViewport.Set(viewData[j], viewData[j+1], viewData[j+2], viewData[j+3]);
    leftEyeDistortedViewport = leftEyeUndistortedViewport;
    j += 4;

    rightEyeUndistortedViewport.Set(viewData[j], viewData[j+1], viewData[j+2], viewData[j+3]);
    rightEyeDistortedViewport = rightEyeUndistortedViewport;
    j += 4;

    recommendedTextureSize = new Vector2(viewData[j], viewData[j+1]);
    j += 2;
  }

  private void UpdateProfile() {
    GetProfile(profileData);
    CardboardProfile.Device device = new CardboardProfile.Device();
    CardboardProfile.Screen screen = new CardboardProfile.Screen();
    device.maxFOV.outer = profileData[0];
    device.maxFOV.upper = profileData[1];
    device.maxFOV.inner = profileData[2];
    device.maxFOV.lower = profileData[3];
    screen.width = profileData[4];
    screen.height = profileData[5];
    screen.border = profileData[6];
    device.lenses.separation = profileData[7];
    device.lenses.offset = profileData[8];
    device.lenses.screenDistance = profileData[9];
    device.lenses.alignment = (int)profileData[10];
    device.distortion.k1 = profileData[11];
    device.distortion.k2 = profileData[12];
    device.inverse = CardboardProfile.ApproximateInverse(device.distortion);
    Profile.screen = screen;
    Profile.device = device;
  }

  private static int ExtractMatrix(ref Matrix4x4 mat, float[] data, int i = 0) {
    // Matrices returned from our native layer are in row-major order.
    for (int r = 0; r < 4; r++) {
      for (int c = 0; c < 4; c++, i++) {
        mat[r, c] = data[i];
      }
    }
    return i;
  }

  private int[] events = new int[4];

  protected virtual void ProcessEvents() {
    int num = 0;
    lock (eventQueue) {
      num = eventQueue.Count;
      if (num == 0) {
        return;
      }
      if (num > events.Length) {
        events = new int[num];
      }
      eventQueue.CopyTo(events, 0);
      eventQueue.Clear();
    }
    for (int i = 0; i < num; i++) {
      switch (events[i]) {
        case kTriggered:
          triggered = true;
          break;
        case kTilted:
          tilted = true;
          break;
        case kProfileChanged:
          UpdateScreenData();
          break;
        case kVRBackButton:
          backButtonPressed = true;
          break;
      }
    }
  }

  [MonoPInvokeCallback(typeof(VREventCallback))]
  private static void OnVREvent(int eventID) {
    BaseCardboardDevice device = GetDevice() as BaseCardboardDevice;
    // This function is called back from random native code threads.
    lock (device.eventQueue) {
      device.eventQueue.Enqueue(eventID);
    }
  }

#if UNITY_IOS
  private const string dllName = "__Internal";
#else
  private const string dllName = "vrunity";
#endif

  delegate void VREventCallback(int eventID);

  [DllImport(dllName)]
  private static extern void Start(int width, int height, float xdpi, float ydpi);

  [DllImport(dllName)]
  private static extern void SetEventCallback(VREventCallback callback);

  [DllImport(dllName)]
  private static extern void SetTextureId(int id);

  [DllImport(dllName)]
  private static extern bool SetDefaultProfile(byte[] uri, int size);

  [DllImport(dllName)]
  private static extern void SetUnityVersion(byte[] version_str, int version_length);

  [DllImport(dllName)]
  private static extern void EnableDistortionCorrection(bool enable);

  [DllImport(dllName)]
  private static extern void EnableAutoDriftCorrection(bool enable);

  [DllImport(dllName)]
  private static extern void EnableElectronicDisplayStabilization(bool enable);

  [DllImport(dllName)]
  private static extern void SetNeckModelFactor(float factor);

  [DllImport(dllName)]
  private static extern void ResetHeadTracker();

  [DllImport(dllName)]
  private static extern void GetProfile(float[] profile);

  [DllImport(dllName)]
  private static extern void GetHeadPose(float[] pose);

  [DllImport(dllName)]
  private static extern void GetViewParameters(float[] viewParams);

  [DllImport(dllName)]
  private static extern void Pause();

  [DllImport(dllName)]
  private static extern void Resume();

  [DllImport(dllName)]
  private static extern void Stop();
}
