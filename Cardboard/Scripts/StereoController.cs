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

// Controls a pair of CardboardEye objects that will render the stereo view
// of the camera this script is attached to.
[RequireComponent(typeof(Camera))]
public class StereoController : MonoBehaviour {
  [Tooltip("Whether to draw directly to the output window (true), or " +
           "to an offscreen buffer first and then blit (false).  Image " +
           " Effects and Deferred Lighting may only work if set to false.")]
  public bool directRender = true;

  // Adjusts the level of stereopsis for this stereo rig.  Note that this
  // parameter is not the virtual size of the head -- use a scale on the head
  // game object for that.  Instead, it is a control on eye vergence, or
  // rather, how cross-eyed or not the stereo rig is.  Set to 0 to turn
  // off stereo in this rig independently of any others.
  [Tooltip("Set the stereo level for this camera.")]
  [Range(0,1)]
  public float stereoMultiplier = 1.0f;

  // The stereo cameras by default use the actual optical FOV of the Cardboard device,
  // because otherwise the match between head motion and scene motion is broken, which
  // impacts the virtual reality effect.  However, in some cases it is desirable to
  // adjust the FOV anyway, for special effects or artistic reasons.  But in no case
  // should the FOV be allowed to remain very different from the true optical FOV for
  // very long, or users will experience discomfort.
  //
  // This value determines how much to match the mono camera's field of view.  This is
  // a fraction: 0 means no matching, 1 means full matching, and values in between are
  // compromises.  Reasons for not matching 100% would include preserving some VR-ness,
  // and that due to the lens distortion the edges of the view are not as easily seen as
  // when the phone is not in VR-mode.
  //
  // Another use for this variable is to preserve scene composition against differences
  // in the optical FOV of various Cardboard models.  In all cases, this value simply
  // lets the mono camera have some control over the scene in VR mode, like it does in
  // non-VR mode.
  [Tooltip("How much to adjust the stereo field of view to match this camera.")]
  [Range(0,1)]
  public float matchMonoFOV = 0;

  // Determines the method by which the stereo cameras' FOVs are matched to the mono
  // camera's FOV (assuming matchMonoFOV is not 0).  The default is to move the stereo
  // cameras (matchByZoom = 0), with the option to instead do a simple camera zoom
  // (matchByZoom = 1).  In-between values yield a mix of the two behaviors.
  //
  // It is not recommended to use simple zooming for typical scene composition, as it
  // conflicts with the VR need to match the user's head motion with the corresponding
  // scene motion.  This should be reserved for special effects such as when the player
  // views the scene through a telescope or other magnifier (and thus the player knows
  // that VR is going to be affected), or similar situations.
  //
  // Note that matching by moving the eyes requires that the centerOfInterest object
  // be non-null, or there will be no effect.
  [Tooltip("Whether to adjust FOV by moving the eyes (0) or simply zooming (1).")]
  [Range(0,1)]
  public float matchByZoom = 0;

  // Matching the mono camera's field of view in stereo by moving the eyes requires
  // a designated "center of interest".  This is either a point in space (an empty
  // gameobject) you place in the scene as a sort of "3D cursor", or an actual scene
  // entity which the player is likely to be focussed on.
  //
  // The FOV adjustment is done by moving the eyes toward or away from the COI
  // so that it appears to have the same size on screen as it would in the mono
  // camera.  This is disabled if the COI is null.
  [Tooltip("Object or point where field of view matching is done.")]
  public Transform centerOfInterest;

  // The COI is generally meant to be just a point in space, like a 3D cursor.
  // Occasionally, you will want it to be an actual object with size.  Set this
  // to the approximate radius of the object to help the FOV-matching code
  // compensate for the object's horizon when it is close to the camera.
  [Tooltip("If COI is an object, its approximate size.")]
  public float radiusOfInterest = 0;

  // If true, check that the centerOfInterest is between the min and max comfortable
  // viewing distances (see Cardboard.cs), or else adjust the stereo multiplier to
  // compensate.  If the COI has a radius, then the near side is checked.  COI must
  // be non-null for this setting to have any effect.
  [Tooltip("Adjust stereo level when COI gets too close or too far.")]
  public bool checkStereoComfort = true;

  // For picture-in-picture cameras that don't fill the entire screen,
  // set the virtual depth of the window itself.  A value of 0 means
  // zero parallax, which is fairly close.  A value of 1 means "full"
  // parallax, which is equal to the interpupillary distance and equates
  // to an infinitely distant window.  This does not affect the actual
  // screen size of the the window (in pixels), only the stereo separation
  // of the left and right images.
  [Tooltip("Adjust the virtual depth of this camera's window (picture-in-picture only).")]
  [Range(0,1)]
  public float screenParallax = 0;

  // For picture-in-picture cameras, move the window away from the edges
  // in VR Mode to make it easier to see.  The optics of HMDs make the screen
  // edges hard to see sometimes, so you can use this to keep the PIP visible
  // whether in VR Mode or not.  The x value is the fraction of the screen along
  // either side to pad, and the y value is for the top and bottom of the screen.
  [Tooltip("Move the camera window horizontally towards the center of the screen (PIP only).")]
  [Range(0,1)]
  public float stereoPaddingX = 0;

  [Tooltip("Move the camera window vertically towards the center of the screen (PIP only).")]
  [Range(0,1)]
  public float stereoPaddingY = 0;

