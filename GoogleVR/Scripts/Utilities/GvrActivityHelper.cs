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

using UnityEngine;

// Simple static class to abstract out several jni calls that need to be shared
// between different classes.
public static class GvrActivityHelper {

#if UNITY_IOS
  public const string GVR_DLL_NAME = "__Internal";
#else
  public const string GVR_DLL_NAME = "gvr";
#endif  // UNITY_IOS

  public const string PACKAGE_UNITY_PLAYER = "com.unity3d.player.UnityPlayer";

#if UNITY_ANDROID && !UNITY_EDITOR
  /// Returns the Android Activity used by the Unity device player. The caller is
  /// responsible for memory-managing the returned AndroidJavaObject.
  public static AndroidJavaObject GetActivity() {
    AndroidJavaClass jc = new AndroidJavaClass(PACKAGE_UNITY_PLAYER);
    if (jc == null) {
      Debug.LogErrorFormat("Failed to get class {0}", PACKAGE_UNITY_PLAYER);
      return null;
    }
    AndroidJavaObject activity = jc.GetStatic<AndroidJavaObject>("currentActivity");
    if (activity == null) {
      Debug.LogError("Failed to obtain current Android activity.");
      return null;
    }
    return activity;
  }

  /// Returns the application context of the current Android Activity.
  public static AndroidJavaObject GetApplicationContext(AndroidJavaObject activity) {
    AndroidJavaObject context = activity.Call<AndroidJavaObject>("getApplicationContext");
    if (context == null) {
      Debug.LogError("Failed to get application context from Activity.");
      return null;
    }
    return context;
  }
#endif  // UNITY_ANDROID && !UNITY_EDITOR
}
