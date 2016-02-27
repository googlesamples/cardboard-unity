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

/// A custom editor for properties on the CardboardAudioSource script. This appears in the Inspector
/// window of a CardboardAudioSource object.
[CustomEditor(typeof(CardboardAudioSource))]
[CanEditMultipleObjects]
public class CardboardAudioSourceEditor : Editor {
  private SerializedProperty clip = null;
  private SerializedProperty loop = null;
  private SerializedProperty mute = null;
  private SerializedProperty pitch = null;
  private SerializedProperty playOnAwake = null;
  private SerializedProperty volume = null;
  private SerializedProperty rolloffMode = null;
  private SerializedProperty maxDistance = null;
  private SerializedProperty minDistance = null;
  private SerializedProperty directivityAlpha = null;
  private SerializedProperty directivitySharpness = null;
  private Texture2D directivityTexture = null;
  private SerializedProperty gainDb = null;
  private SerializedProperty hrtfEnabled = null;
  private SerializedProperty occlusionEnabled = null;

  private GUIContent clipLabel = new GUIContent("AudioClip",
      "The AudioClip asset played by the CardboardAudioSource.");
  private GUIContent loopLabel = new GUIContent("Loop",
      "Sets the source to loop.");
  private GUIContent muteLabel = new GUIContent("Mute",
      "Mutes the sound.");
  private GUIContent pitchLabel = new GUIContent("Pitch",
      "Sets the frequency of the sound. Use this to slow down or speed up the sound.");
  private GUIContent volumeLabel = new GUIContent("Volume",
      "Sets the overall volume of the sound.");
  private GUIContent rolloffModeLabel = new GUIContent("Volume Rolloff",
      "Which type of rolloff curve to use.");
  private GUIContent maxDistanceLabel = new GUIContent("Max Distance",
      "Max distance is the distance a sound stops attenuating at.");
  private GUIContent minDistanceLabel = new GUIContent("Min Distance",
      "Within the min distance, the volume will stay at the loudest possible. " +
      "Outside this min distance it will begin to attenuate.");
  private GUIContent playOnAwakeLabel = new GUIContent("Play On Awake",
      "Play the sound when the scene loads.");
  private GUIContent directivityLabel = new GUIContent("Directivity",
      "Controls the pattern of sound emission of the source. This can change the perceived " +
      "loudness of the source depending on which way it is facing relative to the listener. " +
      "Patterns are aligned to the 'forward' direction of the parent object.");
  private GUIContent directivityAlphaLabel = new GUIContent("Alpha",
      "Controls the balance between dipole pattern and omnidirectional pattern for source " +
      "emission. By varying this value, differing directivity patterns can be formed.");
  private GUIContent directivitySharpnessLabel = new GUIContent("Sharpness",
      "Sets the sharpness of the directivity pattern. Higher values will result in increased " +
      "directivity.");
  private GUIContent gainLabel = new GUIContent("Gain (dB)",
      "Applies a gain to the source for adjustment of relative loudness.");
  private GUIContent hrtfEnabledLabel = new GUIContent("Enable HRTF",
      "Sets HRTF binaural rendering for the source. Note that this setting has no effect when " +
      "global low quality mode is selected.");
  private GUIContent occlusionLabel = new GUIContent("Enable Occlusion",
      "Sets whether the sound of the source should be occluded when passing through objects " +
      "between the source and the listener.");

  void OnEnable () {
    clip = serializedObject.FindProperty("sourceClip");
    loop = serializedObject.FindProperty("sourceLoop");
    mute = serializedObject.FindProperty("sourceMute");
    pitch = serializedObject.FindProperty("sourcePitch");
    playOnAwake = serializedObject.FindProperty("playOnAwake");
    volume = serializedObject.FindProperty("sourceVolume");
    rolloffMode = serializedObject.FindProperty("rolloffMode");
    maxDistance = serializedObject.FindProperty("sourceMaxDistance");
    minDistance = serializedObject.FindProperty("sourceMinDistance");
    directivityAlpha = serializedObject.FindProperty("directivityAlpha");
    directivitySharpness = serializedObject.FindProperty("directivitySharpness");
#if UNITY_4_5
    directivityTexture = Texture2D.CreateExternalTexture(0, 0, TextureFormat.ARGB32, false, false,
                                                         System.IntPtr.Zero);
#else
    directivityTexture = Texture2D.blackTexture;
#endif
    gainDb = serializedObject.FindProperty("gainDb");
    hrtfEnabled = serializedObject.FindProperty("hrtfEnabled");
    occlusionEnabled = serializedObject.FindProperty("occlusionEnabled");
  }

