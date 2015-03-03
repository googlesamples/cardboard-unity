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

#if UNITY_ANDROID && !UNITY_EDITOR
#define ANDROID_DEVICE
#endif

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

// A Cardboard object serves as the bridge between the C# and the Java/native
// components of the plugin.
//
// On each frame, it's responsible for
//  - Reading the current head orientation and eye transforms from the Java
//    vrtoolkit
//  - Triggering the native (C++) UnityRenderEvent callback at the end of the
//    frame.  This sends the texture containing the latest frame that Unity
//    rendered into the Java vrtoolkit to be corrected for lens distortion and
//    displayed on the device.
public class Cardboard : MonoBehaviour {
  // Distinguish the two stereo eyes.
  public enum Eye {
    Left,
    Right
  }

#if UNITY_IPHONE && !UNITY_EDITOR
  [DllImport("__Internal")]
#else
  [DllImport("RenderingPlugin")]
#endif
  private static extern void InitFromUnity(int textureID);

  // The singleton instance of the Cardboard class.
  private static Cardboard sdk = null;

  public static Cardboard SDK {
    get {
      if (sdk == null) {
        Debug.Log("Creating Cardboard object");
        var go = new GameObject("Cardboard");
        sdk = go.AddComponent<Cardboard>();
        go.transform.localPosition = Vector3.zero;
      }
      return sdk;
    }
  }

  [Tooltip("Whether distortion correction is performed by the native plugin code.")]
  public bool nativeDistortionCorrection = true;

  // Whether VR-mode is enabled.
  public bool VRModeEnabled {
    get { return vrModeEnabled; }
    set {
      vrModeEnabled = value;
    }
  }

  [SerializeField]
  [HideInInspector]
  private bool vrModeEnabled = true;

  // Whether to draw the alignment marker. The marker is a vertical line that
  // splits the viewport in half, designed to help users align the screen with the Cardboard.
  public bool EnableAlignmentMarker {
      get { return enableAlignmentMarker; }
      set {
          enableAlignmentMarker = value;
#if ANDROID_DEVICE
          CallActivityMethod("setAlignmentMarkerEnabled", enableAlignmentMarker);
#endif
      }
  }

  [SerializeField]
  [HideInInspector]
  private bool enableAlignmentMarker = true;

  // Whether to draw the settings button. The settings button opens the Google
  // Cardboard app to allow the user to  configure their individual settings and Cardboard
  // headset parameters
  public bool EnableSettingsButton {
      get { return enableSettingsButton; }
      set {
          enableSettingsButton = value;
#if ANDROID_DEVICE
          CallActivityMethod("setSettingsButtonEnabled", enableSettingsButton);
#endif
      }
  }

  [SerializeField]
  [HideInInspector]
  private bool enableSettingsButton = true;

  // Whether screen taps are converted to Cardboard trigger events.
  public bool TapIsTrigger {
    get { return tapIsTrigger; }
    set {
      tapIsTrigger = value;
#if ANDROID_DEVICE
      CallActivityMethod("setConvertTapIntoTrigger", tapIsTrigger);
#endif
    }
  }

  [SerializeField]
  [HideInInspector]
  private bool tapIsTrigger = false;

  // Whether the back button exits the application.
  public bool BackButtonExitsApp {
    get { return backButtonExitsApp; }
    set {
      backButtonExitsApp = value;
    }
  }

  [SerializeField]
  [HideInInspector]
  private bool backButtonExitsApp = true;

  // Whether the device is in a Cardboard.
  public bool InCardboard { get; private set; }

  // Defer updating InCardboard till end of frame.
  private bool newInCardboard;

#if UNITY_EDITOR
  // Helper for the custom editor script.
  public void SetInCardboard(bool value) {
      newInCardboard = value; // Takes effect at end of frame.
  }
#endif

  // Whether the Cardboard trigger (i.e. magnet) was pulled.
  // True for exactly one frame (between 2 EndOfFrames) on each pull.
  public bool CardboardTriggered { get; private set; }

  // Next frame's value of CardboardTriggered.
  private bool newCardboardTriggered = false;

