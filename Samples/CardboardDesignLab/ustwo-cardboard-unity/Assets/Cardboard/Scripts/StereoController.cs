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
using System.Collections;
using System.Linq;

/// @ingroup Scripts
/// Controls a pair of CardboardEye objects that will render the stereo view
/// of the camera this script is attached to.
///
/// This script must be added to any camera that should render stereo when the app
/// is in VR Mode.  This includes picture-in-picture windows, whether their contents
/// are in stereo or not: the window itself must be twinned for stereo, regardless.
///
/// For each frame, StereoController decides whether to render via the camera it
/// is attached to (the _mono_ camera) or the stereo eyes that it controls (see
/// CardboardEye). You control this  decision for all cameras at once by setting
/// the value of Cardboard#VRModeEnabled.
///
/// For technical reasons, the mono camera remains enabled for the initial portion of
/// the frame.  It is disabled only when rendering begins in `OnPreCull()`, and is
/// reenabled again at the end of the frame.  This allows 3rd party scripts that use
/// `Camera.main`, for example, to refer the the mono camera even when VR Mode is
/// enabled.
///
/// At startup the script ensures it has a full stereo rig, which consists of two
/// child cameras with CardboardEye scripts attached, and a CardboardHead script
/// somewhere in the hierarchy of parents and children for head tracking.  The rig
/// is created if necessary, the CardboardHead being attached to the controller
/// itself.  The child camera settings are then cloned or updated from the mono
/// camera.
///
/// It is permissible for a StereoController to contain another StereoController
/// as a child.  In this case, a CardboardEye is controlled by its closest
/// StereoController parent.
///
/// The Inspector panel for this script includes a button _Update Stereo Cameras_.
/// This performs the same action as described above for startup, but in the Editor.
/// Use this to generate the rig if you intend to customize it.  This action is also
/// available via _Component -> Cardboard -> Update Stereo Cameras_ in the Editor’s
/// main menu, and in the context menu for the `Camera` component.
[RequireComponent(typeof(Camera))]
[AddComponentMenu("Cardboard/StereoController")]
public class StereoController : MonoBehaviour {
  /// Whether to draw directly to the output window (_true_), or to an offscreen buffer
  /// first and then blit (_false_). If you wish to use Deferred Rendering or any
  /// Image Effects in stereo, turn this option off.  A common symptom that indicates
  /// you should do so is when one of the eyes is spread across the entire screen.
  [Tooltip("Whether to draw directly to the output window (true), or " +
           "to an offscreen buffer first and then blit (false).  Image " +
           " Effects and Deferred Lighting may only work if set to false.")]
  public bool directRender = true;

  /// When enabled, UpdateStereoValues() is called every frame to keep the stereo cameras
  /// completely synchronized with both the mono camera and the device profile.  When
  /// disabled, you must call UpdateStereoValues() whenever you make a change to the mono
  /// camera that should be mirrored to the stereo cameras.  Changes to the device profile
  /// are handled automatically.  It is better for performance to leave this option disabled
  /// whenever possible.  Good use cases for enabling it are when animating values on the
  /// mono camera (like background color), or during development to debug camera synchronization
  /// issues.
  [Tooltip("When enabled, UpdateStereoValues() is called every frame to keep the stereo cameras " +
           "completely synchronized with both the mono camera and the device profile. It is " +
           "better for performance to leave this option disabled whenever possible.")]
  public bool keepStereoUpdated = false;

  /// Adjusts the level of stereopsis for this stereo rig.
  /// @note This parameter is not the virtual size of the head -- use a scale
  /// on the head game object for that.  Instead, it is a control on eye vergence,
  /// or rather, how cross-eyed or not the stereo rig is.  Set to 0 to turn
  /// off stereo in this rig independently of any others.
  [Tooltip("Set the stereo level for this camera.")]
  [Range(0,1)]
  public float stereoMultiplier = 1.0f;

