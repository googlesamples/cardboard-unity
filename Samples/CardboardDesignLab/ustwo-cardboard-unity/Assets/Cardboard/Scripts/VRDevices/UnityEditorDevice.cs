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
#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

// Sends simulated values for use when testing within the Unity Editor.
public class UnityEditorDevice : BaseVRDevice {
  // Simulated neck model.  Vector from the neck pivot point to the point between the eyes.
  private static readonly Vector3 neckOffset = new Vector3(0, 0.075f, 0.08f);

  // Use mouse to emulate head in the editor.
  private float mouseX = 0;
  private float mouseY = 0;
  private float mouseZ = 0;

  public override void Init() {
    Input.gyro.enabled = true;
  }

  public override bool SupportsNativeDistortionCorrection(List<string> diagnostics) {
    return false;  // No need for diagnostic message.
  }

  public override bool SupportsNativeUILayer(List<string> diagnostics) {
    return false;  // No need for diagnostic message.
  }

  // Since we can check all these settings by asking Cardboard.SDK, no need
  // to keep a separate copy here.
  public override void SetUILayerEnabled(bool enabled) {}
  public override void SetVRModeEnabled(bool enabled) {}
  public override void SetDistortionCorrectionEnabled(bool enabled) {}
  public override void SetStereoScreen(RenderTexture stereoScreen) {}
  public override void SetSettingsButtonEnabled(bool enabled) {}
  public override void SetAlignmentMarkerEnabled(bool enabled) {}
  public override void SetVRBackButtonEnabled(bool enabled) {}
  public override void SetShowVrBackButtonOnlyInVR(bool only) {}
  public override void SetNeckModelScale(float scale) {}
  public override void SetAutoDriftCorrectionEnabled(bool enabled) {}
  public override void SetElectronicDisplayStabilizationEnabled(bool enabled) {}
  public override void SetTapIsTrigger(bool enabled) {}

  private Quaternion initialRotation = Quaternion.identity;

  private bool remoteCommunicating = false;
  private bool RemoteCommunicating {
    get {
      if (!remoteCommunicating) {
#if UNITY_5
        remoteCommunicating = EditorApplication.isRemoteConnected;
#else
        remoteCommunicating = Vector3.Dot(Input.gyro.rotationRate, Input.gyro.rotationRate) > 0.05;
#endif
      }
      return remoteCommunicating;
    }
  }

  public override void UpdateState() {
    Quaternion rot;
    if (Cardboard.SDK.UseUnityRemoteInput && RemoteCommunicating) {
      var att = Input.gyro.attitude * initialRotation;
      att = new Quaternion(att.x, att.y, -att.z, -att.w);
      rot = Quaternion.Euler(90, 0, 0) * att;
    } else {
      bool rolled = false;
      if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)) {
        mouseX += Input.GetAxis("Mouse X") * 5;
        if (mouseX <= -180) {
          mouseX += 360;
        } else if (mouseX > 180) {
          mouseX -= 360;
        }
        mouseY -= Input.GetAxis("Mouse Y") * 2.4f;
        mouseY = Mathf.Clamp(mouseY, -85, 85);
      } else if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) {
        rolled = true;
        mouseZ += Input.GetAxis("Mouse X") * 5;
        mouseZ = Mathf.Clamp(mouseZ, -85, 85);
      }
      if (!rolled && Cardboard.SDK.autoUntiltHead) {
        // People don't usually leave their heads tilted to one side for long.
        mouseZ = Mathf.Lerp(mouseZ, 0, Time.deltaTime / (Time.deltaTime + 0.1f));
      }
      rot = Quaternion.Euler(mouseY, mouseX, mouseZ);
    }
    var neck = (rot * neckOffset - neckOffset.y * Vector3.up) * Cardboard.SDK.NeckModelScale;
    headPose.Set(neck, rot);

    triggered = Input.GetMouseButtonDown(0);
    tilted = Input.GetKeyUp(KeyCode.Escape);
  }

  public override void PostRender() {
    // Do nothing.
  }

  public override void UpdateScreenData() {
    Profile = CardboardProfile.GetKnownProfile(Cardboard.SDK.ScreenSize, Cardboard.SDK.DeviceType);
    ComputeEyesFromProfile();
    profileChanged = true;
  }

  public override void Recenter() {
    mouseX = mouseZ = 0;  // Do not reset pitch, which is how it works on the phone.
    if (RemoteCommunicating) {
      //initialRotation = Quaternion.Inverse(Input.gyro.attitude);
    }
  }
}

#endif
