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
using UnityEditor;
using System.Collections;

/// A custom editor for properties on the CardboardAudioListener script. This appears in the
/// Inspector window of a CardboardAudioListener object.
[CustomEditor(typeof(CardboardAudioListener))]
public class CardboardAudioListenerEditor : Editor {
  private SerializedProperty globalGainDb = null;
  private SerializedProperty quality = null;
  private SerializedProperty worldScale = null;

  private GUIContent globalGainLabel = new GUIContent("Global Gain (dB)",
     "Sets the global gain of the system. Can be used to adjust the overall output volume.");
  private GUIContent qualityLabel = new GUIContent("Quality",
     "Sets the quality mode in which the spatial audio will be rendered. " +
     "Higher quality modes allow for increased fidelity at the cost of greater CPU usage.");
  private GUIContent worldScaleLabel = new GUIContent("World Scale",
     "Sets the ratio between game units and real world units (meters).");

  void OnEnable () {
    globalGainDb = serializedObject.FindProperty("globalGainDb");
    quality = serializedObject.FindProperty("quality");
    worldScale = serializedObject.FindProperty("worldScale");
  }

  /// @cond
  public override void OnInspectorGUI () {
    serializedObject.Update();

    // Rendering quality can only be modified through the Inspector in Edit mode.
    GUI.enabled = !EditorApplication.isPlaying;
    EditorGUILayout.PropertyField(quality, qualityLabel);
    GUI.enabled = true;

    EditorGUILayout.Separator();

    EditorGUILayout.Slider(globalGainDb, CardboardAudio.minGainDb, CardboardAudio.maxGainDb,
                           globalGainLabel);
    EditorGUILayout.Slider(worldScale, CardboardAudio.minWorldScale, CardboardAudio.maxWorldScale,
                           worldScaleLabel);

    serializedObject.ApplyModifiedProperties();
  }
  /// @endcond
}