  /// The stereo cameras by default use the actual optical FOV of the Cardboard device,
  /// because otherwise the match between head motion and scene motion is broken, which
  /// impacts the virtual reality effect.  However, in some cases it is desirable to
  /// adjust the FOV anyway, for special effects or artistic reasons.  But in no case
  /// should the FOV be allowed to remain very different from the true optical FOV for
  /// very long, or users will experience discomfort.
  ///
  /// This value determines how much to match the mono camera's field of view.  This is
  /// a fraction: 0 means no matching, 1 means full matching, and values in between are
  /// compromises.  Reasons for not matching 100% would include preserving some VR-ness,
  /// and that due to the lens distortion the edges of the view are not as easily seen as
  /// when the phone is not in VR-mode.
  ///
  /// Another use for this variable is to preserve scene composition against differences
  /// in the optical FOV of various Cardboard models.  In all cases, this value simply
  /// lets the mono camera have some control over the scene in VR mode, like it does in
  /// non-VR mode.
  [Tooltip("How much to adjust the stereo field of view to match this camera.")]
  [Range(0,1)]
  public float matchMonoFOV = 0;

  /// Determines the method by which the stereo cameras' FOVs are matched to the mono
  /// camera's FOV (assuming #matchMonoFOV is not 0).  The default is to move the stereo
  /// cameras (#matchByZoom = 0), with the option to instead do a simple camera zoom
  /// (#matchByZoom = 1).  In-between values yield a mix of the two behaviors.
  ///
  /// It is not recommended to use simple zooming for typical scene composition, as it
  /// conflicts with the VR need to match the user's head motion with the corresponding
  /// scene motion.  This should be reserved for special effects such as when the player
  /// views the scene through a telescope or other magnifier (and thus the player knows
  /// that VR is going to be affected), or similar situations.
  ///
  /// @note Matching by moving the eyes requires that the #centerOfInterest object
  /// be non-null, or there will be no effect.
  [Tooltip("Whether to adjust FOV by moving the eyes (0) or simply zooming (1).")]
  [Range(0,1)]
  public float matchByZoom = 0;

  /// Matching the mono camera's field of view in stereo by moving the eyes requires
  /// a designated "center of interest".  This is either a point in space (an empty
  /// gameobject) you place in the scene as a sort of "3D cursor", or an actual scene
  /// entity which the player is likely to be focussed on.
  ///
  /// The FOV adjustment is done by moving the eyes toward or away from the COI
  /// so that it appears to have the same size on screen as it would in the mono
  /// camera.  This is disabled if the COI is null.
  [Tooltip("Object or point where field of view matching is done.")]
  public Transform centerOfInterest;

  /// The #centerOfInterest is generally meant to be just a point in space, like a 3D cursor.
  /// Occasionally, you will want it to be an actual object with size.  Set this
  /// to the approximate radius of the object to help the FOV-matching code
  /// compensate for the object's horizon when it is close to the camera.
  [Tooltip("If COI is an object, its approximate size.")]
  public float radiusOfInterest = 0;

  /// If true, check that the #centerOfInterest is between the min and max comfortable
  /// viewing distances (see Cardboard.cs), or else adjust the stereo multiplier to
  /// compensate.  If the COI has a radius, then the near side is checked.  COI must
  /// be non-null for this setting to have any effect.
  [Tooltip("Adjust stereo level when COI gets too close or too far.")]
  public bool checkStereoComfort = true;

  /// Smoothes the changes to the stereo camera FOV and position based on #centerOfInterest
  /// and #checkStereoComfort.
  [Tooltip("Smoothing factor to use when adjusting stereo for COI and comfort.")]
  [Range(0,1)]
  public float stereoAdjustSmoothing = 0.1f;

  /// For picture-in-picture cameras that don't fill the entire screen,
  /// set the virtual depth of the window itself.  A value of 0 means
  /// zero parallax, which is fairly close.  A value of 1 means "full"
  /// parallax, which is equal to the interpupillary distance and equates
  /// to an infinitely distant window.  This does not affect the actual
  /// screen size of the the window (in pixels), only the stereo separation
  /// of the left and right images.
  [Tooltip("Adjust the virtual depth of this camera's window (picture-in-picture only).")]
  [Range(0,1)]
  public float screenParallax = 0;

  /// For picture-in-picture cameras, move the window away from the edges
  /// in VR Mode to make it easier to see.  The optics of HMDs make the screen
  /// edges hard to see sometimes, so you can use this to keep the PIP visible
  /// whether in VR Mode or not.  The x value is the fraction of the screen along
  /// either side to pad.
  [Tooltip("Move the camera window horizontally towards the center of the screen (PIP only).")]
  [Range(0,1)]
  public float stereoPaddingX = 0;

