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
using System.Reflection;

/// @ingroup Scripts
/// Controls one camera of a stereo pair.  Each frame, it mirrors the settings of
/// the parent mono Camera, and then sets up side-by-side stereo with
/// the view and projection matrices from the Cardboard.EyeView and Cardboard.Projection.
/// The render output is directed to the Cardboard.StereoScreen render texture, either
/// to the left half or right half depending on the chosen eye.
///
/// To enable a stereo camera pair, enable the parent mono camera and set
/// Cardboard.SDK.vrModeEnabled = true.
///
/// @note If you programmatically change the set of CardboardEyes belonging to a
/// StereoController, be sure to call StereoController::InvalidateEyes on it
/// in order to reset its cache.
[RequireComponent(typeof(Camera))]
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
  private StereoController controller;
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
  public CardboardHead Head {
    get {
      return GetComponentInParent<CardboardHead>();
    }
  }

#if UNITY_5
  // For backwards source code compatibility, since we refer to the camera component A LOT in
  // this script.
  new private Camera camera;

  void Awake() {
    camera = GetComponent<Camera>();
  }
#endif

  void Start() {
    var ctlr = Controller;
    if (ctlr == null) {
      Debug.LogError("CardboardEye must be child of a StereoController.");
      enabled = false;
    }
    // Save reference to the found controller.
    controller = ctlr;
    // Add an image effect when playing in the editor to preview the distortion correction, since
    // native distortion corrrection is only available on the phone.
    if (Application.isPlaying && !Cardboard.SDK.NativeDistortionCorrectionSupported
        && SystemInfo.supportsRenderTextures) {
      var effect = GetComponent<RadialUndistortionEffect>();
      if (effect == null) {
        effect = gameObject.AddComponent<RadialUndistortionEffect>();
      }
    }
  }

  private void FixProjection(ref Matrix4x4 proj, float near, float far, float ipdScale) {
    // Adjust for non-fullscreen camera.  Cardboard SDK assumes fullscreen,
    // so the aspect ratio might not match.
    float aspectFix = camera.rect.height / camera.rect.width / 2;
    proj[0, 0] *= aspectFix;

    // Adjust for IPD scale.  This changes the vergence of the two frustums.
    Vector2 dir = transform.localPosition; // ignore Z
    dir = dir.normalized * ipdScale;
    proj[0, 2] *= Mathf.Abs(dir.x);
    proj[1, 2] *= Mathf.Abs(dir.y);

    // Cardboard had to pass "nominal" values of near/far to the SDK, which
    // we fix here to match our mono camera's specific values.
    proj[2, 2] = (near + far) / (near - far);
    proj[2, 3] = 2 * near * far / (near - far);
  }

  private void Setup() {
    // Shouldn't happen because of the check in Start(), but just in case...
    if (Controller == null) {
      return;
    }

    var monoCamera = Controller.GetComponent<Camera>();
    Matrix4x4 proj = Cardboard.SDK.Projection(eye);

    CopyCameraAndMakeSideBySide(Controller, proj[0,2], proj[1,2]);

    // Zoom the stereo cameras if requested.
    float lerp = Mathf.Clamp01(Controller.matchByZoom) * Mathf.Clamp01(Controller.matchMonoFOV);
    // Lerping the reciprocal of proj(1,1) so zooming is linear in the frustum width, not the depth.
    float monoProj11 = monoCamera.projectionMatrix[1, 1];
    float zoom = 1 / Mathf.Lerp(1 / proj[1, 1], 1 / monoProj11, lerp) / proj[1, 1];
    proj[0, 0] *= zoom;
    proj[1, 1] *= zoom;

    // Calculate stereo adjustments based on the center of interest.
    float ipdScale;
    float eyeOffset;
    Controller.ComputeStereoAdjustment(proj[1, 1], transform.lossyScale.z,
                                       out ipdScale, out eyeOffset);

    // Set up the eye's view transform.
    transform.localPosition = ipdScale * Cardboard.SDK.EyePose(eye).Position +
                              eyeOffset * Vector3.forward;

    // Set up the eye's projection.
    float near = monoCamera.nearClipPlane;
    float far = monoCamera.farClipPlane;
    FixProjection(ref proj, near, far, ipdScale);
    camera.projectionMatrix = proj;

    if (Application.isEditor) {
      // So you can see the approximate frustum in the Scene view when the camera is selected.
      camera.fieldOfView = 2 * Mathf.Atan(1 / proj[1, 1]) * Mathf.Rad2Deg;
    }

    // Set up variables for an image effect that will do distortion correction, e.g. the
    // RadialDistortionEffect.  Note that native distortion correction should take precedence
    // over an image effect, so if that is active then we don't need to compute these variables.
    // (Exception: we're in the editor, so we use the image effect to preview the distortion
    // correction, because native distortion correction only works on the phone.)
    if (Cardboard.SDK.UseDistortionEffect) {
      Matrix4x4 realProj = Cardboard.SDK.Projection(eye, Cardboard.Distortion.Undistorted);
      FixProjection(ref realProj, near, far, ipdScale);
      // Parts of the projection matrices that we need to convert texture coordinates between
      // distorted and undistorted frustums.  Include the transform between texture space [0..1]
      // and NDC [-1..1] (that's what the -1 and the /2 are for).  Also note that the zoom
      // factor is removed, because that will interfere with the distortion calculation.
      Vector4 projvec = new Vector4(proj[0, 0] / zoom, proj[1, 1] / zoom,
                                    proj[0, 2] - 1, proj[1, 2] - 1) / 2;
      Vector4 unprojvec = new Vector4(realProj[0, 0], realProj[1, 1],
                                      realProj[0, 2] - 1, realProj[1, 2] - 1) / 2;
      Shader.SetGlobalVector("_Projection", projvec);
      Shader.SetGlobalVector("_Unprojection", unprojvec);
      CardboardProfile p = Cardboard.SDK.Profile;
      Shader.SetGlobalVector("_Undistortion",
                             new Vector4(p.device.inverse.k1, p.device.inverse.k2));
      Shader.SetGlobalVector("_Distortion",
                             new Vector4(p.device.distortion.k1, p.device.distortion.k2));
    }

    if (Controller.StereoScreen == null) {
      Rect rect = camera.rect;
      if (!Cardboard.SDK.DistortionCorrection
          || Cardboard.SDK.UseDistortionEffect) {
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
      }
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
        }
      }
      camera.rect = rect;
    }
  }

  /// Called by StereoController to run the whole render pipeline for this camera.
  public void Render() {
    Setup();

    // Use the "fast" or "slow" method.  Fast means the camera draws right into one half of
    // the stereo screen.  Slow means it draws first to a side buffer, and then the buffer
    // is written to the screen. The slow method is provided because a lot of Image Effects
    // don't work if you draw to only part of the window.
    if (Controller.directRender) {
      // Redirect to our stereo screen.
      camera.targetTexture = Controller.StereoScreen;
      // Draw!
      camera.Render();
    } else {
      // Save the viewport rectangle and reset to "full screen".
      Rect pixRect = camera.pixelRect;
      camera.rect = new Rect (0, 0, 1, 1);
      // Redirect to a temporary texture.  The defaults are supposedly Android-friendly.
      RenderTexture stereoScreen = controller.StereoScreen;
      int screenWidth = stereoScreen ? stereoScreen.width : Screen.width;
      int screenHeight = stereoScreen ? stereoScreen.height : Screen.height;
      int depth = stereoScreen ? stereoScreen.depth : 16;
      RenderTextureFormat format = stereoScreen ? stereoScreen.format : RenderTextureFormat.RGB565;
      camera.targetTexture = RenderTexture.GetTemporary((int)pixRect.width, (int)pixRect.height,
                                                        depth, format);
      // Draw!
      camera.Render();
      // Blit the temp texture to the stereo screen.
      RenderTexture oldTarget = RenderTexture.active;
      RenderTexture.active = stereoScreen;
      GL.PushMatrix();
      GL.LoadPixelMatrix(0, screenWidth, screenHeight, 0);
      // Camera rects are in screen coordinates (bottom left is origin), but DrawTexture takes a
      // rect in GUI coordinates (top left is origin).
      Rect blitRect = pixRect;
      blitRect.y = screenHeight - pixRect.height - pixRect.y;
      // Blit!
      Graphics.DrawTexture(blitRect, camera.targetTexture);
      // Clean up.
      GL.PopMatrix();
      RenderTexture.active = oldTarget;
      RenderTexture.ReleaseTemporary(camera.targetTexture);
    }
    camera.targetTexture = null;
  }

  /// Alternate means of rendering stereo, when you don't plan to switch in and out of VR mode:
  /// In the editor, disable the MainCamera's camera component.  Enable the two stereo eye
  /// camera components.
  /// @note due to a quirk of Unity, there must be at least one camera not rendering to a texture,
  /// preferably last in order.  If necessary, add a dummy camera to the scene with a high depth
  /// value, it's clear flags set to "Don't Clear", and culling mask set to Nothing.
  void OnPreCull() {
    if (camera.enabled) {
      Setup();
      camera.targetTexture = Controller.StereoScreen;
    }
  }

  /// Helper to copy camera settings from the controller's mono camera.
  /// Used in OnPreCull and the custom editor for StereoController.
  /// The parameters parx and pary, if not left at default, should come from a
  /// projection matrix returned by the SDK.
  /// They affect the apparent depth of the camera's window.  See OnPreCull().
  public void CopyCameraAndMakeSideBySide(StereoController controller,
                                          float parx = 0, float pary = 0) {
#if UNITY_5
#if UNITY_EDITOR
    // Member variable 'camera' not always initialized when this method called in Editor.
    // So, we'll just make a local of the same name.
    var camera = GetComponent<Camera>();
#endif
#endif

    // Sync the camera properties.
    camera.CopyFrom(controller.GetComponent<Camera>());
    camera.cullingMask ^= toggleCullingMask.value;

    // Reset transform, which was clobbered by the CopyFrom() call.
    // Since we are a child of the mono camera, we inherit its
    // transform already.
    // Use nominal IPD for the editor.  During play, OnPreCull() will
    // compute a real value.
    float ipd = CardboardProfile.Default.device.lenses.separation * controller.stereoMultiplier;
    transform.localPosition = (eye == Cardboard.Eye.Left ? -ipd/2 : ipd/2) * Vector3.right;
    transform.localRotation = Quaternion.identity;
    transform.localScale = Vector3.one;

    // Set up side-by-side stereo.
    // Note: The code is written this way so that non-fullscreen cameras
    // (PIP: picture-in-picture) still work in stereo.  Even if the PIP's content is
    // not going to be in stereo, the PIP itself still has to be rendered in both eyes.
    Rect rect = camera.rect;

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
    if (controller.GetComponent<Camera>().rect.width < 1 && parallax > 0) {
      // Note: parx and pary are signed, with opposite signs in each eye.
      rect.x -= parx / 4 * parallax; // Extra factor of 1/2 because of side-by-side stereo.
      rect.y -= pary / 2 * parallax;
    }

    camera.rect = rect;
  }
}
