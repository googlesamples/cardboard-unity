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

/// @ingroup Scripts
/// Controls one camera of a stereo pair.  Each frame, it mirrors the settings of
/// the parent mono Camera, and then sets up side-by-side stereo with
/// the view and projection matrices from the Cardboard.EyeView and Cardboard.Projection.
/// The render output is directed to the Cardboard.StereoScreen render texture, either
/// to the left half or right half depending on the chosen eye.
///
/// To enable a stereo camera pair, enable the parent mono camera and set
/// Cardboard.vrModeEnabled = true.
///
/// @note If you programmatically change the set of CardboardEyes belonging to a
/// StereoController, be sure to call StereoController::InvalidateEyes on it
/// in order to reset its cache.
[RequireComponent(typeof(Camera))]
[AddComponentMenu("Cardboard/CardboardEye")]
public class CardboardEye : MonoBehaviour {
  /// Whether this is the left eye or the right eye.
  /// Determines which stereo eye to render, that is, which `EyeOffset` and
  /// `Projection` matrix to use and which half of the screen to render to.
  public Cardboard.Eye eye;

  /// Allows you to flip on or off specific culling mask layers for just this
  /// eye.  The mask is a toggle:  The eye's culling mask is first copied from
  /// the parent mono camera, and then the layers specified here are flipped.
  /// Each eye has its own toggle mask.
  [Tooltip("Culling mask layers that this eye should toggle relative to the parent camera.")]
  public LayerMask toggleCullingMask = 0;

  /// The StereoController in charge of this eye (and whose mono camera
  /// we will copy settings from).
  public StereoController Controller {
    // This property is set up to work both in editor and in player.
    get {
      if (transform.parent == null) { // Should not happen.
        return null;
      }
      if ((Application.isEditor && !Application.isPlaying) || controller == null) {
        // Go find our controller.
        return transform.parent.GetComponentInParent<StereoController>();
      }
      return controller;
    }
  }

  /// Returns the closest ancestor CardboardHead.
  /// @note Uses GetComponentInParent(), so the result will be null if no active ancestor is found.
  public CardboardHead Head {
    get {
      return GetComponentInParent<CardboardHead>();
    }
  }

  private StereoController controller;
  private StereoRenderEffect stereoEffect;
  private Camera monoCamera;
  private Matrix4x4 realProj;
  private float interpPosition = 1;

  // Convenient accessor to the camera component used throughout this script.
  public Camera cam { get; private set; }

  void Awake() {
    cam = GetComponent<Camera>();
  }

  void Start() {
    var ctlr = Controller;
    if (ctlr == null) {
      Debug.LogError("CardboardEye must be child of a StereoController.");
      enabled = false;
      return;
    }
    // Save reference to the found controller and it's camera.
    controller = ctlr;
    monoCamera = controller.GetComponent<Camera>();
    UpdateStereoValues();
  }

  private void FixProjection(ref Matrix4x4 proj) {
    // Adjust for non-fullscreen camera.  Cardboard SDK assumes fullscreen,
    // so the aspect ratio might not match.
    proj[0, 0] *= cam.rect.height / cam.rect.width / 2;

    // Cardboard had to pass "nominal" values of near/far to the SDK, which
    // we fix here to match our mono camera's specific values.
    float near = monoCamera.nearClipPlane;
    float far = monoCamera.farClipPlane;
    proj[2, 2] = (near + far) / (near - far);
    proj[2, 3] = 2 * near * far / (near - far);
  }

  private Rect FixViewport(Rect rect) {
    // We are rendering straight to the screen.  Use the reported rect that is visible
    // through the device's lenses.
    Rect view = Cardboard.SDK.Viewport(eye);
    if (eye == Cardboard.Eye.Right) {
      rect.x -= 0.5f;
    }
    rect.width *= 2 * view.width;
    rect.x = view.x + 2 * rect.x * view.width;
    rect.height *= view.height;
    rect.y = view.y + rect.y * view.height;
    if (Application.isEditor) {
      // The Game window's aspect ratio may not match the fake device parameters.
      float realAspect = (float)Screen.width / Screen.height;
      float fakeAspect = Cardboard.SDK.Profile.screen.width / Cardboard.SDK.Profile.screen.height;
      float aspectComparison = fakeAspect / realAspect;
      if (aspectComparison < 1) {
        rect.width *= aspectComparison;
        rect.x *= aspectComparison;
        rect.x += (1 - aspectComparison) / 2;
      } else {
        rect.height /= aspectComparison;
        rect.y /= aspectComparison;
      }
    }
    return rect;
  }