  // The texture that Unity renders the scene to. This is sent to the plugin,
  // which renders it to screen, correcting for lens distortion.
  public RenderTexture StereoScreen {
    get {
      // Don't need it except for distortion correction.
      if (!nativeDistortionCorrection || !vrModeEnabled) {
        return null;
      }
      if (stereoScreen == null) {
        CreateStereoScreen();
      }
      return stereoScreen;
    }
  }

  private RenderTexture stereoScreen;

  // Describes the current device, including phone screen.
  public CardboardProfile Profile { get; private set; }

  // Transform from body to head.
  public Matrix4x4 HeadView { get { return headView; } }
  private Matrix4x4 headView;

  // Transform from body to head as a Quaternion.
  public Quaternion HeadRotation { get; private set; }

  // The transformation from head to eye.
  public Matrix4x4 EyeView(Cardboard.Eye eye) {
      return eye == Cardboard.Eye.Left ? leftEyeView : rightEyeView;
  }
  private Matrix4x4 leftEyeView;
  private Matrix4x4 rightEyeView;

  // The projection matrix for a given eye.  This encodes the field of view,
  // IPD, and other parameters configured by the SDK.
  public Matrix4x4 Projection(Cardboard.Eye eye) {
      return eye == Cardboard.Eye.Left ? leftEyeProj : rightEyeProj;
  }
  private Matrix4x4 leftEyeProj;
  private Matrix4x4 rightEyeProj;

  // The undistorted projection matrix for a given eye.  This encodes the field of
  // view, IPD, and other parameters configured by the SDK, but ignores the distortion
  // caused by the lenses.
  public Matrix4x4 UndistortedProjection(Cardboard.Eye eye) {
      return eye == Cardboard.Eye.Left ? leftEyeUndistortedProj : rightEyeUndistortedProj;
  }
  private Matrix4x4 leftEyeUndistortedProj;
  private Matrix4x4 rightEyeUndistortedProj;

  // Local transformations of eyes relative to head.
  public Vector3 EyeOffset(Cardboard.Eye eye) {
      return eye == Cardboard.Eye.Left ? leftEyeOffset : rightEyeOffset;
  }
  private Vector3 leftEyeOffset;
  private Vector3 rightEyeOffset;

  // The screen-space rectangle each eye should render into.
  public Rect EyeRect(Cardboard.Eye eye) {
      return eye == Cardboard.Eye.Left ? leftEyeRect : rightEyeRect;
  }
  private Rect leftEyeRect;
  private Rect rightEyeRect;

  // Minimum distance from the user that an object may be viewed in stereo
  // without eye strain, in meters.  The stereo eye separation should
  // be scaled down if the "center of interest" is closer than this.  This
  // will set a lower limit on the disparity of the COI between the two eyes.
  // See CardboardEye.OnPreCull().
  public float MinimumComfortDistance {
      get {
          return 1.0f;
      }
  }

  // Maximum distance from the user that an object may be viewed in
  // stereo without eye strain, in meters.  The stereo eye separation
  // should be scaled up of if the COI is farther than this.  This will
  // set an upper limit on the disparity of the COI between the two eyes.
  // See CardboardEye.OnPreCull().
  // Note: For HMDs with optics that focus at infinity there really isn't a
  // maximum distance, so this number can be set to "really really big".
  public float MaximumComfortDistance {
      get {
          return 100000f;  // i.e. really really big.
      }
  }

  // Only call native layer once per frame.
  private bool updated = false;

  // Configures which Cardboard features are enabled, depending on
  // the Unity features available.
  private class Config {
      // Features.
      public bool supportsRenderTextures;
      public bool isAndroid;
      public bool supportsAndroidRenderEvent;
      public bool isAtLeastUnity4_5;

      // Access to plugin.
      public bool canAccessActivity = false;

      // Should be called on main thread.
      public void initialize() {
          supportsRenderTextures = SystemInfo.supportsRenderTextures;
          isAndroid = Application.platform == RuntimePlatform.Android;
          try {
              Regex r = new Regex(@"(\d+\.\d+)\..*");
              string version = r.Replace(Application.unityVersion, "$1");
              if (new Version(version) >= new Version("4.5")) {
                  isAtLeastUnity4_5 = true;
              }
          } catch {
              Debug.LogWarning("Unable to determine Unity version from: "
                      + Application.unityVersion);
          }
          supportsAndroidRenderEvent = isAtLeastUnity4_5 && isAndroid;
      }

