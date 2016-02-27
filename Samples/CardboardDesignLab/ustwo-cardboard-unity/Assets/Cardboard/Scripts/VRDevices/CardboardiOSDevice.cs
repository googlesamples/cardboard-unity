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
#if UNITY_IOS

using System.Runtime.InteropServices;
using System.Collections.Generic;
using UnityEngine;

public class CardboardiOSDevice : BaseCardboardDevice {
  // Native code libraries use OpenGL, but Unity picks Metal for iOS by default.
  bool isOpenGL = false;

  // Returns landscape orientation display metrics.
  public override DisplayMetrics GetDisplayMetrics() {
    // Always return landscape orientation.
    int width = Mathf.Max(Screen.width, Screen.height);
    int height = Mathf.Min(Screen.width, Screen.height);
    float dpi = getScreenDPI();
    return new DisplayMetrics { width = width, height = height, xdpi = dpi, ydpi = dpi };
  }

  public override bool SupportsNativeDistortionCorrection(List<string> diagnostics) {
    bool support = base.SupportsNativeDistortionCorrection(diagnostics);
    if (!isOpenGL) {
      diagnostics.Add("Requires OpenGL");
      support = false;
    }
    return support;
  }

  public override void SetUILayerEnabled(bool enabled) {
    setUILayerEnabled(enabled);
  }

  public override void SetVRModeEnabled(bool enabled) {
    setVRModeEnabled(enabled);
  }

  public override void SetSettingsButtonEnabled(bool enabled) {
    setSettingsButtonEnabled(enabled);
  }

  public override void SetAlignmentMarkerEnabled(bool enabled) {
    setAlignmentMarkerEnabled(enabled);
  }

  public override void SetVRBackButtonEnabled(bool enabled) {
    setVRBackButtonEnabled(enabled);
  }

  public override void SetShowVrBackButtonOnlyInVR(bool only) {
    setShowVrBackButtonOnlyInVR(only);
  }

  public override void SetAutoDriftCorrectionEnabled(bool enabled){
    // For iOS don't use Drift Correction.
    base.SetAutoDriftCorrectionEnabled(false);
  }

  public override void SetTapIsTrigger(bool enabled) {
    // Not supported on iOS.
  }

  public override bool SetDefaultDeviceProfile(System.Uri uri) {
    bool result = base.SetDefaultDeviceProfile(uri);
    if (result) {
      setOnboardingDone();
    }
    return result;
  }

  public override void Init() {
    isOpenGL = isOpenGLAPI();
    setSyncWithCardboardEnabled(Cardboard.SDK.SyncWithCardboardApp);
    base.Init();
    // For iOS don't use Drift Correction.
    SetAutoDriftCorrectionEnabled(false);
  }

  // Set this to true to force an onboarding process.
  private bool debugOnboarding = false;

  public void ShowOnboardingDialog() {
    if (debugOnboarding || !isOnboardingDone()) {
      debugOnboarding = false;
      launchOnboardingDialog();
      setOnboardingDone();
    }
  }

  public override void ShowSettingsDialog() {
    launchSettingsDialog();
  }

  [DllImport("__Internal")]
  private static extern bool isOpenGLAPI();

  [DllImport("__Internal")]
  private static extern void setUILayerEnabled(bool enabled);

  [DllImport("__Internal")]
  private static extern void setVRModeEnabled(bool enabled);

  [DllImport("__Internal")]
  private static extern void setShowVrBackButtonOnlyInVR(bool only);

  [DllImport("__Internal")]
  private static extern void setSettingsButtonEnabled(bool enabled);

  [DllImport("__Internal")]
  private static extern void setAlignmentMarkerEnabled(bool enabled);

  [DllImport("__Internal")]
  private static extern void setVRBackButtonEnabled(bool enabled);

  [DllImport("__Internal")]
  private static extern void setSyncWithCardboardEnabled(bool enabled);

  [DllImport("__Internal")]
  private static extern void setOnboardingDone();

  [DllImport("__Internal")]
  private static extern bool isOnboardingDone();

  [DllImport("__Internal")]
  private static extern void launchSettingsDialog();

  [DllImport("__Internal")]
  private static extern void launchOnboardingDialog();

  [DllImport("__Internal")]
  private static extern float getScreenDPI();
}

#endif
