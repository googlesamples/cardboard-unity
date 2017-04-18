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
using System;
using System.Collections.Generic;

using Gvr.Internal;

/// The GvrViewer object communicates with the head-mounted display.
/// Is is repsonsible for:
/// -  Querying the device for viewing parameters
/// -  Retrieving the latest head tracking data
/// -  Providing the rendered scene to the device for distortion correction (optional)
///
/// There should only be one of these in a scene.  An instance will be generated automatically
/// by this script at runtime, or you can add one via the Editor if you wish to customize
/// its starting properties.
[AddComponentMenu("GoogleVR/GvrViewer")]
public class GvrViewer : MonoBehaviour {
  /// The singleton instance of the GvrViewer class.
  public static GvrViewer Instance {
    get {
#if UNITY_EDITOR
      if (instance == null && !Application.isPlaying) {
        instance = UnityEngine.Object.FindObjectOfType<GvrViewer>();
      }
#endif
      if (instance == null) {
        Debug.LogError("No GvrViewer instance found.  Ensure one exists in the scene, or call "
            + "GvrViewer.Create() at startup to generate one.\n"
            + "If one does exist but hasn't called Awake() yet, "
            + "then this error is due to order-of-initialization.\n"
            + "In that case, consider moving "
            + "your first reference to GvrViewer.Instance to a later point in time.\n"
            + "If exiting the scene, this indicates that the GvrViewer object has already "
            + "been destroyed.");
      }
      return instance;
    }
  }
  private static GvrViewer instance = null;

  /// Generate a GvrViewer instance.  Takes no action if one already exists.
  public static void Create() {
    if (instance == null && UnityEngine.Object.FindObjectOfType<GvrViewer>() == null) {
      Debug.Log("Creating GvrViewer object");
      var go = new GameObject("GvrViewer", typeof(GvrViewer));
      go.transform.localPosition = Vector3.zero;
      // sdk will be set by Awake().
    }
  }

  /// The StereoController instance attached to the main camera, or null if there is none.
  /// @note Cached for performance.
  public static StereoController Controller {
    get {
#if !UNITY_HAS_GOOGLEVR || UNITY_EDITOR
      Camera camera = Camera.main;
      // Cache for performance, if possible.
      if (camera != currentMainCamera || currentController == null) {
        currentMainCamera = camera;
        currentController = camera.GetComponent<StereoController>();
      }
      return currentController;
#else
      return null;
#endif  // !UNITY_HAS_GOOGLEVR || UNITY_EDITOR
    }
  }
  private static StereoController currentController;
  private static Camera currentMainCamera;

  /// Determine whether the scene renders in stereo or mono.
  /// Supported only for versions of Unity *without* the GVR integration.
  /// VRModeEnabled will be a no-op for versions of Unity with the GVR integration.
  /// _True_ means to render in stereo, and _false_ means to render in mono.
  public bool VRModeEnabled {
    get {
#if !UNITY_HAS_GOOGLEVR || UNITY_EDITOR
      return vrModeEnabled;
#else
      return UnityEngine.VR.VRSettings.enabled;
#endif  // !UNITY_HAS_GOOGLEVR || UNITY_EDITOR
    }
    set {
#if !UNITY_HAS_GOOGLEVR || UNITY_EDITOR
      if (value != vrModeEnabled && device != null) {
        device.SetVRModeEnabled(value);
      }
      vrModeEnabled = value;
#endif  // !UNITY_HAS_GOOGLEVR || UNITY_EDITOR
    }
  }

// Ignore private field is assigned but its value is never used compile warning.
#pragma warning disable 414
  [SerializeField]
  private bool vrModeEnabled = true;
#pragma warning restore 414

  /// Methods for performing lens distortion correction.
  public enum DistortionCorrectionMethod {
      None,    ///< No distortion correction
      Native,  ///< Use the native C++ plugin
      Unity,   ///< Perform distortion correction in Unity (recommended)
  }