  /// For picture-in-picture cameras, move the window away from the edges
  /// in VR Mode to make it easier to see.  The optics of HMDs make the screen
  /// edges hard to see sometimes, so you can use this to keep the PIP visible
  /// whether in VR Mode or not.  The y value is for the top and bottom of the screen to pad.
  [Tooltip("Move the camera window vertically towards the center of the screen (PIP only).")]
  [Range(0,1)]
  public float stereoPaddingY = 0;

  // Flags whether we rendered in stereo for this frame.
  private bool renderedStereo = false;

#if !UNITY_EDITOR
  // Cache for speed, except in editor (don't want to get out of sync with the scene).
  private CardboardEye[] eyes;
  private CardboardHead head;
#endif

  /// Returns an array of stereo cameras that are controlled by this instance of
  /// the script.
  /// @note This array is cached for speedier access.  Call
  /// InvalidateEyes if it is ever necessary to reset the cache.
  public CardboardEye[] Eyes {
    get {
#if UNITY_EDITOR
      CardboardEye[] eyes = null;  // Local variable rather than member, so as not to cache.
#endif
      if (eyes == null) {
        eyes = GetComponentsInChildren<CardboardEye>(true)
               .Where(eye => eye.Controller == this)
               .ToArray();
      }
      return eyes;
    }
  }

  /// Returns the nearest CardboardHead that affects our eyes.
  /// @note Cached for speed.  Call InvalidateEyes to clear the cache.
  public CardboardHead Head {
    get {
#if UNITY_EDITOR
      CardboardHead head = null;  // Local variable rather than member, so as not to cache.
#endif
      if (head == null) {
        head = Eyes.Select(eye => eye.Head).FirstOrDefault();
      }
      return head;
    }
  }

  /// Clear the cached array of CardboardEye children, as well as the CardboardHead that controls
  /// their gaze.
  /// @note Be sure to call this if you programmatically change the set of CardboardEye children
  /// managed by this StereoController.
  public void InvalidateEyes() {
#if !UNITY_EDITOR
    eyes = null;
    head = null;
#endif
  }

  /// Updates the stereo cameras from the mono camera every frame.  This includes all Camera
  /// component values such as background color, culling mask, viewport rect, and so on.  Also,
  /// it includes updating the viewport rect and projection matrix for side-by-side stereo, plus
  /// applying any adjustments for center of interest and stereo comfort.
  public void UpdateStereoValues() {
    CardboardEye[] eyes = Eyes;
    for (int i = 0, n = eyes.Length; i < n; i++) {
      eyes[i].UpdateStereoValues();
    }
  }

  public Camera cam { get; private set; }

  void Awake() {
    Cardboard.Create();
    cam = GetComponent<Camera>();
    AddStereoRig();
  }

  /// Helper routine for creation of a stereo rig.  Used by the
  /// custom editor for this class, or to build the rig at runtime.
  public void AddStereoRig() {
#if UNITY_EDITOR
    // Member variable 'cam' not always initialized when this method called in Editor.
    // So, we'll just make a local of the same name.
    var cam = GetComponent<Camera>();
#endif

    // Simplistic test if rig already exists.
    // Note: Do not use Eyes property, because it caches the result before we have created the rig.
    var eyes = GetComponentsInChildren<CardboardEye>(true).Where(eye => eye.Controller == this);
    if (eyes.Any()) {
      return;
    }
    CreateEye(Cardboard.Eye.Left);
    CreateEye(Cardboard.Eye.Right);
    if (Head == null) {
      var head = gameObject.AddComponent<CardboardHead>();
      // Don't track position for dynamically added Head components, or else
      // you may unexpectedly find your camera pinned to the origin.
      head.trackPosition = false;
    }
#if !UNITY_5
    if (cam.tag == "MainCamera" && GetComponent<SkyboxMesh>() == null) {
      gameObject.AddComponent<SkyboxMesh>();
    }
#endif
  }

