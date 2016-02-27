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
using System.Collections.Generic;
using UnityEngine.EventSystems;

public class CardboardSetup : EditorWindow {
  // Camera objects.
  private string[] cameraNames;
  private GameObject[] cameraObjects;

  // Is there a Cardboard camera already?
  private bool foundCardboardCamera = false;
  private bool addReticle = false;

  // Currently selected camera in the dialog.
  private int selectedCamera = -1;

  public static void ShowWindow () {
    // Get existing open window or if none, make a new one:
    CardboardSetup window = (CardboardSetup)EditorWindow.GetWindow(typeof(CardboardSetup));

    // Setup the window.
    window.FindCameras();
    window.AdjustWindowSize();
    window.Show();
  }

  /// Unity callback called when the hierarchy is change.
  void OnHierarchyChange() {
    // Update the cameras.
    FindCameras();
    AdjustWindowSize();
  }

  // Render the dialog.
  void OnGUI () {
    //
    // Info text.
    //
    GUILayout.BeginVertical("Box");
    GUILayout.Label("Preview your work with a stereoscopic camera and a " +
        "Cardboard viewer.", EditorStyles.wordWrappedLabel);
    GUILayout.Label("Would you like to add a new camera (ideal option), or " +
        "modify one of the existing cameras to add stereoscopic rendering " +
        "support?", EditorStyles.wordWrappedLabel);
    GUILayout.EndVertical();

    //
    // Camera choice.
    //
    GUILayout.Label ("Select Camera for Cardbard Support:",
        EditorStyles.boldLabel);
    GUILayout.BeginVertical("Box");

    // New camera.
    GUILayout.Label ("Add a new camera:");
    // Is there a Cardboard camera already?
    if (foundCardboardCamera) {
      // Warn the user.
      GUILayout.Label ("An existing Cardboard camera detected. Are you sure " +
          "you need another?", EditorStyles.wordWrappedMiniLabel);
    }
    HandleCameraToggleObject(0);
    GUILayout.Space(10);

    // Existing camera.
    GUILayout.Label ("Modify an existing camera:");
    for (int i = 1; i < cameraNames.Length; ++i) {
      HandleCameraToggleObject(i);
    }

    // No cameras in the scene.
    if (cameraNames.Length <= 1) {
      GUILayout.Label ("No available cameras found in the scene.",
          EditorStyles.boldLabel);
    }

    GUILayout.EndVertical();
    GUILayout.Space(20);

    //
    // Cardboard Settings.
    //
    GUILayout.Label ("Cardboard Settings", EditorStyles.boldLabel);
    GUILayout.BeginVertical("Box");

    // Reticle.
    addReticle = GUILayout.Toggle(addReticle, "Reticle support");

    GUILayout.EndVertical();
    GUILayout.Space(20);

    //
    // Confirmation buttons.
    //
    // Use up all available space.
    GUILayout.FlexibleSpace();
    GUILayout.BeginHorizontal();

    // Apply button.
    if (GUILayout.Button("Apply")) {
      if (selectedCamera >= 0 && selectedCamera < cameraObjects.Length) {
        // Apply user's choice.
        ApplyCamera(selectedCamera);
        AddCardboardManager();
      }
      Close();
    }

    // Cancel.
    if (GUILayout.Button("Cancel")) {
      Close();
    }
    GUILayout.EndHorizontal();
    GUILayout.Space(5);
  }