  /// Determines the distortion correction method used by the SDK to render the
  /// #StereoScreen texture on the phone.  If _Native_ is selected but not supported
  /// by the device, the _Unity_ method will be used instead.
  public DistortionCorrectionMethod DistortionCorrection {
    get {
      return distortionCorrection;
    }
    set {
      if (device != null && device.RequiresNativeDistortionCorrection()) {
        value = DistortionCorrectionMethod.Native;
      }
      if (value != distortionCorrection && device != null) {
        device.SetDistortionCorrectionEnabled(value == DistortionCorrectionMethod.Native
            && NativeDistortionCorrectionSupported);
        device.UpdateScreenData();
      }
      distortionCorrection = value;
    }
  }
  [SerializeField]
#if UNITY_HAS_GOOGLEVR && UNITY_ANDROID && !UNITY_EDITOR
  private DistortionCorrectionMethod distortionCorrection = DistortionCorrectionMethod.Native;
#else
  private DistortionCorrectionMethod distortionCorrection = DistortionCorrectionMethod.Unity;
#endif  // UNITY_HAS_GOOGLEVR && UNITY_ANDROID && !UNITY_EDITOR

  /// The native SDK will apply a neck offset to the head tracking, resulting in
  /// a more realistic model of a person's head position.  This control determines
  /// the scale factor of the offset.  To turn off the neck model, set it to 0, and
  /// to turn it all on, set to 1.  Intermediate values can be used to animate from
  /// on to off or vice versa.
  public float NeckModelScale {
    get {
      return neckModelScale;
    }
    set {
      value = Mathf.Clamp01(value);
      if (!Mathf.Approximately(value, neckModelScale) && device != null) {
        device.SetNeckModelScale(value);
      }
      neckModelScale = value;
    }
  }
  [SerializeField]
  private float neckModelScale = 0.0f;

#if UNITY_EDITOR
  /// Restores level head tilt in when playing in the Unity Editor after you
  /// release the Ctrl key.
  public bool autoUntiltHead = true;

  /// @cond
  /// Use unity remote as the input source.
  public bool UseUnityRemoteInput = false;
  /// @endcond

  /// The screen size to emulate when testing in the Unity Editor.
  public GvrProfile.ScreenSizes ScreenSize {
    get {
      return screenSize;
    }
    set {
      if (value != screenSize) {
        screenSize = value;
        if (device != null) {
          device.UpdateScreenData();
        }
      }
    }
  }
  [SerializeField]
  private GvrProfile.ScreenSizes screenSize = GvrProfile.ScreenSizes.Nexus5;

  /// The viewer type to emulate when testing in the Unity Editor.
  public GvrProfile.ViewerTypes ViewerType {
    get {
      return viewerType;
    }
    set {
      if (value != viewerType) {
        viewerType = value;
        if (device != null) {
          device.UpdateScreenData();
        }
      }
    }
  }
  [SerializeField]
  private GvrProfile.ViewerTypes viewerType = GvrProfile.ViewerTypes.CardboardMay2015;
#endif

  // The VR device that will be providing input data.
  private static BaseVRDevice device;

  /// Whether native distortion correction functionality is supported by the VR device.
  public bool NativeDistortionCorrectionSupported { get; private set; }

  /// Whether the VR device supports showing a native UI layer, for example for settings.
  public bool NativeUILayerSupported { get; private set; }

  /// Scales the resolution of the #StereoScreen.  Set to less than 1.0 to increase
  /// rendering speed while decreasing sharpness, or greater than 1.0 to do the
  /// opposite.
  public float StereoScreenScale {
    get {
      return stereoScreenScale;
    }
    set {
      value = Mathf.Clamp(value, 0.1f, 10.0f);  // Sanity.
      if (stereoScreenScale != value) {
        stereoScreenScale = value;
        StereoScreen = null;
      }
    }
  }
  [SerializeField]
  private float stereoScreenScale = 1;

  /// The texture that Unity renders the scene to.  After the frame has been rendered,
  /// this texture is drawn to the screen with a lens distortion correction effect.
  /// The texture size is based on the size of the screen, the lens distortion
  /// parameters, and the #StereoScreenScale factor.
  public RenderTexture StereoScreen {
    get {
      // Don't need it except for distortion correction.
      if (distortionCorrection == DistortionCorrectionMethod.None || !VRModeEnabled) {
        return null;
      }
      if (stereoScreen == null) {
        // Create on demand.
        StereoScreen = device.CreateStereoScreen();  // Note: uses set{}
      }
      return stereoScreen;
    }
    set {
      if (value == stereoScreen) {
        return;
      }
      if (stereoScreen != null) {
        stereoScreen.Release();
      }
      stereoScreen = value;
      if (OnStereoScreenChanged != null) {
        OnStereoScreenChanged(stereoScreen);
      }
    }
  }
  private static RenderTexture stereoScreen = null;