  public void UpdateStereoValues() {
    Matrix4x4 proj = Cardboard.SDK.Projection(eye);
    realProj = Cardboard.SDK.Projection(eye, Cardboard.Distortion.Undistorted);

    CopyCameraAndMakeSideBySide(controller, proj[0, 2], proj[1, 2]);

    // Fix aspect ratio and near/far clipping planes.
    FixProjection(ref proj);
    FixProjection(ref realProj);

    // Zoom the stereo cameras if requested.
    float lerp = Mathf.Clamp01(controller.matchByZoom) * Mathf.Clamp01(controller.matchMonoFOV);
    // Lerping the reciprocal of proj(1,1), so zoom is linear in frustum height not the depth.
    float monoProj11 = monoCamera.projectionMatrix[1, 1];
    float zoom = 1 / Mathf.Lerp(1 / proj[1, 1], 1 / monoProj11, lerp) / proj[1, 1];
    proj[0, 0] *= zoom;
    proj[1, 1] *= zoom;

    // Set the eye camera's projection for rendering.
    cam.projectionMatrix = proj;
    if (Application.isEditor) {
      // So you can see the approximate frustum in the Scene view when the camera is selected.
      cam.fieldOfView = 2 * Mathf.Atan(1 / proj[1, 1]) * Mathf.Rad2Deg;
    }

    // Draw to the mono camera's target, or the stereo screen.
    cam.targetTexture = monoCamera.targetTexture ?? Cardboard.SDK.StereoScreen;
    if (cam.targetTexture == null) {
      // When drawing straight to screen, account for lens FOV limits.
      // Note: do this after all calls to FixProjection() which needs the unfixed rect.
      cam.rect = FixViewport(cam.rect);
    }
  }

  private void SetupStereo() {
    Cardboard.SDK.UpdateState();

    // Will need to update view transform if there is a COI, or if there is a remnant of
    // prior stereo-adjustment smoothing to finish off.
    bool haveCOI = controller.centerOfInterest != null
        && controller.centerOfInterest.gameObject.activeInHierarchy;
    bool updatePosition = haveCOI || interpPosition < 1;

    if (controller.keepStereoUpdated || Cardboard.SDK.ProfileChanged
        || cam.targetTexture == null && Cardboard.SDK.StereoScreen != null) {
      // Set projection and viewport.
      UpdateStereoValues();
      // Also view transform.
      updatePosition = true;
    }

    if (updatePosition) {
      // Set view transform.
      float proj11 = cam.projectionMatrix[1, 1];
      float zScale = transform.lossyScale.z;
      Vector3 eyePos = controller.ComputeStereoEyePosition(eye, proj11, zScale);
      // Apply smoothing only if updating position every frame.
      interpPosition = controller.keepStereoUpdated || haveCOI ?
          Time.deltaTime / (controller.stereoAdjustSmoothing + Time.deltaTime) : 1;
      transform.localPosition = Vector3.Lerp(transform.localPosition, eyePos, interpPosition);
    }

    // Pass necessary information to any shaders doing distortion correction.
    if (Cardboard.SDK.DistortionCorrection == Cardboard.DistortionCorrectionMethod.None) {
      // Correction matrix for use in surface shaders that do vertex warping for distortion.
      // Have to compute it every frame because cameraToWorldMatrix is changing constantly.
      var fixProj = cam.cameraToWorldMatrix *
                    Matrix4x4.Inverse(cam.projectionMatrix) *
                    realProj;
      Shader.SetGlobalMatrix("_RealProjection", realProj);
      Shader.SetGlobalMatrix("_FixProjection", fixProj);
    } else {
      // Not doing vertex-based distortion.  Set up the parameters to make any vertex-warping
      // shaders in the project leave vertexes alone.
      Shader.SetGlobalMatrix("_RealProjection", cam.projectionMatrix);
      Shader.SetGlobalMatrix("_FixProjection", cam.cameraToWorldMatrix);
    }
    Shader.SetGlobalFloat("_NearClip", cam.nearClipPlane);
  }