  // Find all the cameras and add them to the arrays.
  private void FindCameras() {
    // Lists to hold objects.
    List<string> cameraNamesList = new List<string>();
    List<GameObject> cameraObjectsList = new List<GameObject>();

    // Retrieve all cameras.
    Camera[] cameras = FindObjectsOfType(typeof(Camera)) as Camera[];

    // Retrieve applicable cameras.
    for (int i = 0; i < cameras.Length; ++i) {
      Camera camera = cameras[i];
      GameObject gameObject = camera.gameObject;

      // Skip cameras with Cardboard scripts on them.
      if (gameObject.GetComponent<CardboardEye>() == null
          && gameObject.GetComponent<CardboardPreRender>() == null
          && gameObject.GetComponent<CardboardPostRender>() == null) {
        // Valid camera!
        cameraNamesList.Add(gameObject.name);
        cameraObjectsList.Add(gameObject);

        // Is there a StereoController?
        if (gameObject.GetComponent<StereoController>() != null) {
          // Found a StereoController Cardboard camera script.
          // Should notify the user a Cardboard camera already exists.
          foundCardboardCamera = true;
        }
      } else {
        // Found some sort of Cardboard camera script.
        // Should notify the user something is out there.
        foundCardboardCamera = true;
      }
    }

    // Add new camera option.
    cameraNamesList.Insert(0, "Cardboard Camera");
    cameraObjectsList.Insert(0, null);

    // Convert lists to nice and lovely arrays.
    cameraNames = cameraNamesList.ToArray();
    cameraObjects = cameraObjectsList.ToArray();
  }

  private void ApplyCamera(int selection) {
    // Select the correct camera to add Cardboard support to.
    if (selection == 0) {
      // Index 0 means add a new camera.
      AddNewCamera();
    } else {
      // Everything else modifies an existing camera.
      ModifyCamera(cameraObjects[selection]);
    }
  }

  private void ModifyCamera(GameObject cameraObject) {
    // Select the camera object in the editor.
    Selection.activeObject = cameraObject;

    // Register object for undo.
    //Undo.RegisterCompleteObjectUndo(cameraObject, "Added Cardboard support");

    // Attempt finding existing controller.
    StereoController stereoController =
        cameraObject.GetComponent<StereoController>();

    // Otherwise,
    if (stereoController == null) {
      // Add a stereo controller.
      stereoController= cameraObject.AddComponent<StereoController>();
    }

    // Add the stereo setup.
    stereoController.AddStereoRig();

    // Add reticle support.
    AddReticleSupport(cameraObject);
  }

  private void AddNewCamera() {
    // Create a new camera.
    GameObject instance = new GameObject("Cardboard Camera");
    instance.AddComponent<Camera>();

    // Add Cardboard support
    ModifyCamera(instance);
  }

  private void AddReticleSupport(GameObject gameObject) {
    // User wants a reticle?
    if (addReticle) {
      // Retrieve the reticle asset.
      Object asset = UnityEditor.AssetDatabase.LoadAssetAtPath(
          "Assets/Cardboard/Prefabs/UI/CardboardReticle.prefab",
          typeof(GameObject));
      if (asset == null) {
        // Show there was an error.
        EditorUtility.DisplayDialog("Failed to create Reticle",
            "CardboardReticle.prefab not found. Did you move the Cardboard " +
            "assets directory? Expected location: \"Assets/Cardboard/Prefabs" +
            "/UI/CardboardReticle.prefab\"", "Maybe?..");
        return;
      }

      // Find head. Reticle should be child of head.
      CardboardHead head = gameObject.GetComponentInChildren<CardboardHead>();

      if (head) {
        // Found head! Refer its GameObject.
        GameObject headObject = head.gameObject;

        // Instantiate reticle.
        GameObject instance = Object.Instantiate(asset) as GameObject;
        instance.name = asset.name; // removes the (clone) from the name.
        // Set as child of CardboardHead object.
        instance.transform.SetParent(headObject.transform, false);
      }

      // Add modules to support uGUI.
      AddGazeInputModule();
      AddPhysicsRaycasterToCamera(gameObject);
    }
  }

  private void HandleCameraToggleObject(int index) {
    if (index < 0 || index >= cameraNames.Length) {
      return;
    }

    // Show the camera toggle.
    bool selected = GUILayout.Toggle((selectedCamera == index),
        cameraNames[index], EditorStyles.radioButton);

    // Handle selection.
    if (selected) {
      selectedCamera = index;
    }
  }