  /// A callback for notifications that the StereoScreen property has changed.
  public delegate void StereoScreenChangeDelegate(RenderTexture newStereoScreen);

  /// Emitted when the StereoScreen property has changed.
  public event StereoScreenChangeDelegate OnStereoScreenChanged;

  /// Describes the current device, including phone screen.
  public GvrProfile Profile {
    get {
      return device.Profile;
    }
  }

  /// Returns true if GoogleVR is NOT supported natively.
  /// That is, this version of Unity does not have native integration but supports
  /// the GVR SDK (5.2, 5.3), or the current VR player is the in-editor emulator.
  public static bool NoNativeGVRSupport {
    get {
#if !UNITY_HAS_GOOGLEVR || UNITY_EDITOR
      return true;
#else
      return false;
#endif  // !UNITY_HAS_GOOGLEVR || UNITY_EDITOR
    }
  }

  /// Distinguish the stereo eyes.
  public enum Eye {
    Left,   ///< The left eye
    Right,  ///< The right eye
    Center  ///< The "center" eye (unused)
  }

  /// When retrieving the #Projection and #Viewport properties, specifies
  /// whether you want the values as seen through the viewer's lenses (`Distorted`) or
  /// as if no lenses were present (`Undistorted`).
  public enum Distortion {
    Distorted,   ///< Viewing through the lenses
    Undistorted  ///< No lenses
  }

  /// The transformation of head from origin in the tracking system.
  public Pose3D HeadPose {
    get {
      return device.GetHeadPose();
    }
  }

  /// The transformation from head to eye.
  public Pose3D EyePose(Eye eye) {
    return device.GetEyePose(eye);
  }

  /// The projection matrix for a given eye.
  /// This matrix is an off-axis perspective projection with near and far
  /// clipping planes of 1m and 1000m, respectively.  The GvrEye script
  /// takes care of adjusting the matrix for its particular camera.
  public Matrix4x4 Projection(Eye eye, Distortion distortion = Distortion.Distorted) {
    return device.GetProjection(eye, distortion);
  }

  /// The screen space viewport that the camera for the specified eye should render into.
  /// In the _Distorted_ case, this will be either the left or right half of the `StereoScreen`
  /// render texture.  In the _Undistorted_ case, it refers to the actual rectangle on the
  /// screen that the eye can see.
  public Rect Viewport(Eye eye, Distortion distortion = Distortion.Distorted) {
    return device.GetViewport(eye, distortion);
  }

  /// The distance range from the viewer in user-space meters where objects may be viewed
  /// comfortably in stereo.  If the center of interest falls outside this range, the stereo
  /// eye separation should be adjusted to keep the onscreen disparity within the limits set
  /// by this range.  If native integration is not supported, or the current VR player is the
  /// in-editor emulator, StereoController will handle this if the _checkStereoComfort_ is
  /// enabled.
  public Vector2 ComfortableViewingRange {
    get {
      return defaultComfortableViewingRange;
    }
  }
  private readonly Vector2 defaultComfortableViewingRange = new Vector2(0.4f, 100000.0f);

  /// @cond
  // Optional.  Set to a URI obtained from the Google Cardboard profile generator at
  //   https://www.google.com/get/cardboard/viewerprofilegenerator/
  // Example: Cardboard I/O 2015 viewer profile
  //public Uri DefaultDeviceProfile = new Uri("http://google.com/cardboard/cfg?p=CgZHb29nbGUSEkNhcmRib2FyZCBJL08gMjAxNR0J-SA9JQHegj0qEAAAcEIAAHBCAABwQgAAcEJYADUpXA89OghX8as-YrENP1AAYAM");
  public Uri DefaultDeviceProfile = null;
  /// @endcond

  // The screen sleep timeout used when instantiating GvrViewer
  private int oldScreenSleepTimeout;