      public bool canApplyDistortionCorrection() {
          return supportsRenderTextures && supportsAndroidRenderEvent
              && canAccessActivity;
      }

      public string getDistortionCorrectionDiagnostic() {
          List<string> causes = new List<string>();
          if (!isAndroid) {
              causes.Add("Must be running on Android device");
          } else if (!canAccessActivity) {
              causes.Add("Cannot access UnityCardboardActivity. "
                      + "Verify that the jar is in Assets/Plugins/Android");
          }
          if (!supportsRenderTextures) {
              causes.Add("RenderTexture (Unity Pro feature) is unavailable");
          }
          if (!isAtLeastUnity4_5) {
              causes.Add("Unity 4.5+ is needed for Android UnityPluginEvent");
          }
          return String.Join("; ", causes.ToArray());
      }
  }

  private Config config = new Config();

  void Awake() {
      if (sdk == null) {
          sdk = this;
      }
      if (sdk != this) {
          Debug.LogWarning("Cardboard SDK object should be a singleton.");
          enabled = false;
          return;
      }

      config.initialize();

#if ANDROID_DEVICE
      ConnectToActivity();
#endif
      CreateStereoScreen();
      UpdateScreenData();

      // Force side-effectful initialization using serialized values.
      EnableAlignmentMarker = enableAlignmentMarker;
      EnableSettingsButton = enableSettingsButton;
      TapIsTrigger = tapIsTrigger;

      InCardboard = newInCardboard = false;
#if UNITY_EDITOR
      if (VRModeEnabled && Application.isPlaying) {
          SetInCardboard(true);
      }
#endif

      StartCoroutine("EndOfFrame");
  }

  void Update() {
      if (Input.GetKeyDown(KeyCode.Escape) && BackButtonExitsApp) {
          Application.Quit();
      }
#if UNITY_EDITOR
      SimulateTrigger();
#endif
  }

  // Call the SDK (if needed) to get the current transforms for the frame.
  // This is public so any game script can do this if they need the values.
  public bool UpdateState() {
      if (updated) {
          return true;
      }
#if ANDROID_DEVICE
      UpdateFrameParamsFromActivity();
#else
      UpdateSimulatedFrameParams();
#endif
      HeadRotation = Quaternion.LookRotation(headView.GetColumn(2), headView.GetColumn(1));
      leftEyeOffset = leftEyeView.GetColumn(3);
      rightEyeOffset = rightEyeView.GetColumn(3);
      updated = true;
      return true;
  }

  public void Recenter() {
#if ANDROID_DEVICE
      CallActivityMethod("resetHeadTracker");
#elif UNITY_EDITOR
      mouseX = mouseY = mouseZ = 0;
#endif
  }

  public void UpdateScreenData() {
#if ANDROID_DEVICE
    UpdateScreenDataFromActivity();
#else
    Profile = CardboardProfile.Default.Clone();
#endif
  }

  public void CreateStereoScreen(int x, int y) {
    if (config.canApplyDistortionCorrection()) {
      Debug.Log("Creating new cardboard screen texture.");
      if (stereoScreen != null) {
        stereoScreen.Release();
      }
      stereoScreen = new RenderTexture(x, y, 16, RenderTextureFormat.RGB565);
      stereoScreen.Create();
      InitFromUnity(stereoScreen.GetNativeTextureID());
    } else {
      stereoScreen = null;
      if (!Application.isEditor) {
        nativeDistortionCorrection = false;
        Debug.LogWarning("Lens distortion-correction disabled. Causes: [" +
                         config.getDistortionCorrectionDiagnostic() + "]");
      }
    }
  }

  public void CreateStereoScreen() {
    CreateStereoScreen(Screen.width, Screen.height);
  }

  // Makes Unity see a mouse click (down + up) at the given pixel.
  public void InjectMouseClick(int x, int y) {
#if ANDROID_DEVICE
      if (downTime == NO_DOWNTIME) {  // If not in the middle of a tap injection.
          StartCoroutine(DoAndroidScreenTap(x, y));
      }
#endif
  }

