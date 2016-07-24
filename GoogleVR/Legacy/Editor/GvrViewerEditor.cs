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
using UnityEditor;
using UnityEditor.Callbacks;
#if UNITY_IOS
using UnityEditor.iOS.Xcode;
#endif

/// A custom editor for properties on the GvrViewer script.  This appears in the
/// Inspector window of a GvrViewer object.  Its purpose is to allow changing the
/// `GvrViewer.Instance` object's properties from their default values.
[CustomEditor(typeof(GvrViewer))]
public class GvrViewerEditor : Editor {
  GUIContent vrModeLabel = new GUIContent("VR Mode Enabled",
      "Sets whether VR mode is enabled.");

  GUIContent distortionCorrectionLabel = new GUIContent("Distortion Correction",
      "The distortion correction method performed by the SDK.");

  GUIContent stereoScreenScale = new GUIContent("Stereo Screen Scale",
      "The screen resolution is multiplied by this value when creating the " +
      "RenderTexture for the stereo screen.");

  GUIContent neckModelScaleLabel = new GUIContent("Neck Model Scale",
      "The scale factor of the builtin neck model [0..1].  To disable, set to 0.");

  GUIContent editorSettingsLabel = new GUIContent("Unity Editor Emulation Settings",
      "Controls for the in-editor emulation of a Cardboard viewer.");

  GUIContent autoUntiltHeadLabel = new GUIContent("Auto Untilt Head",
      "When enabled, just release Ctrl to untilt the head.");

  GUIContent UseUnityRemoteInputLabel = new GUIContent("Use unity remote as the input.",
      "When you can use Unity Remote Gyro");

  GUIContent screenSizeLabel = new GUIContent("Screen Size",
      "The screen size to emulate.");

  GUIContent viewerTypeLabel = new GUIContent("Viewer Type",
      "The viewer type to emulate.");

  /// @cond HIDDEN
  public override void OnInspectorGUI() {
    GUI.changed = false;

    GUIStyle headingStyle = new GUIStyle(GUI.skin.label);
    headingStyle.fontStyle = FontStyle.Bold;

    GvrViewer gvrViewer = (GvrViewer)target;

    EditorGUILayout.LabelField("General Settings", headingStyle);
    gvrViewer.VRModeEnabled =
        EditorGUILayout.Toggle(vrModeLabel, gvrViewer.VRModeEnabled);
    gvrViewer.DistortionCorrection = (GvrViewer.DistortionCorrectionMethod)
        EditorGUILayout.EnumPopup(distortionCorrectionLabel, gvrViewer.DistortionCorrection);
    float oldScale = gvrViewer.StereoScreenScale;
    float newScale = EditorGUILayout.Slider(stereoScreenScale, oldScale, 0.25f, 2.0f);
    if (!Mathf.Approximately(newScale, oldScale)) {
      gvrViewer.StereoScreenScale = newScale;
    }
    gvrViewer.NeckModelScale =
        EditorGUILayout.Slider(neckModelScaleLabel, gvrViewer.NeckModelScale, 0, 1);

    EditorGUILayout.Separator();

    EditorGUILayout.LabelField(editorSettingsLabel, headingStyle);
    gvrViewer.autoUntiltHead =
        EditorGUILayout.Toggle(autoUntiltHeadLabel, gvrViewer.autoUntiltHead);
    gvrViewer.UseUnityRemoteInput =
        EditorGUILayout.Toggle(UseUnityRemoteInputLabel, gvrViewer.UseUnityRemoteInput);
    gvrViewer.ScreenSize = (GvrProfile.ScreenSizes)
        EditorGUILayout.EnumPopup(screenSizeLabel, gvrViewer.ScreenSize);
    gvrViewer.ViewerType = (GvrProfile.ViewerTypes)
        EditorGUILayout.EnumPopup(viewerTypeLabel, gvrViewer.ViewerType);

    if (GUI.changed) {
      EditorUtility.SetDirty(gvrViewer);
    }
  }

#if UNITY_IOS
  // Add -ObjC to the Xcode project's linker flags, since our native iOS code
  // requires it.  Also add required frameworks.
  [PostProcessBuild(100)]
  public static void OnPostProcessBuild(BuildTarget platform, string projectPath) {
    if (platform != BuildTarget.iOS) {
      return;
    }
    string pbxFile = PBXProject.GetPBXProjectPath(projectPath);
    PBXProject pbxProject = new PBXProject();
    pbxProject.ReadFromFile(pbxFile);
    string target = pbxProject.TargetGuidByName(PBXProject.GetUnityTargetName());
    pbxProject.AddFrameworkToProject(target, "Security.framework", false);
    pbxProject.AddFrameworkToProject(target, "GLKit.framework", false);
    pbxProject.AddBuildProperty(target, "OTHER_LDFLAGS", "-ObjC");
    pbxProject.WriteToFile(pbxFile);
  }
#endif
}