  private void InitDevice() {
    if (device != null) {
      device.Destroy();
    }
    device = BaseVRDevice.GetDevice();
    device.Init();

    List<string> diagnostics = new List<string>();
    NativeDistortionCorrectionSupported = device.SupportsNativeDistortionCorrection(diagnostics);
    if (diagnostics.Count > 0) {
      Debug.LogWarning("Built-in distortion correction disabled. Causes: ["
                       + String.Join("; ", diagnostics.ToArray()) + "]");
    }
    diagnostics.Clear();
    NativeUILayerSupported = device.SupportsNativeUILayer(diagnostics);
    if (diagnostics.Count > 0) {
      Debug.LogWarning("Built-in UI layer disabled. Causes: ["
                       + String.Join("; ", diagnostics.ToArray()) + "]");
    }

    if (DefaultDeviceProfile != null) {
      device.SetDefaultDeviceProfile(DefaultDeviceProfile);
    }

    device.SetDistortionCorrectionEnabled(distortionCorrection == DistortionCorrectionMethod.Native
        && NativeDistortionCorrectionSupported);
    device.SetNeckModelScale(neckModelScale);

#if !UNITY_HAS_GOOGLEVR || UNITY_EDITOR
    device.SetVRModeEnabled(vrModeEnabled);
#endif  // !UNITY_HAS_GOOGLEVR || UNITY_EDITOR

    device.UpdateScreenData();
  }

  /// @note Each scene load causes an OnDestroy of the current SDK, followed
  /// by and Awake of a new one.  That should not cause the underlying native
  /// code to hiccup.  Exception: developer may call Application.DontDestroyOnLoad
  /// on the SDK if they want it to survive across scene loads.
  void Awake() {
    if (instance == null) {
      instance = this;
    }
    if (instance != this) {
      Debug.LogError("There must be only one GvrViewer object in a scene.");
      UnityEngine.Object.DestroyImmediate(this);
      return;
    }
#if UNITY_IOS
    Application.targetFrameRate = 60;
#endif
    // Prevent the screen from dimming / sleeping but remember current value to restore it later
    oldScreenSleepTimeout = Screen.sleepTimeout;
    if (oldScreenSleepTimeout >= 0) {
      // "get" may return the number of seconds while "set" only allows predefined values
      oldScreenSleepTimeout = SleepTimeout.SystemSetting;
    }
    Screen.sleepTimeout = SleepTimeout.NeverSleep;
    InitDevice();
    StereoScreen = null;

// Set up stereo pre- and post-render stages only for:
// - Unity without the GVR native integration
// - In-editor emulator when the current platform is Android or iOS.
//   Since GVR is the only valid VR SDK on Android or iOS, this prevents it from
//   interfering with VR SDKs on other platforms.
#if !UNITY_HAS_GOOGLEVR || (UNITY_EDITOR && (UNITY_ANDROID || UNITY_IOS))
      AddPrePostRenderStages();
#endif  // !UNITY_HAS_GOOGLEVR || UNITY_EDITOR
  }

  void Start() {
// Set up stereo controller only for:
// - Unity without the GVR native integration
// - In-editor emulator when the current platform is Android or iOS.
//   Since GVR is the only valid VR SDK on Android or iOS, this prevents it from
//   interfering with VR SDKs on other platforms.
#if !UNITY_HAS_GOOGLEVR || (UNITY_EDITOR && (UNITY_ANDROID || UNITY_IOS))
      AddStereoControllerToCameras();
#endif  // !UNITY_HAS_GOOGLEVR || UNITY_EDITOR
  }

#if !UNITY_HAS_GOOGLEVR || UNITY_EDITOR
  void AddPrePostRenderStages() {
    var preRender = UnityEngine.Object.FindObjectOfType<GvrPreRender>();
    if (preRender == null) {
      var go = new GameObject("PreRender", typeof(GvrPreRender));
      go.SendMessage("Reset");
      go.transform.parent = transform;
    }
    var postRender = UnityEngine.Object.FindObjectOfType<GvrPostRender>();
    if (postRender == null) {
      var go = new GameObject("PostRender", typeof(GvrPostRender));
      go.SendMessage("Reset");
      go.transform.parent = transform;
    }
  }
#endif  // !UNITY_HAS_GOOGLEVR || UNITY_EDITOR

  /// Whether the viewer's trigger was pulled. True for exactly one complete frame
  /// after each pull.
  public bool Triggered { get; private set; }

  /// Whether the viewer was tilted on its side. True for exactly one complete frame
  /// after each tilt.  Whether and how to respond to this event is up to the app.
  public bool Tilted { get; private set; }

  /// Whether the viewer profile has possibly changed.  This is meant to indicate
  /// that a new QR code has been scanned, although currently it is actually set any time the
  /// application is unpaused, whether it was due to a profile change or not.  True for one
  /// frame.
  public bool ProfileChanged { get; private set; }