  void OnPreCull() {
    if (!Cardboard.SDK.VRModeEnabled || !monoCamera.enabled) {
      // Keep stereo enabled flag in sync with parent mono camera.
      cam.enabled = false;
      return;
    }
    SetupStereo();
    if (!controller.directRender && Cardboard.SDK.StereoScreen != null) {
      // Some image effects clobber the whole screen.  Add a final image effect to the chain
      // which restores side-by-side stereo.
      stereoEffect = GetComponent<StereoRenderEffect>();
      if (stereoEffect == null) {
        stereoEffect = gameObject.AddComponent<StereoRenderEffect>();
      }
      stereoEffect.enabled = true;
    } else if (stereoEffect != null) {
      // Don't need the side-by-side image effect.
      stereoEffect.enabled = false;
    }
  }

  /// Helper to copy camera settings from the controller's mono camera.  Used in SetupStereo() and
  /// in the custom editor for StereoController.  The parameters parx and pary, if not left at
  /// default, should come from a projection matrix returned by the SDK.  They affect the apparent
  /// depth of the camera's window.  See SetupStereo().
  public void CopyCameraAndMakeSideBySide(StereoController controller,
                                          float parx = 0, float pary = 0) {
#if UNITY_EDITOR
    // Member variable 'cam' not always initialized when this method called in Editor.
    // So, we'll just make a local of the same name.
    var cam = GetComponent<Camera>();
#endif
    // Same for controller's camera, but it can happen at runtime too (via AddStereoRig on
    // StereoController).
    var monoCamera =
        controller == this.controller ? this.monoCamera : controller.GetComponent<Camera>();

    float ipd = CardboardProfile.Default.device.lenses.separation * controller.stereoMultiplier;
    Vector3 localPosition = Application.isPlaying ?
        transform.localPosition : (eye == Cardboard.Eye.Left ? -ipd/2 : ipd/2) * Vector3.right;;

    // Sync the camera properties.
    cam.CopyFrom(monoCamera);
    cam.cullingMask ^= toggleCullingMask.value;

    // Not sure why we have to do this, but if we don't then switching between drawing to
    // the main screen or to the stereo rendertexture acts very strangely.
    cam.depth = monoCamera.depth;

    // Reset transform, which was clobbered by the CopyFrom() call.
    // Since we are a child of the mono camera, we inherit its transform already.
    transform.localPosition = localPosition;
    transform.localRotation = Quaternion.identity;
    transform.localScale = Vector3.one;

    Skybox monoCameraSkybox = monoCamera.GetComponent<Skybox>();
    Skybox customSkybox = GetComponent<Skybox>();
    if(monoCameraSkybox != null) {
      if (customSkybox == null) {
        customSkybox = gameObject.AddComponent<Skybox>();
      }
      customSkybox.material = monoCameraSkybox.material;
    } else if (customSkybox != null) {
      Destroy(customSkybox);
    }

    // Set up side-by-side stereo.
    // Note: The code is written this way so that non-fullscreen cameras
    // (PIP: picture-in-picture) still work in stereo.  Even if the PIP's content is
    // not going to be in stereo, the PIP itself still has to be rendered in both eyes.
    Rect rect = cam.rect;

    // Move away from edges if padding requested.  Some HMDs make the edges of the
    // screen a bit hard to see.
    Vector2 center = rect.center;
    center.x = Mathf.Lerp(center.x, 0.5f, Mathf.Clamp01(controller.stereoPaddingX));
    center.y = Mathf.Lerp(center.y, 0.5f, Mathf.Clamp01(controller.stereoPaddingY));
    rect.center = center;

    // Semi-hacky aspect ratio adjustment because the screen is only half as wide due
    // to side-by-side stereo, to make sure the PIP width fits.
    float width = Mathf.SmoothStep(-0.5f, 0.5f, (rect.width + 1) / 2);
    rect.x += (rect.width - width) / 2;
    rect.width = width;

    // Divide the outside region of window proportionally in each half of the screen.
    rect.x *= (0.5f - rect.width) / (1 - rect.width);
    if (eye == Cardboard.Eye.Right) {
      rect.x += 0.5f; // Move to right half of the screen.
    }

    // Adjust the window for requested parallax.  This affects the apparent depth of the
    // window in the main camera's screen.  Useful for PIP windows only, where rect.width < 1.
    float parallax = Mathf.Clamp01(controller.screenParallax);
    if (monoCamera.rect.width < 1 && parallax > 0) {
      // Note: parx and pary are signed, with opposite signs in each eye.
      rect.x -= parx / 4 * parallax; // Extra factor of 1/2 because of side-by-side stereo.
      rect.y -= pary / 2 * parallax;
    }

    cam.rect = rect;
  }
}
