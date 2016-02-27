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
using System.Collections;
using System.Linq;

/// @ingroup EditorScripts
/// A custom editor for the StereoController script.  It exists to add the _Update
/// Stereo Cameras_ button to the StereoController's Inspector window, and to the
/// corresponding main menu entry and Camera context menu command.  The usage of the
/// these actions is described in StereoController.
[CustomEditor(typeof(StereoController))]
public class StereoControllerEditor : Editor {
  /// Name of button, and part of "Undo ..." message.
  public const string ACTION_NAME = "Update Stereo Cameras";

  private GUIContent updateButton =
    new GUIContent(ACTION_NAME, "Copy all Camera settings to the stereo cameras.");

  /// @cond HIDDEN
  public override void OnInspectorGUI() {
    DrawDefaultInspector();
    GUILayout.BeginHorizontal(GUILayout.ExpandHeight(false));
    GUILayout.FlexibleSpace();
    if (GUILayout.Button(updateButton, GUILayout.ExpandWidth(false))) {
      var controller = (StereoController)target;
      DoUpdateStereoCameras(controller.gameObject);
    }
    GUILayout.FlexibleSpace();
    GUILayout.EndHorizontal();
  }

  [MenuItem("Component/Cardboard/Update Stereo Cameras", true, 40)]
  public static bool CanUpdateStereoCameras() {
    // Make sure all selected items have valid cameras.
    return Selection.gameObjects.Where(go => CanUpdateStereoCameras(go)).Count()
        == Selection.gameObjects.Length;
  }

  [MenuItem("CONTEXT/Camera/Update Stereo Cameras", true, 41)]
  public static bool CanUpdateStereoCamerasContext(MenuCommand command) {
    var camera = (Camera)command.context;
    return CanUpdateStereoCameras(camera.gameObject);
  }

  [MenuItem("Component/Cardboard/Update Stereo Cameras", false, 42)]
  public static void UpdateStereoCameras() {
    foreach (var go in Selection.gameObjects) {
      DoUpdateStereoCameras(go);
    }
  }

  [MenuItem("CONTEXT/Camera/Update Stereo Cameras", false, 43)]
  public static void UpdateStereoCamerasContext(MenuCommand command) {
    var camera = (Camera)command.context;
    DoUpdateStereoCameras(camera.gameObject);
  }
  /// @endcond

  private static bool CanUpdateStereoCameras(GameObject go) {
    return go != null &&
           go.hideFlags == HideFlags.None &&
           go.GetComponent<Camera>() != null &&
           go.GetComponent<CardboardEye>() == null;
  }

  private static void DoUpdateStereoCameras(GameObject go) {
    // Make sure there is a StereoController.
    var controller = go.GetComponent<StereoController>();
    if (controller == null) {
      controller = go.AddComponent<StereoController>();
      Undo.RegisterCreatedObjectUndo(controller, ACTION_NAME);
    }

    // Remember current state of stereo rig.
#if !UNITY_5
    bool hadSkybox = go.GetComponent<SkyboxMesh>() != null;
#endif
    bool hadHead = controller.Head != null;
    bool hadEyes = controller.Eyes.Length > 0;

    controller.AddStereoRig();

    // Support undo...

#if !UNITY_5
    // Skybox mesh.  Deletes it if camera is not Main.
    var skybox = go.GetComponent<SkyboxMesh>();
    if (skybox != null) {
      if (!hadSkybox) {
        Undo.RegisterCreatedObjectUndo(skybox, ACTION_NAME);
      } else if (go.GetComponent<Camera>().tag != "MainCamera") {
        Undo.DestroyObjectImmediate(skybox);
      }
    }
#endif

    // Head.
    var head = go.GetComponent<CardboardHead>();
    if (head != null && !hadHead) {
      Undo.RegisterCreatedObjectUndo(head, ACTION_NAME);
    }

    // Eyes. Synchronizes them with controller's camera too.
    foreach (var eye in controller.Eyes) {
      if (!hadEyes) {
        Undo.RegisterCreatedObjectUndo(eye.gameObject, ACTION_NAME);
      } else {
        Undo.RecordObject(eye.GetComponent<Camera>(), ACTION_NAME);
        eye.CopyCameraAndMakeSideBySide(controller);
      }
    }
  }
}