  /// Whether the user has pressed the "VR Back Button", which on Android should be treated the
  /// same as the normal system Back Button, although you can respond to either however you want
  /// in your app.
  public bool BackButtonPressed { get; private set; }

  // Only call device.UpdateState() once per frame.
  private int updatedToFrame = 0;

  /// Reads the latest tracking data from the phone.  This must be
  /// called before accessing any of the poses and matrices above.
  ///
  /// Multiple invocations per frame are OK:  Subsequent calls merely yield the
  /// cached results of the first call.  To minimize latency, it should be first
  /// called later in the frame (for example, in `LateUpdate`) if possible.
  public void UpdateState() {
    if (updatedToFrame != Time.frameCount) {
      updatedToFrame = Time.frameCount;
      device.UpdateState();

      if (device.profileChanged) {
        if (distortionCorrection != DistortionCorrectionMethod.Native &&
            device.RequiresNativeDistortionCorrection()) {
          DistortionCorrection = DistortionCorrectionMethod.Native;
        }
        if (stereoScreen != null &&
            device.ShouldRecreateStereoScreen(stereoScreen.width, stereoScreen.height)) {
          StereoScreen = null;
        }
      }

      DispatchEvents();
    }
  }

  private void DispatchEvents() {
      // Update flags first by copying from device and other inputs.
    Triggered = Input.GetMouseButtonDown(0);
#if UNITY_HAS_GOOGLEVR && (UNITY_ANDROID || UNITY_EDITOR)
    Triggered |= GvrController.ClickButtonDown;
#endif  // UNITY_HAS_GOOGLEVR && (UNITY_ANDROID || UNITY_EDITOR)

    Tilted = device.tilted;
    ProfileChanged = device.profileChanged;
    BackButtonPressed = device.backButtonPressed || Input.GetKeyDown(KeyCode.Escape);
    // Reset device flags.
    device.tilted = false;
    device.profileChanged = false;
    device.backButtonPressed = false;
  }

  /// Presents the #StereoScreen to the device for distortion correction and display.
  /// @note This function is only used if #DistortionCorrection is set to _Native_,
  /// and it only has an effect if the device supports it.
  public void PostRender(RenderTexture stereoScreen) {
    if (NativeDistortionCorrectionSupported && stereoScreen != null && stereoScreen.IsCreated()) {
      device.PostRender(stereoScreen);
    }
  }

  /// Resets the tracker so that the user's current direction becomes forward.
  public void Recenter() {
    device.Recenter();
  }

  /// Launch the device pairing and setup dialog.
  public void ShowSettingsDialog() {
    device.ShowSettingsDialog();
  }

#if !UNITY_HAS_GOOGLEVR || UNITY_EDITOR
  /// Add a StereoController to any camera that does not have a Render Texture (meaning it is
  /// rendering to the screen).
  public static void AddStereoControllerToCameras() {
    for (int i = 0; i < Camera.allCameras.Length; i++) {
      Camera camera = Camera.allCameras[i];
      if (camera.targetTexture == null &&
          camera.cullingMask != 0 &&
          camera.GetComponent<StereoController>() == null &&
          camera.GetComponent<GvrEye>() == null &&
          camera.GetComponent<GvrPreRender>() == null &&
          camera.GetComponent<GvrPostRender>() == null) {
        camera.gameObject.AddComponent<StereoController>();
      }
    }
  }
#endif  // !UNITY_HAS_GOOGLEVR || UNITY_EDITOR

  void OnEnable() {
#if UNITY_EDITOR
    // This can happen if you edit code while the editor is in Play mode.
    if (device == null) {
      InitDevice();
    }
#endif
    device.OnPause(false);
  }

  void OnDisable() {
    device.OnPause(true);
  }

  void OnApplicationPause(bool pause) {
    device.OnPause(pause);
  }

  void OnApplicationFocus(bool focus) {
    device.OnFocus(focus);
  }

  void OnApplicationQuit() {
    device.OnApplicationQuit();
  }

  void OnDestroy() {
#if !UNITY_HAS_GOOGLEVR || UNITY_EDITOR
    VRModeEnabled = false;
#endif  // !UNITY_HAS_GOOGLEVR || UNITY_EDITOR
    if (device != null) {
      device.Destroy();
    }
    Screen.sleepTimeout = oldScreenSleepTimeout;
    if (instance == this) {
      instance = null;
    }
  }
}
