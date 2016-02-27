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

/// A custom editor for properties on the CardboardAudioRoom script. This appears in the Inspector
/// window of a CardboardAudioRoom object.
[CustomEditor(typeof(CardboardAudioRoom))]
[CanEditMultipleObjects]
public class CardboardAudioRoomEditor : Editor {
  private SerializedProperty leftWall = null;
  private SerializedProperty rightWall = null;
  private SerializedProperty floor = null;
  private SerializedProperty ceiling = null;
  private SerializedProperty backWall = null;
  private SerializedProperty frontWall = null;
  private SerializedProperty reflectivity = null;
  private SerializedProperty reverbGainDb = null;
  private SerializedProperty reverbBrightness = null;
  private SerializedProperty reverbTime = null;
  private SerializedProperty size = null;

  private GUIContent surfaceMaterialsLabel = new GUIContent("Surface Materials",
      "Room surface materials to calculate the acoustic properties of the room.");
  private GUIContent surfaceMaterialLabel = new GUIContent("Surface Material",
      "Surface material used to calculate the acoustic properties of the room.");
  private GUIContent reflectivityLabel = new GUIContent("Reflectivity",
      "Adjusts what proportion of the direct sound is reflected back by each surface, after an " +
      "appropriate delay. Reverberation is unaffected by this setting.");
  private GUIContent reverbGainLabel = new GUIContent("Gain (dB)",
      "Applies a gain adjustment to the reverberation in the room. The default value will leave " +
      "reverb unaffected.");
  private GUIContent reverbPropertiesLabel = new GUIContent("Reverb Properties",
      "Parameters to adjust the reverb properties of the room.");
  private GUIContent reverbBrightnessLabel = new GUIContent("Brightness",
      "Adjusts the balance between high and low frequencies in the reverb.");
  private GUIContent reverbTimeLabel = new GUIContent("Time",
      "Adjusts the overall duration of the reverb by a positive scaling factor.");
  private GUIContent sizeLabel = new GUIContent("Size", "Sets the room dimensions.");

  void OnEnable () {
    leftWall = serializedObject.FindProperty("leftWall");
    rightWall = serializedObject.FindProperty("rightWall");
    floor = serializedObject.FindProperty("floor");
    ceiling = serializedObject.FindProperty("ceiling");
    backWall = serializedObject.FindProperty("backWall");
    frontWall = serializedObject.FindProperty("frontWall");
    reflectivity = serializedObject.FindProperty("reflectivity");
    reverbGainDb = serializedObject.FindProperty("reverbGainDb");
    reverbBrightness = serializedObject.FindProperty("reverbBrightness");
    reverbTime = serializedObject.FindProperty("reverbTime");
    size = serializedObject.FindProperty("size");
  }

  /// @cond
  public override void OnInspectorGUI () {
    serializedObject.Update();

    EditorGUILayout.LabelField(surfaceMaterialsLabel);
    ++EditorGUI.indentLevel;
    DrawSurfaceMaterial(leftWall);
    DrawSurfaceMaterial(rightWall);
    DrawSurfaceMaterial(floor);
    DrawSurfaceMaterial(ceiling);
    DrawSurfaceMaterial(backWall);
    DrawSurfaceMaterial(frontWall);
    --EditorGUI.indentLevel;

    EditorGUILayout.Separator();

    EditorGUILayout.Slider(reflectivity, 0.0f, CardboardAudio.maxReflectivity, reflectivityLabel);

    EditorGUILayout.Separator();

    EditorGUILayout.LabelField(reverbPropertiesLabel);
    ++EditorGUI.indentLevel;
    EditorGUILayout.Slider(reverbGainDb, CardboardAudio.minGainDb, CardboardAudio.maxGainDb,
                           reverbGainLabel);
    EditorGUILayout.Slider(reverbBrightness, CardboardAudio.minReverbBrightness,
                           CardboardAudio.maxReverbBrightness, reverbBrightnessLabel);
    EditorGUILayout.Slider(reverbTime, 0.0f, CardboardAudio.maxReverbTime, reverbTimeLabel);
    --EditorGUI.indentLevel;

    EditorGUILayout.Separator();

    EditorGUILayout.PropertyField(size, sizeLabel);

    serializedObject.ApplyModifiedProperties();
  }
  /// @endcond

  private void DrawSurfaceMaterial (SerializedProperty surfaceMaterial) {
#if UNITY_4_5
    surfaceMaterialLabel.text = ObjectNames.NicifyVariableName(surfaceMaterial.name);
#else
    surfaceMaterialLabel.text = surfaceMaterial.displayName;
#endif
    EditorGUILayout.PropertyField(surfaceMaterial, surfaceMaterialLabel);
  }
}
