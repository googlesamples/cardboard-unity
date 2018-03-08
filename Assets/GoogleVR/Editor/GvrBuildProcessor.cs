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

// Only invoke custom build processor when building for Android or iOS.
#if UNITY_ANDROID || UNITY_IOS
using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using System.Collections.Generic;
using System.IO;
using System.Linq;

#if UNITY_2017_2_OR_NEWER
using UnityEngine.XR;
#else
using XRSettings = UnityEngine.VR.VRSettings;
#endif  // UNITY_2017_2_OR_NEWER

// Notifies users if they build for Android or iOS without Cardboard or Daydream enabled
// and updates the generated Xcode project with the changes needed for the iOS controller plugin.
class GvrBuildProcessor : IPreprocessBuild {
  private const string VR_SETTINGS_NOT_ENABLED_ERROR_MESSAGE_FORMAT =
    "To use the Google VR SDK on {0}, 'Player Settings > Virtual Reality Supported' setting must be checked.\n" +
    "Please fix this setting and rebuild your app.";
  private const string IOS_MISSING_GVR_SDK_ERROR_MESSAGE =
    "To use the Google VR SDK on iOS, 'Player Settings > Virtual Reality SDKs' must include 'Cardboard'.\n" +
    "Please fix this setting and rebuild your app.";
  private const string ANDROID_MISSING_GVR_SDK_ERROR_MESSAGE =
    "To use the Google VR SDK on Android, 'Player Settings > Virtual Reality SDKs' must include 'Daydream' or 'Cardboard'.\n" +
    "Please fix this setting and rebuild your app.";

  private const string XCODE_PLUGIN_SOURCE_DIRECTORY = "Libraries/GoogleVR/Plugins/iOS/";

  private static readonly List<string> IOS_DAYDREAM_CONTROLLER_PLUGIN_FILE_NAMES = new List<string> {
    "daydream_controller_state.h",
    "daydream_controller_state.m",
    "DaydreamControllerPlugin.h",
    "DaydreamControllerPlugin.m",
    "GvrDaydreamController.h",
    "GvrDaydreamController.m"
  };

  public int callbackOrder {
    get { return 0; }
  }

  public void OnPreprocessBuild (BuildTarget target, string path)
  {
    if (target != BuildTarget.Android && target != BuildTarget.iOS) {
      // Do nothing when not building for Android or iOS.
      return;
    }

    // 'Player Settings > Virtual Reality Supported' must be enabled.
    if (!IsVRSupportEnabled()) {
      Debug.LogErrorFormat(VR_SETTINGS_NOT_ENABLED_ERROR_MESSAGE_FORMAT, target);
    }

    if (target == BuildTarget.Android) {
      // When building for Android at least one VR SDK must be included.
      // For Google VR valid VR SDKs are 'Daydream' and/or 'Cardboard'.
      if (!IsSDKOtherThanNoneIncluded()) {
        Debug.LogError(ANDROID_MISSING_GVR_SDK_ERROR_MESSAGE);
      }
    }

    if (target == BuildTarget.iOS) {
      // When building for iOS at least one VR SDK must be included.
      // For Google VR only 'Cardboard' is supported.
      if (!IsSDKOtherThanNoneIncluded()) {
        Debug.LogError(IOS_MISSING_GVR_SDK_ERROR_MESSAGE);
      }
    }
  }

  // OnPostprocessBuild is used to update the XCode project for changes specific to Daydream controller support on iOS.
  // If iOS Daydream controller support is enabled with the GVR_IOS_DAYDREAM_CONTROLLER_ENABLED flag (see iOSNativeControllerProvider),
  // then the dependencies for CoreBluetooth are added to the XCode project. If the flag is omitted (the default), then the
  // XCode project is updated to remove the files for the iOS Daydream controller plugin.
  [PostProcessBuild]
  public static void OnPostprocessBuild(BuildTarget target, string path) {

    if (target == BuildTarget.iOS) {
      string projectPath = path + "/Unity-iPhone.xcodeproj/project.pbxproj";
      PBXProject project = new PBXProject();
      project.ReadFromString(File.ReadAllText(projectPath));
      string targetGuid = project.TargetGuidByName("Unity-iPhone");

      #if GVR_IOS_DAYDREAM_CONTROLLER_ENABLED
        // Compile this file with GNU99 C, since that's required for GLKit.
        var fileGuid = project.FindFileGuidByProjectPath(XCODE_PLUGIN_SOURCE_DIRECTORY + "daydream_controller_state.m");
        var flags = project.GetCompileFlagsForFile(targetGuid, fileGuid);
        flags.Add("-std=gnu99");
        project.SetCompileFlagsForFile(targetGuid, fileGuid, flags);

        // Add CoreBluetooth for connecting to a Daydream controller.
        project.AddFrameworkToProject(targetGuid, "CoreBluetooth.framework", false);

        // Update Info.plist to add the NSBluetoothPeripheralUsageDescription property,
        // which is required for Bluetooth.
        string plistPath = path + "/Info.plist";
        PlistDocument plist = new PlistDocument();
        plist.ReadFromString(File.ReadAllText(plistPath));
        PlistElementDict rootDict = plist.root;
        rootDict.SetString("NSBluetoothPeripheralUsageDescription","Bluetooth is needed to connect a controller.");
        File.WriteAllText(plistPath, plist.WriteToString());
      #else
        foreach (var fileName in IOS_DAYDREAM_CONTROLLER_PLUGIN_FILE_NAMES) {
          var fileGuid = project.FindFileGuidByProjectPath(XCODE_PLUGIN_SOURCE_DIRECTORY + fileName);
          project.RemoveFileFromBuild(targetGuid, fileGuid);
        }
      #endif // GVR_IOS_DAYDREAM_CONTROLLER_ENABLED

      File.WriteAllText(projectPath, project.WriteToString());
    }
  }

  // 'Player Settings > Virtual Reality Supported' enabled?
  private bool IsVRSupportEnabled() {
    return PlayerSettings.virtualRealitySupported;
  }

  // 'Player Settings > Virtual Reality SDKs' includes any VR SDK other than 'None'?
  private bool IsSDKOtherThanNoneIncluded() {
    bool containsNone = XRSettings.supportedDevices.Contains(GvrSettings.VR_SDK_NONE);
    int numSdks = XRSettings.supportedDevices.Length;
    return containsNone ? numSdks > 1 : numSdks > 0;
  }
}
#endif  // UNITY_ANDROID || UNITY_IOS