  /// @cond
  public override void OnInspectorGUI () {
    serializedObject.Update();

    EditorGUILayout.PropertyField(clip, clipLabel);

    EditorGUILayout.Separator();

    EditorGUILayout.PropertyField(mute, muteLabel);
    EditorGUILayout.PropertyField(playOnAwake, playOnAwakeLabel);
    EditorGUILayout.PropertyField(loop, loopLabel);

    EditorGUILayout.Separator();

    EditorGUILayout.PropertyField(volume, volumeLabel);

    EditorGUILayout.Separator();

    EditorGUILayout.PropertyField(pitch, pitchLabel);

    EditorGUILayout.Separator();

    EditorGUILayout.Slider(gainDb, CardboardAudio.minGainDb, CardboardAudio.maxGainDb, gainLabel);

    EditorGUILayout.Separator();

    EditorGUILayout.PropertyField(rolloffMode, rolloffModeLabel);
    ++EditorGUI.indentLevel;
    EditorGUILayout.PropertyField(minDistance, minDistanceLabel);
    EditorGUILayout.PropertyField(maxDistance, maxDistanceLabel);
    --EditorGUI.indentLevel;
    if (rolloffMode.enumValueIndex == (int)AudioRolloffMode.Custom) {
      EditorGUILayout.HelpBox("Custom rolloff mode is not supported.", MessageType.Error);
    }

    EditorGUILayout.Separator();

    EditorGUILayout.BeginHorizontal();
    EditorGUILayout.BeginVertical();
    GUILayout.Label(directivityLabel);
    ++EditorGUI.indentLevel;
    EditorGUILayout.Slider(directivityAlpha, 0.0f, 1.0f, directivityAlphaLabel);
    EditorGUILayout.Slider(directivitySharpness, 1.0f, 10.0f, directivitySharpnessLabel);
    --EditorGUI.indentLevel;
    EditorGUILayout.EndVertical();
    DrawDirectivityPattern((int)(3.0f * EditorGUIUtility.singleLineHeight));
    EditorGUILayout.EndHorizontal();
    EditorGUILayout.PropertyField(occlusionEnabled, occlusionLabel);

    EditorGUILayout.Separator();

    // HRTF toggle can only be modified through the Inspector in Edit mode.
    GUI.enabled = !EditorApplication.isPlaying;
    EditorGUILayout.PropertyField(hrtfEnabled, hrtfEnabledLabel);
    GUI.enabled = true;

    serializedObject.ApplyModifiedProperties();
  }
  /// @endcond

  private void DrawDirectivityPattern (int size) {
    directivityTexture.Resize(size, size);
    // Draw the axes.
    Color axisColor = 0.5f * Color.black;
    for (int i = 0; i < size; ++i) {
      directivityTexture.SetPixel(i, size / 2, axisColor);
      directivityTexture.SetPixel(size / 2, i, axisColor);
    }
    // Draw the 2D polar directivity pattern.
    Color cardioidColor = 0.75f * Color.blue;
    float offset = 0.5f * size;
    float cardioidSize = 0.45f * size;
    Vector2[] vertices = CardboardAudio.Generate2dPolarPattern(directivityAlpha.floatValue,
                                                               directivitySharpness.floatValue,
                                                               180);
    for (int i = 0; i < vertices.Length; ++i) {
      directivityTexture.SetPixel((int)(offset + cardioidSize * vertices[i].x),
                                  (int)(offset + cardioidSize * vertices[i].y),
                                  cardioidColor);
    }
    directivityTexture.Apply();
    // Show the texture.
    GUILayout.Box(directivityTexture);
  }

}
