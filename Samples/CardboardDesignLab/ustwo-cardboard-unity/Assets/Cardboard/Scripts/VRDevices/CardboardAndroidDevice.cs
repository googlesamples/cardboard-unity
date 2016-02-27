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
#if UNITY_ANDROID

using UnityEngine;

public class CardboardAndroidDevice : BaseCardboardDevice {
  private const string ActivityListenerClass =
      "com.google.vr.platform.unity.UnityVrActivityListener";

  private static AndroidJavaObject activityListener;

  public override void Init() {
    SetApplicationState();
    base.Init();
    ConnectToActivity();
  }

  protected override void ConnectToActivity() {
    base.ConnectToActivity();
    if (androidActivity != null && activityListener == null) {
      activityListener = Create(ActivityListenerClass);
    }
  }

  // Returns landscape orientation display metrics.
  public override DisplayMetrics GetDisplayMetrics() {
    using (var listenerClass = GetClass(ActivityListenerClass)) {
      // Sadly some Android devices still don't report accurate values.  If this
      // doesn't work correctly on your device, comment out this function to try
      // the Unity implementation.
      float[] metrics = listenerClass.CallStatic<float[]>("getDisplayMetrics");
      // Always return landscape orientation.
      int width, height;
      if (metrics[0] > metrics[1]) {
        width = (int)metrics[0];
        height = (int)metrics[1];
      } else {
        width = (int)metrics[1];
        height = (int)metrics[0];
      }
      // DPI-x (metrics[2]) on Android appears to refer to the narrow dimension of the screen.
      return new DisplayMetrics { width = width, height = height, xdpi = metrics[3], ydpi = metrics[2] };
    }
  }

  public override void SetUILayerEnabled(bool enabled) {
    CallObjectMethod(activityListener, "setUILayerEnabled", enabled);
  }

  public override void SetVRModeEnabled(bool enabled) {
    CallObjectMethod(activityListener, "setVRModeEnabled", enabled);
  }

  public override void SetSettingsButtonEnabled(bool enabled) {
    CallObjectMethod(activityListener, "setSettingsButtonEnabled", enabled);
  }

  public override void SetAlignmentMarkerEnabled(bool enabled) {
    CallObjectMethod(activityListener, "setAlignmentMarkerEnabled", enabled);
  }

  public override void SetVRBackButtonEnabled(bool enabled) {
    CallObjectMethod(activityListener, "setVRBackButtonEnabled", enabled);
  }

  public override void SetShowVrBackButtonOnlyInVR(bool only) {
    CallObjectMethod(activityListener, "setShowVrBackButtonOnlyInVR", only);
  }

  public override void SetTapIsTrigger(bool enabled) {
    CallObjectMethod(activityListener, "setTapIsTrigger", enabled);
  }

  public override void SetTouchCoordinates(int x, int y) {
    CallObjectMethod(activityListener, "setTouchCoordinates", x, y);
  }

  public override void ShowSettingsDialog() {
    CallObjectMethod(activityListener, "launchConfigureActivity");
  }

  protected override void ProcessEvents() {
    base.ProcessEvents();
    if (!Cardboard.SDK.TapIsTrigger) {
      if (triggered) {
        CallObjectMethod(activityListener, "injectSingleTap");
      }
      if (backButtonPressed) {
        CallObjectMethod(activityListener, "injectKeyPress", 111);  // Escape key.
      }
    }
  }

  public override void OnPause(bool pause) {
    base.OnPause(pause);
    CallObjectMethod(activityListener, "onPause", pause);
  }

  private void SetApplicationState() {
    if (activityListener == null) {
      using (var listenerClass = GetClass(ActivityListenerClass)) {
        CallStaticMethod(listenerClass, "setUnityApplicationState");
      }
    }
  }
}

#endif
