# Google VR SDK for Unity

Enables Daydream and Cardboard app development in Unity.

Copyright (c) 2016 Google Inc. All rights reserved.

For updates, known issues, and upgrade instructions, see:
[https://developers.google.com/vr/unity/release-notes](https://developers.google.com/vr/unity/release-notes)

For first time users, see the Get Started Guides for [Android Cardboard](https://developers.google.com/vr/unity/get-started-android), [Android Daydream](https://developers.google.com/vr/unity/get-started-controller), and [iOS Cardboard](https://developers.google.com/vr/unity/get-started-ios).

Please note, we do not accept pull requests.

## Migration to Unity 5.6
__The GVR Unity SDK will no longer support Unity versions older than 5.6 as of 1.50.__ Please upgrade to 5.6 via the following steps:
1. Update the GVR Unity SDK to 1.40.
2. Migrate to Unity 5.6.0f3 or above. The SDK will import or remove all the necessary libraries.
3. Update to GVR Unity SDK 1.50 or above.

## Repo Guide
* __GoogleVRForUnity.unitypackage__. The GoogleVR Unity SDK for importing into Unity GoogleVR projects.
* __GoogleVR__. The source code for convenient review and discussion.
* __Samples__. Reference Unity projects for Daydream and Cardboard.

## Pod update to the latest GVR iOS SDK
As of Unity 5.6.0f3, the generated Cocoapod can be updated to the latest GVR iOS SDK by following these steps.
* Build an XCode project from Unity.
* In a terminal, change directories into the XCode project folder.
* Run the following commands
  * ``pod deintegrate``
  * ``pod cache clean --all``
* Change the number in the Podfile from 1.20 to 1.60
* Open the project in XCode, and delete the Pods directory and Pods.xcodeproj if they exist.
* In the XCode project settings, change the deployment target to 8.0.
* Quit XCode
* In Terminal, do ``pod update``
  * "Installing GVRSDK (1.60.0)" should appear in the console.
* ``pod install``