  // Makes Unity see a mouse move to the given pixel.
  public void InjectMouseMove(int x, int y) {
      if (x == (int)Input.mousePosition.x && y == (int)Input.mousePosition.y) {
          return;  // Don't send a 0-pixel move.
      }
#if ANDROID_DEVICE
      if (downTime == NO_DOWNTIME) {  // If not in the middle of a tap injection.
          CallActivityMethod("injectMouseMove", x, y);
      }
#endif
  }

  void OnInsertedIntoCardboardInternal() {
      newInCardboard = true;
  }

  void OnRemovedFromCardboardInternal() {
      newInCardboard = false;
  }

  void OnCardboardTriggerInternal() {
      newCardboardTriggered = true;
  }

  void OnDestroy() {
      StopCoroutine("EndOfFrame");
      if (sdk == this) {
          sdk = null;
      }
  }

  // Event IDs supported by our native render plugin.
  private const int kPerformDistortionCorrection = 1;
  private const int kDrawCardboardUILayer = 2;

  IEnumerator EndOfFrame() {
      while (true) {
          yield return new WaitForEndOfFrame();
          if (UpdateState() && vrModeEnabled && !Application.isEditor) {
              GL.InvalidateState();  // necessary for Windows, but not Mac.
              if (stereoScreen != null && nativeDistortionCorrection) {
                GL.IssuePluginEvent(kPerformDistortionCorrection);
              }
              if (enableSettingsButton || enableAlignmentMarker) {
                GL.IssuePluginEvent(kDrawCardboardUILayer);
              }
          }
          if (InCardboard != newInCardboard) {
              UpdateScreenData();
          }
          InCardboard = newInCardboard;
          CardboardTriggered = newCardboardTriggered;
          newCardboardTriggered = false;
          updated = false;
      }
  }

  private float[] GetLeftEyeVisibleTanAngles() {
    CardboardProfile p = Profile;
    // Tan-angles from the max FOV.
    float fovLeft = (float) Math.Tan(-p.device.maxFOV.outer * Math.PI / 180);
    float fovTop = (float) Math.Tan(p.device.maxFOV.upper * Math.PI / 180);
    float fovRight = (float) Math.Tan(p.device.maxFOV.inner * Math.PI / 180);
    float fovBottom = (float) Math.Tan(-p.device.maxFOV.lower * Math.PI / 180);
    // Viewport size.
    float halfWidth = p.screen.width / 4;
    float halfHeight = p.screen.height / 2;
    // Viewport center, measured from left lens position.
    float centerX = p.device.lenses.separation / 2 - halfWidth;
    float centerY = halfHeight + p.screen.border - p.device.lenses.height;
    float centerZ = p.device.lenses.screenDistance;
    // Tan-angles of the viewport edges, as seen through the lens.
    float screenLeft = p.device.distortion.distort((centerX - halfWidth) / centerZ);
    float screenTop = p.device.distortion.distort((centerY + halfHeight) / centerZ);
    float screenRight = p.device.distortion.distort((centerX + halfWidth) / centerZ);
    float screenBottom = p.device.distortion.distort((centerY - halfWidth) / centerZ);
    // Compare the two sets of tan-angles and take the value closer to zero on each side.
    float left = Math.Max(fovLeft, screenLeft);
    float top = Math.Min(fovTop, screenTop);
    float right = Math.Min(fovRight, screenRight);
    float bottom = Math.Max(fovBottom, screenBottom);
    return new float[] { left, top, right, bottom };
  }

