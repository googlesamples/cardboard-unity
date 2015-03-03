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

using System.Collections;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Cardboard))]
public class CardboardEditor : Editor {
  GUIContent tapIsTriggerLabel = new GUIContent("Tap Is Trigger",
    "Allow screen taps and mouse clicks to emulate the magnet trigger.");

  GUIContent alignmentMarkerLabel = new GUIContent("Alignment Marker",
    "Whether to draw the alignment marker. The marker is a vertical line that splits " +
    "the viewport in half, designed to help users align the screen with the Cardboard.");

  GUIContent settingsButtonLabel = new GUIContent("Settings Button",
    "Whether to draw the settings button. The settings button opens the " +
    "Google Cardboard app to allow the user to  configure their individual " +
    "settings and Cardboard headset parameters.");

  GUIContent vrModeEnabledLabel = new GUIContent("VR Mode Enabled",
    "Explicitly set whether VR mode is enabled.  Clears the Auto Enable VR setting.");

  GUIContent backButtonExitsAppLabel = new GUIContent("Back Button Exits App",
    "Whether tapping the back button exits the app.");

  public override void OnInspectorGUI() {
    GUI.changed = false;

    DrawDefaultInspector();

    Cardboard cardboard = (Cardboard)target;

    bool newTapIsTrigger = EditorGUILayout.Toggle(tapIsTriggerLabel, cardboard.TapIsTrigger);
    if (newTapIsTrigger != cardboard.TapIsTrigger) {
        cardboard.TapIsTrigger = newTapIsTrigger;
    }

    bool newEnableAlignmentMarkder =
      EditorGUILayout.Toggle(alignmentMarkerLabel, cardboard.EnableAlignmentMarker);
    if (newEnableAlignmentMarkder != cardboard.EnableAlignmentMarker) {
      cardboard.EnableAlignmentMarker = newEnableAlignmentMarkder;
    }

    bool newEnableSettingsButton =
      EditorGUILayout.Toggle(settingsButtonLabel, cardboard.EnableSettingsButton);
    if (newEnableSettingsButton != cardboard.EnableSettingsButton) {
      cardboard.EnableSettingsButton = newEnableSettingsButton;
    }

    bool newVRModeEnabled = EditorGUILayout.Toggle(vrModeEnabledLabel, cardboard.VRModeEnabled);
    if (newVRModeEnabled != cardboard.VRModeEnabled) {
      cardboard.VRModeEnabled = newVRModeEnabled;
    }

    cardboard.BackButtonExitsApp =
      EditorGUILayout.Toggle(backButtonExitsAppLabel, cardboard.BackButtonExitsApp);

    if (GUI.changed) {
      EditorUtility.SetDirty(cardboard);
    }

    if (EditorApplication.isPlaying) {
      bool newInCardboard = EditorGUILayout.Toggle("Is In Cardboard", cardboard.InCardboard);
      if (newInCardboard != cardboard.InCardboard) {
        cardboard.SetInCardboard(newInCardboard); // Takes effect at end of frame.
      }
    }
  }
}