  // Flags whether we rendered in stereo for this frame.
  private bool renderedStereo = false;

  // Returns the CardboardEye components that we control.
  public CardboardEye[] Eyes {
    get {
      return GetComponentsInChildren<CardboardEye>(true)
             .Where(eye => eye.Controller == this)
             .ToArray();
    }
  }

  // Returns the nearest CardboardHead that affects our eyes.
  public CardboardHead Head {
    get {
      return Eyes.Select(eye => eye.Head).FirstOrDefault();
    }
  }

  // Where the stereo eyes will render the scene.
  public RenderTexture StereoScreen {
    get {
      return GetComponent<Camera>().targetTexture ?? Cardboard.SDK.StereoScreen;
    }
  }

  void Awake() {
    AddStereoRig();
  }

  // Helper routine for creation of a stereo rig.  Used by the
  // custom editor for this class, or to build the rig at runtime.
  public void AddStereoRig() {
    if (Eyes.Length > 0) {  // Simplistic test if rig already exists.
      return;
    }
    CreateEye(Cardboard.Eye.Left);
    CreateEye(Cardboard.Eye.Right);
    if (Head == null) {
      gameObject.AddComponent<CardboardHead>();
    }
    if (GetComponent<Camera>().tag == "MainCamera" && GetComponent<SkyboxMesh>() == null) {
      gameObject.AddComponent<SkyboxMesh>();
    }
  }

  // Helper routine for creation of a stereo eye.
  private void CreateEye(Cardboard.Eye eye) {
    string nm = name + (eye == Cardboard.Eye.Left ? " Left" : " Right");
    GameObject go = new GameObject(nm);
    go.transform.parent = transform;
    go.AddComponent<Camera>().enabled = false;
    if (GetComponent<GUILayer>() != null) {
      go.AddComponent<GUILayer>();
    }
    if (GetComponent("FlareLayer") != null) {
      go.AddComponent<FlareLayer>();
    }
    var cardboardEye = go.AddComponent<CardboardEye>();
    cardboardEye.eye = eye;
    cardboardEye.CopyCameraAndMakeSideBySide(this);
  }

  // Given information about a specific camera (usually one of the stereo eyes),
  // computes an adjustment to the stereo settings for both FOV matching and
  // stereo comfort.  The input is the [1,1] entry of the camera's projection
  // matrix, representing the vertical field of view, and the overall scale
  // being applied to the Z axis.  The output is a multiplier of the IPD to
  // use for offseting the eyes laterally, and an offset in the eye's Z direction
  // to account for the FOV difference.  The eye offset is in local coordinates.
  public void ComputeStereoAdjustment(float proj11, float zScale,
                                      out float ipdScale, out float eyeOffset) {
    ipdScale = stereoMultiplier;
    eyeOffset = 0;
    if (centerOfInterest == null || !centerOfInterest.gameObject.activeInHierarchy) {
      return;
    }

    // Distance of COI relative to head.
    float distance = (centerOfInterest.position - transform.position).magnitude;

    // Size of the COI, clamped to [0..distance] for mathematical sanity in following equations.
    float radius = Mathf.Clamp(radiusOfInterest, 0, distance);

    // Move the eye so that COI has about the same size onscreen as in the mono camera FOV.
    // The radius affects the horizon location, which is where the screen-size matching has to
    // occur.
    float scale = proj11 / GetComponent<Camera>().projectionMatrix[1, 1];  // vertical FOV
    float offset =
        Mathf.Sqrt(radius * radius + (distance * distance - radius * radius) * scale * scale);
    eyeOffset = (distance - offset) * Mathf.Clamp01(matchMonoFOV) / zScale;

    // Manage IPD scale based on the distance to the COI.
    if (checkStereoComfort) {
      float minComfort = Cardboard.SDK.MinimumComfortDistance;
      float maxComfort = Cardboard.SDK.MaximumComfortDistance;
      if (minComfort < maxComfort) {  // Sanity check.
        // If closer than the minimum comfort distance, IPD is scaled down.
        // If farther than the maximum comfort distance, IPD is scaled up.
        // The result is that parallax is clamped within a reasonable range.
        float minDistance = (distance - radius) / zScale - eyeOffset;
        ipdScale *= minDistance / Mathf.Clamp(minDistance, minComfort, maxComfort);
      }
    }
  }

  void Start() {
    StartCoroutine("EndOfFrame");
  }

  void OnPreCull() {
    if (!Cardboard.SDK.VRModeEnabled || !Cardboard.SDK.UpdateState()) {
      // Nothing to do.
      return;
    }

    // Turn off the mono camera so it doesn't waste time rendering.
    // Note: mono camera is left on from beginning of frame till now
    // in order that other game logic (e.g. Camera.main) continues
    // to work as expected.
    GetComponent<Camera>().enabled = false;

    // Render the eyes under our control.
    foreach (var eye in Eyes) {
      eye.Render();
    }

    // Remember to reenable
    renderedStereo = true;
  }

  IEnumerator EndOfFrame() {
    while (true) {
      // If *we* turned off the mono cam, turn it back on for next frame.
      if (renderedStereo) {
        GetComponent<Camera>().enabled = true;
        renderedStereo = false;
      }
      yield return new WaitForEndOfFrame();
    }
  }

  void OnDestroy() {
    StopCoroutine("EndOfFrame");
  }
}