  private float[] GetLeftEyeNoLensTanAngles() {
    CardboardProfile p = Profile;
    // Tan-angles from the max FOV.
    float fovLeft = p.device.inverse.distort((float)Math.Tan(-p.device.maxFOV.outer * Math.PI / 180));
    float fovTop = p.device.inverse.distort((float)Math.Tan(p.device.maxFOV.upper * Math.PI / 180));
    float fovRight = p.device.inverse.distort((float)Math.Tan(p.device.maxFOV.inner * Math.PI / 180));
    float fovBottom = p.device.inverse.distort((float)Math.Tan(-p.device.maxFOV.lower * Math.PI / 180));
    // Viewport size.
    float halfWidth = p.screen.width / 4;
    float halfHeight = p.screen.height / 2;
    // Viewport center, measured from left lens position.
    float centerX = p.device.lenses.separation / 2 - halfWidth;
    float centerY = halfHeight + p.screen.border - p.device.lenses.height;
    float centerZ = p.device.lenses.screenDistance;
    // Tan-angles of the viewport edges, as seen through the lens.
    float screenLeft = (centerX - halfWidth) / centerZ;
    float screenTop = (centerY + halfHeight) / centerZ;
    float screenRight = (centerX + halfWidth) / centerZ;
    float screenBottom = (centerY - halfWidth) / centerZ;
    // Compare the two sets of tan-angles and take the value closer to zero on each side.
    float left = Math.Max(fovLeft, screenLeft);
    float top = Math.Min(fovTop, screenTop);
    float right = Math.Min(fovRight, screenRight);
    float bottom = Math.Max(fovBottom, screenBottom);
    return new float[] { left, top, right, bottom };
  }

  private Rect GetLeftEyeVisibleScreenRect(float[] undistortedFrustum = null) {
    CardboardProfile p = Profile;
    float dist = p.device.lenses.screenDistance;
    float eyeX = (p.screen.width - p.device.lenses.separation) / 2;
    float eyeY = p.device.lenses.height - p.screen.border;
    float left = (undistortedFrustum[0] * dist + eyeX) / p.screen.width;
    float top = (undistortedFrustum[1] * dist + eyeY) / p.screen.height;
    float right = (undistortedFrustum[2] * dist + eyeX) / p.screen.width;
    float bottom = (undistortedFrustum[3] * dist + eyeY) / p.screen.height;
    return new Rect(left, bottom, right - left, top - bottom);
  }

#if ANDROID_DEVICE
  private const string cardboardClass =
    "com.google.vrtoolkit.cardboard.plugins.unity.UnityCardboardActivity";
  private AndroidJavaObject cardboardActivity;

  private bool CallActivityMethod(string name, params object[] args) {
    try {
      cardboardActivity.Call(name, args);
      return true;
    } catch (AndroidJavaException e) {
      Debug.LogError("Exception calling activity method " + name + ": " + e);
      return false;
    }

  }

  private bool CallActivityMethod<T>(ref T result, string name, params object[] args) {
    try {
      result = cardboardActivity.Call<T>(name, args);
      return true;
    } catch (AndroidJavaException e) {
      Debug.LogError("Exception calling activity method " + name + ": " + e);
      return false;
    }
  }

  private void ConnectToActivity() {
    try {
      using (AndroidJavaClass player = new AndroidJavaClass(cardboardClass)) {
        cardboardActivity = player.CallStatic<AndroidJavaObject>("getActivity");
      }
      config.canAccessActivity = true;
    } catch (AndroidJavaException e) {
      Debug.LogError("Cannot access UnityCardboardActivity. "
        + "Verify that the jar is in Assets/Plugins/Android. " + e);
    }
    CallActivityMethod("initFromUnity", gameObject.name);
  }

  private void UpdateScreenDataFromActivity() {
    CardboardProfile.Device device = new CardboardProfile.Device();
    CardboardProfile.Screen screen = new CardboardProfile.Screen();

    float[] lensData = null;
    if (CallActivityMethod(ref lensData, "getLensParameters")) {
      device.lenses.separation = lensData[0];
      device.lenses.height = lensData[1];
      device.lenses.screenDistance = lensData[2];
    }

    float[] screenSize = null;
    if (CallActivityMethod(ref screenSize, "getScreenSizeMeters")) {
      screen.width = screenSize[0];
      screen.height = screenSize[1];
      screen.border = screenSize[2];
    }

    float[] distCoeff = null;
    if (CallActivityMethod(ref distCoeff, "getDistortionCoefficients")) {
      device.distortion.k1 = distCoeff[0];
      device.distortion.k2 = distCoeff[1];
    }

    float[] invDistCoeff = null;
    if (CallActivityMethod(ref invDistCoeff, "getInverseDistortionCoefficients")) {
      device.inverse.k1 = invDistCoeff[0];
      device.inverse.k2 = invDistCoeff[1];

    }

    float[] maxFov = null;
    if (CallActivityMethod(ref maxFov, "getLeftEyeMaximumFOV")) {
      device.maxFOV.outer = maxFov[0];
      device.maxFOV.upper = maxFov[1];
      device.maxFOV.inner = maxFov[2];
      device.maxFOV.lower = maxFov[3];
    }

    Profile = new CardboardProfile { screen=screen, device=device };
  }