  // Helper routine for creation of a stereo eye.
  private void CreateEye(Cardboard.Eye eye) {
    string nm = name + (eye == Cardboard.Eye.Left ? " Left" : " Right");
    GameObject go = new GameObject(nm);
    go.transform.SetParent(transform, false);
    go.AddComponent<Camera>().enabled = false;
#if !UNITY_5
    if (GetComponent("FlareLayer") != null) {
      go.AddComponent("FlareLayer");
    }
#endif
    var cardboardEye = go.AddComponent<CardboardEye>();
    cardboardEye.eye = eye;
    cardboardEye.CopyCameraAndMakeSideBySide(this);
  }

  /// Compute the position of one of the stereo eye cameras.  Accounts for both
  /// FOV matching and stereo comfort, if those features are enabled.  The input is
  /// the [1,1] entry of the eye camera's projection matrix, representing the vertical
  /// field of view, and the overall scale being applied to the Z axis.  Returns the
  /// position of the stereo eye camera in local coordinates.
  public Vector3 ComputeStereoEyePosition(Cardboard.Eye eye, float proj11, float zScale) {
    if (centerOfInterest == null || !centerOfInterest.gameObject.activeInHierarchy) {
      return Cardboard.SDK.EyePose(eye).Position * stereoMultiplier;
    }

    // Distance of COI relative to head.
    float distance = centerOfInterest != null ?
        (centerOfInterest.position - transform.position).magnitude : 0;

    // Size of the COI, clamped to [0..distance] for mathematical sanity in following equations.
    float radius = Mathf.Clamp(radiusOfInterest, 0, distance);

    // Move the eye so that COI has about the same size onscreen as in the mono camera FOV.
    // The radius affects the horizon location, which is where the screen-size matching has to
    // occur.
    float scale = proj11 / cam.projectionMatrix[1, 1];  // vertical FOV
    float offset =
        Mathf.Sqrt(radius * radius + (distance * distance - radius * radius) * scale * scale);
    float eyeOffset = (distance - offset) * Mathf.Clamp01(matchMonoFOV) / zScale;

    float ipdScale = stereoMultiplier;
    if (checkStereoComfort) {
      // Manage IPD scale based on the distance to the COI.
      float minComfort = Cardboard.SDK.ComfortableViewingRange.x;
      float maxComfort = Cardboard.SDK.ComfortableViewingRange.y;
      if (minComfort < maxComfort) {  // Sanity check.
        // If closer than the minimum comfort distance, IPD is scaled down.
        // If farther than the maximum comfort distance, IPD is scaled up.
        // The result is that parallax is clamped within a reasonable range.
        float minDistance = (distance - radius) / zScale - eyeOffset;
        ipdScale *= minDistance / Mathf.Clamp(minDistance, minComfort, maxComfort);
      }
    }

    return ipdScale * Cardboard.SDK.EyePose(eye).Position + eyeOffset * Vector3.forward;
  }

  void OnEnable() {
    StartCoroutine("EndOfFrame");
  }

  void OnDisable() {
    StopCoroutine("EndOfFrame");
  }

  void OnPreCull() {
    if (Cardboard.SDK.VRModeEnabled) {
      // Activate the eyes under our control.
      CardboardEye[] eyes = Eyes;
      for (int i = 0, n = eyes.Length; i < n; i++) {
        eyes[i].cam.enabled = true;
      }
      // Turn off the mono camera so it doesn't waste time rendering.  Remember to reenable.
      // @note The mono camera is left on from beginning of frame till now in order that other game
      // logic (e.g. referring to Camera.main) continues to work as expected.
      cam.enabled = false;
      renderedStereo = true;
    } else {
      Cardboard.SDK.UpdateState();
      // Make sure any vertex-distorting shaders don't break completely.
      Shader.SetGlobalMatrix("_RealProjection", cam.projectionMatrix);
      Shader.SetGlobalMatrix("_FixProjection", cam.cameraToWorldMatrix);
      Shader.SetGlobalFloat("_NearClip", cam.nearClipPlane);
    }
  }

  IEnumerator EndOfFrame() {
    while (true) {
      // If *we* turned off the mono cam, turn it back on for next frame.
      if (renderedStereo) {
        cam.enabled = true;
        renderedStereo = false;
      }
      yield return new WaitForEndOfFrame();
    }
  }
}