  // Adds the physics raycaster component
  // to the correct object in the hierarchy.
  private void AddPhysicsRaycasterToCamera(GameObject cardboardHead) {
    // Find existing raycaster.
    PhysicsRaycaster raycaster =
        cardboardHead.GetComponent<PhysicsRaycaster>();

    // Otherwise,
     if (raycaster == null) {
       // Add the raycaster.
       raycaster = cardboardHead.AddComponent<PhysicsRaycaster>();
     }

    // Remove Ignore Raycast layer from the event mask.
    raycaster.eventMask &= ~(1 <<LayerMask.NameToLayer("Ignore Raycast"));
  }

  // Add cardboard manager and pre/post rendering.
  private void AddCardboardManager() {
    GameObject managerObject = null;
    // Is there a pre-render / post-render object?
    Object[] preRenders = FindObjectsOfType(typeof(CardboardPreRender));
    Object[] postRenders = FindObjectsOfType(typeof(CardboardPostRender));
    if ((preRenders == null || preRenders.Length == 0) && (postRenders == null
        || postRenders.Length == 0)) {

      // Retrieve the asset.
      Object asset = UnityEditor.AssetDatabase.LoadAssetAtPath(
          "Assets/Cardboard/Prefabs/CardboardCamera.prefab", typeof(GameObject));
      if (asset == null) {
        // Show there was an error.
        EditorUtility.DisplayDialog("Failed to create CardboardCamera",
            "CardboardCamera.prefab not found. Did you move the Cardboard " +
            "assets directory? Expected location: \"Assets/Cardboard/Prefabs" +
            "/CardboardCamera.prefab\"", "Maybe?..");
        return;
      }
      // Instantiate the object.
      managerObject = Object.Instantiate(asset) as GameObject;
      managerObject.name = "Cardboard Manager";
    }

    // Is there a Cardboard script already?
    // Must only be one Cardboard script.
    Cardboard[] cardboardInstances = FindObjectsOfType(typeof(Cardboard))
        as Cardboard[];
    if (cardboardInstances == null || cardboardInstances.Length == 0) {
      // If we made a pre/post renderer,
      // then add the script to the same object.
      if (managerObject == null) {
        // otherwise, create a new object.
        managerObject = new GameObject();
        managerObject.name = "Cardboard Manager";
      }

      // Add the cardboard script.
      managerObject.AddComponent<Cardboard>();
    }
  }

  // Add Gaze Input Module to EventSystem.
  // Handle existing EventSystems and new EventSystems.
  private void AddGazeInputModule() {
    // The object we are adding the Input Module to.
    GameObject eventSystemObject = null;

    // Find existing event systems.
    Object[] eventSystems = FindObjectsOfType(typeof(EventSystem));
    if (eventSystems == null || eventSystems.Length == 0) {
      // If none, create a new one.
      eventSystemObject = new GameObject("EventSystem");
      eventSystemObject.AddComponent<EventSystem>();
    } else {
      // If there is one, use the first one.
      eventSystemObject = ((EventSystem)eventSystems[0]).gameObject;
    }

    // Get the list of components for fixing the module's priority.
    Component[] components = eventSystemObject.GetComponents<Component>();

    // Add the GazeInputModule.
    GazeInputModule gazeInputModule =
        eventSystemObject.AddComponent<GazeInputModule>();

    // Make sure the GazeInputModule is at the top of the components list.
    for (int i = 0; i < components.Length; ++i) {
      UnityEditorInternal.ComponentUtility.MoveComponentUp(gazeInputModule);
    }
  }

  private void AdjustWindowSize() {
    const float kMinWidth = 403.0f;
    const float kMinHeight = 290.0f;

    // Modify the height to accomodate additional cameras.
    int heightModifer = 0;
    if (cameraNames != null) {
      heightModifer += 12 * cameraNames.Length;
    }

    // Restrict minimum window size.
    minSize = new Vector2(kMinWidth, kMinHeight + heightModifer);
  }
}