  // Right-handed to left-handed matrix converter.
  private static readonly Matrix4x4 flipZ = Matrix4x4.Scale(new Vector3(1, 1, -1));

  public void UpdateFrameParamsFromActivity() {
    float[] frameInfo = null;
    // Pass nominal clip distances - will correct later for each camera.
    if (!CallActivityMethod(ref frameInfo, "getFrameParams", 1.0f /* near */, 1000.0f /* far */)) {
      return;
    }

    // Extract the matrices (currently that's all we get back).
    int j = 0;
    for (int i = 0; i < 16; ++i, ++j) {
      headView[i] = frameInfo[j];
    }
    for (int i = 0; i < 16; ++i, ++j) {
      leftEyeView[i] = frameInfo[j];
    }
    for (int i = 0; i < 16; ++i, ++j) {
      leftEyeProj[i] = frameInfo[j];
    }
    for (int i = 0; i < 16; ++i, ++j) {
      rightEyeView[i] = frameInfo[j];
    }
    for (int i = 0; i < 16; ++i, ++j) {
      rightEyeProj[i] = frameInfo[j];
    }
    for (int i = 0; i < 16; ++i, ++j) {
      leftEyeUndistortedProj[i] = frameInfo[j];
    }
    for (int i = 0; i < 16; ++i, ++j) {
      rightEyeUndistortedProj[i] = frameInfo[j];
    }

    leftEyeRect = new Rect(frameInfo[j], frameInfo[j+1], frameInfo[j+2], frameInfo[j+3]);
    j += 4;
    rightEyeRect = new Rect(frameInfo[j], frameInfo[j+1], frameInfo[j+2], frameInfo[j+3]);

    // Convert views to left-handed coordinates because Unity uses them
    // for Transforms, which is what we will update from the views.
    // Also invert because the incoming matrices go from camera space to
    // cardboard space, and we want the opposite.
    // Lastly, cancel out the head rotation from the eye views,
    // because we are applying that on a parent object.
    leftEyeView = flipZ * headView * leftEyeView.inverse * flipZ;
    rightEyeView = flipZ * headView * rightEyeView.inverse * flipZ;
    headView = flipZ * headView.inverse * flipZ;
  }

  // How long to hold a simulated screen tap.
  private const float TAP_INJECTION_TIME = 0.1f;

  private const long NO_DOWNTIME = -1;  // Sentinel for when down time is not set.

  private long downTime = NO_DOWNTIME;  // Time the current tap injection started, if any.

  // Fakes a screen tap by injecting a pointer-down and pointer-up events
  // with a suitable delay between them.
  IEnumerator DoAndroidScreenTap(int x, int y) {
    if (downTime != NO_DOWNTIME) {  // Sanity check.
      yield break;
    }
    if (!CallActivityMethod(ref downTime, "injectTouchDown", x, y)) {
      yield break;
    }
    yield return new WaitForSeconds(TAP_INJECTION_TIME);
    CallActivityMethod("injectTouchUp", x, y, downTime);
    downTime = NO_DOWNTIME;
  }

#else

  private static Matrix4x4 MakeProjection(float l, float t, float r, float b, float n, float f) {
    Matrix4x4 m = Matrix4x4.zero;
    m[0, 0] = 2 * n / (r - l);
    m[1, 1] = 2 * n / (t - b);
    m[0, 2] = (r + l) / (r - l);
    m[1, 2] = (t + b) / (t - b);
    m[2, 2] = (n + f) / (n - f);
    m[2, 3] = 2 * n * f / (n - f);
    m[3, 2] = -1;
    return m;
  }

