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

public class CardboardMenu {
  // Setup wizard
  //[MenuItem("Cardboard/Cardboard Setup...", false, 20)]
  private static void ModifyExistingCamera() {
    // Show helper dialog.
    CardboardSetup.ShowWindow();
  }

  [MenuItem("Cardboard/Documentation/Developers Site", false, 100)]
  private static void OpenDocumentation() {
    Application.OpenURL("https://developers.google.com/cardboard");
  }

  [MenuItem("Cardboard/Documentation/Unity Guide", false, 100)]
  private static void OpenUnityGuide() {
    Application.OpenURL("https://developers.google.com/cardboard/unity/guide");
  }

  [MenuItem("Cardboard/Documentation/Release Notes", false, 100)]
  private static void OpenReleaseNotes() {
    Application.OpenURL("https://developers.google.com/cardboard/unity/release-notes");
  }

  [MenuItem("Cardboard/Documentation/Known Issues", false, 100)]
  private static void OpenKnownIssues() {
    Application.OpenURL("https://developers.google.com/cardboard/unity/release-notes#known_issues");
  }

  [MenuItem("Cardboard/Report Bug", false, 100)]
  private static void OpenReportBug() {
    Application.OpenURL("https://github.com/googlesamples/cardboard-unity/issues");
  }

  [MenuItem("Cardboard/About Cardboard", false, 200)]
  private static void OpenAbout() {
    EditorUtility.DisplayDialog("Cardboard SDK for Unity",
        "Version: " + Cardboard.CARDBOARD_SDK_VERSION + "\n\n"
        + "License: Apache 2.0\n"
        + "Copyright: ©2015 Google Inc. All rights reserved.\n"
        + "See LICENSE for additional license information.",
        "OK");
  }
}