  private void UpdateSimulatedFrameParams() {
    // Mock implementation for use in the Unity editor.
    headView = Matrix4x4.identity;
#if UNITY_EDITOR
    SimulateHeadTracking();
#endif

    // Compute left eye matrices from screen and device params

    leftEyeView = Matrix4x4.identity;
    leftEyeView[0, 3] = -Profile.device.lenses.separation / 2;

    float[] rect = GetLeftEyeVisibleTanAngles();
    leftEyeProj = MakeProjection(rect[0], rect[1], rect[2], rect[3], 1, 1000);

    rect = GetLeftEyeNoLensTanAngles();
    leftEyeUndistortedProj = MakeProjection(rect[0], rect[1], rect[2], rect[3], 1, 1000);

    leftEyeRect = GetLeftEyeVisibleScreenRect(rect);

    // Right eye matrices same as left ones but for some sign flippage.

    rightEyeView = leftEyeView;
    rightEyeView[0, 3] *= -1;

    rightEyeProj = leftEyeProj;
    rightEyeProj[0, 2] *= -1;

    rightEyeUndistortedProj = leftEyeUndistortedProj;
    rightEyeUndistortedProj[0, 2] *= -1;

    rightEyeRect = leftEyeRect;
    rightEyeRect.x = 1 - rightEyeRect.xMax;
  }
#endif

#if UNITY_EDITOR
  [Tooltip("When playing in the editor, just release Ctrl to untilt the head.")]
  public bool autoUntiltHead = true;

  // Use mouse to emulate head in the editor.
  private float mouseX = 0;
  private float mouseY = 0;
  private float mouseZ = 0;

  private void SimulateHeadTracking() {
    bool rolled = false;
    if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)) {
      mouseX += Input.GetAxis("Mouse X") * 5;
      if (mouseX <= -180) {
        mouseX += 360;
      } else if (mouseX > 180)
        mouseX -= 360;
      mouseY -= Input.GetAxis("Mouse Y") * 2.4f;
      mouseY = Mathf.Clamp(mouseY, -80, 80);
    } else if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) {
      rolled = true;
      mouseZ += Input.GetAxis("Mouse X") * 5;
      mouseZ = Mathf.Clamp(mouseZ, -80, 80);
    }
    if (!rolled && autoUntiltHead) {
      // People don't usually leave their heads tilted to one side for long.
      mouseZ = Mathf.Lerp(mouseZ, 0, Time.deltaTime / (Time.deltaTime + 0.1f));
    }
    var rot = Quaternion.Euler(mouseY, mouseX, mouseZ);
    headView = Matrix4x4.TRS(Vector3.zero, rot, Vector3.one);
  }

  private const float TOUCH_TIME_LIMIT = 0.2f;
  private float touchStartTime = 0;

  private void SimulateTrigger() {
    if (!InCardboard) {
      return;  // Only simulate trigger pull if there is a trigger to pull.
    }

    if (Input.GetMouseButtonDown(0)) {
      touchStartTime = Time.time;
    } else if (Input.GetMouseButtonUp(0)) {
      if (Time.time - touchStartTime <= TOUCH_TIME_LIMIT) {
        newCardboardTriggered = true;
      }
      touchStartTime = 0;
    }
  }
#else
  private GUIStyle style;

  // The amount of time (in seconds) to show error message on GUI.
  private const float GUI_ERROR_MSG_DURATION = 10;

  private const string warning =
@"Distortion correction is disabled.
Requires Unity Android Pro v4.5+.
See log for details.";

  void OnGUI() {
      if (Debug.isDebugBuild && !config.canApplyDistortionCorrection() &&
              Time.realtimeSinceStartup <= GUI_ERROR_MSG_DURATION) {
          DisplayDistortionCorrectionDisabledWarning();
      }
  }

  private void DisplayDistortionCorrectionDisabledWarning() {
      if (style == null) {
          style = new GUIStyle();
          style.normal.textColor = Color.red;
          style.alignment = TextAnchor.LowerCenter;
      }
      if (VRModeEnabled) {
          style.fontSize = (int)(50 * Screen.width / 1920f / 2);
          GUI.Label(new Rect(0, 0, Screen.width / 2, Screen.height), warning, style);
          GUI.Label(new Rect(Screen.width / 2, 0, Screen.width / 2, Screen.height), warning, style);
      } else {
          style.fontSize = (int)(50 * Screen.width / 1920f);
          GUI.Label(new Rect(0, 0, Screen.width, Screen.height), warning, style);
      }
  }
#endif
}
