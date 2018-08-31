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
using System.Runtime.InteropServices;
using System;
using System.Text;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace Gvr.Internal {
  [HelpURL("https://developers.google.com/vr/unity/reference/class/InstantPreview")]
  public class InstantPreview : MonoBehaviour {
    private const string NoDevicesFoundAdbResult = "error: no devices/emulators found";

    internal static InstantPreview Instance { get; set; }

    internal const string dllName = "instant_preview_unity_plugin";

    public enum Resolutions : int {
      Big,
      Regular,
      WindowSized,
    }
    struct ResolutionSize {
      public int width;
      public int height;
    }

    [Tooltip("Resolution of video stream. Higher = more expensive / better visual quality.")]
    public Resolutions OutputResolution = Resolutions.Big;

    public enum MultisampleCounts {
      One,
      Two,
      Four,
      Eight,
    }

    [Tooltip("Anti-aliasing for video preview. Higher = more expensive / better visual quality.")]
    public MultisampleCounts MultisampleCount = MultisampleCounts.One;

    public enum BitRates {
      _2000,
      _4000,
      _8000,
      _16000,
      _24000,
      _32000,
    }

    [Tooltip("Video codec streaming bit rate. Higher = more expensive / better visual quality.")]
    public BitRates BitRate = BitRates._16000;

    [Tooltip("Installs the Instant Preview app if it isn't found on the connected device.")]
    public bool InstallApkOnRun = true;

    public UnityEngine.Object InstantPreviewApk;

    struct UnityRect {
      public float right;
      public float left;
      public float top;
      public float bottom;
    }

    struct UnityEyeViews {
      public Matrix4x4 leftEyePose;
      public Matrix4x4 rightEyePose;
      public UnityRect leftEyeViewSize;
      public UnityRect rightEyeViewSize;
    }

#if UNITY_HAS_GOOGLEVR && UNITY_EDITOR
    static ResolutionSize[] resolutionSizes = new ResolutionSize[] {
      new ResolutionSize() { width = 2560, height = 1440, },  // ResolutionSize.Big
      new ResolutionSize() { width = 1920, height = 1080, },  // ResolutionSize.Regular
      new ResolutionSize() ,                                  // ResolutionSize.WindowSized
    };

    private static readonly int[] multisampleCounts = new int[] {
      1,  // MultisampleCounts.One
      2,  // MultisampleCounts.Two
      4,  // MultisampleCounts.Four
      8,  // MultisampleCounts.Eight
    };

    private static readonly int[] bitRates = new int[] {
      2000,   // BitRates._2000
      4000,   // BitRates._4000
      8000,   // BitRates._8000
      16000,  // BitRates._16000
      24000,  // BitRates._24000
      32000,  // BitRates._32000
    };

    [DllImport(dllName)]
    private static extern bool IsConnected();

    [DllImport(dllName)]
    private static extern bool GetHeadPose(out Matrix4x4 pose, out double timestamp);

    [DllImport(dllName)]
    private static extern bool GetEyeViews(out UnityEyeViews outputEyeViews);

    [DllImport(dllName)]
    private static extern IntPtr GetRenderEventFunc();

    [DllImport(dllName)]
    private static extern void SendFrame(IntPtr renderTexture, ref Matrix4x4 pose, double timestamp, int bitRate);

    [DllImport(dllName)]
    private static extern void GetVersionString(StringBuilder dest, uint n);

    public bool IsCurrentlyConnected { get { return connected; } }

    private IntPtr renderEventFunc;
    private RenderTexture renderTexture;
    private Matrix4x4 headPose = Matrix4x4.identity;
    private double timestamp;

    private class EyeCamera {
      public Camera leftEyeCamera = null;
      public Camera rightEyeCamera = null;
    }
    Dictionary<Camera, EyeCamera> eyeCameras = new Dictionary<Camera, EyeCamera>();

    List<Camera> camerasLastFrame = new List<Camera>();

    private bool connected;

    void Awake() {
      renderEventFunc = GetRenderEventFunc();

      if (Instance != null) {
        Destroy(gameObject);
        gameObject.SetActive(false);
        return;
      }

      Instance = this;
      DontDestroyOnLoad(gameObject);
    }

    void Start() {
      // Gets local version name and prints it out.
      var sb = new StringBuilder(256);
      GetVersionString(sb, (uint)sb.Capacity);
      var localVersionName = sb.ToString();
      Debug.Log("Instant Preview Version: " + localVersionName);

      // Tries to install Instant Preview apk if set to do so.
      if (InstallApkOnRun) {
        // Early outs if set to install but the apk can't be found.
        if (InstantPreviewApk == null) {
          Debug.LogError("Trying to install Instant Preview apk but reference to InstantPreview.apk is broken.");
          return;
        }

        // Gets the apk path and installs it on a separate thread.
        var apkPath = Path.GetFullPath(UnityEditor.AssetDatabase.GetAssetPath(InstantPreviewApk));
        if (File.Exists(apkPath)) {
          new Thread(() => {
            string output;
            string errors;

            // Gets version of installed apk.
            RunCommand(InstantPreviewHelper.AdbPath,
                       "shell dumpsys package com.google.instantpreview | grep versionName",
                       out output, out errors);
            string installedVersionName = null;
            if (!string.IsNullOrEmpty(output) && string.IsNullOrEmpty(errors)) {
              installedVersionName = output.Substring(output.IndexOf('=') + 1);
            }

            // Early outs if no device is connected.
            if (string.Compare(errors, NoDevicesFoundAdbResult) == 0) {
              return;
            }

            // Prints errors and exits on failure.
            if (!string.IsNullOrEmpty(errors)) {
              Debug.LogError(errors);
              return;
            }

            // Determines if app is installed.
            if (installedVersionName != localVersionName) {
              if (installedVersionName == null) {
                Debug.Log(string.Format(
                  "Instant Preview: app not found on device, attempting to install it from {0}.",
                  apkPath));
              } else {
                Debug.Log(string.Format(
                  "Instant Preview: installed version \"{0}\" does not match local version \"{1}\", attempting upgrade.",
                  installedVersionName, localVersionName));
              }

              RunCommand(InstantPreviewHelper.AdbPath,
                         string.Format("uninstall com.google.instantpreview", apkPath),
                         out output, out errors);

              RunCommand(InstantPreviewHelper.AdbPath,
                         string.Format("install \"{0}\"", apkPath),
                         out output, out errors);

              // Prints any output from trying to install.
              if (!string.IsNullOrEmpty(output)) {
                Debug.Log(output);
              }
              if (!string.IsNullOrEmpty(errors)) {
                if (string.Equals(errors.Trim(), "Success")) {
                  Debug.Log("Successfully installed Instant Preview app.");
                } else {
                  Debug.LogError(errors);
                }
              }
            }

            StartInstantPreviewActivity(InstantPreviewHelper.AdbPath);
          }).Start();
        }
      } else {
        new Thread(() => { StartInstantPreviewActivity(InstantPreviewHelper.AdbPath); }).Start();
      }
    }

    void UpdateCamera(Camera camera) {

      EyeCamera eyeCamera;

      if (!eyeCameras.TryGetValue(camera, out eyeCamera)) {
        return;
      }

      if (connected) {
        if (GetHeadPose(out headPose, out timestamp)) {
          SetEditorEmulatorsEnabled(false);
          camera.transform.localRotation = Quaternion.LookRotation(headPose.GetColumn(2), headPose.GetColumn(1));
          camera.transform.localPosition = camera.transform.localRotation * headPose.GetRow(3) * -1;
        } else {
          SetEditorEmulatorsEnabled(true);
        }

        var eyeViews = new UnityEyeViews();
        if (GetEyeViews(out eyeViews)) {
          SetTransformFromMatrix(eyeCamera.leftEyeCamera.gameObject.transform, eyeViews.leftEyePose);
          SetTransformFromMatrix(eyeCamera.rightEyeCamera.gameObject.transform, eyeViews.rightEyePose);

          var near = Camera.main.nearClipPlane;
          var far = Camera.main.farClipPlane;
          eyeCamera.leftEyeCamera.projectionMatrix =
            PerspectiveMatrixFromUnityRect(eyeViews.leftEyeViewSize, near, far);
          eyeCamera.rightEyeCamera.projectionMatrix =
            PerspectiveMatrixFromUnityRect(eyeViews.rightEyeViewSize, near, far);

          bool multisampleChanged = multisampleCounts[(int)MultisampleCount] != renderTexture.antiAliasing;

          // Adjusts render texture size.
          if (OutputResolution != Resolutions.WindowSized) {
            var selectedResolutionSize = resolutionSizes[(int)OutputResolution];
            if (selectedResolutionSize.width != renderTexture.width ||
              selectedResolutionSize.height != renderTexture.height ||
              multisampleChanged) {
              ResizeRenderTexture(selectedResolutionSize.width, selectedResolutionSize.height);
            }
          } else { // OutputResolution == Resolutions.WindowSized
            var screenAspectRatio = (float)Screen.width / Screen.height;

            var eyeViewsWidth =
              -eyeViews.leftEyeViewSize.left +
              eyeViews.leftEyeViewSize.right +
              -eyeViews.rightEyeViewSize.left +
              eyeViews.rightEyeViewSize.right;
            var eyeViewsHeight =
              eyeViews.leftEyeViewSize.top +
              -eyeViews.leftEyeViewSize.bottom;
            if (eyeViewsHeight > 0f) {
              int renderTextureHeight;
              int renderTextureWidth;
              var eyeViewsAspectRatio = eyeViewsWidth / eyeViewsHeight;
              if (screenAspectRatio > eyeViewsAspectRatio) {
                renderTextureHeight = Screen.height;
                renderTextureWidth = (int)(Screen.height * eyeViewsAspectRatio);
              } else {
                renderTextureWidth = Screen.width;
                renderTextureHeight = (int)(Screen.width / eyeViewsAspectRatio);
              }
              renderTextureWidth = renderTextureWidth & ~0x3;
              renderTextureHeight = renderTextureHeight & ~0x3;

              if (multisampleChanged ||
                renderTexture.width != renderTextureWidth ||
                renderTexture.height != renderTextureHeight) {
                ResizeRenderTexture(renderTextureWidth, renderTextureHeight);
              }
            }
          }
        }
      } else { // !connected
        SetEditorEmulatorsEnabled(true);

        if (renderTexture.width != Screen.width || renderTexture.height != Screen.height) {
          ResizeRenderTexture(Screen.width, Screen.height);
        }
      }
    }

    void Update() {
      if (!EnsureCameras()) {
        return;
      }

      var newConnectionState = IsConnected();
      if (connected && !newConnectionState) {
        Debug.Log("Disconnected from Instant Preview.");
      } else if (!connected && newConnectionState) {
        Debug.Log("Connected to Instant Preview.");
      }
      connected = newConnectionState;

      foreach (KeyValuePair<Camera, EyeCamera> eyeCamera in eyeCameras) {
        UpdateCamera(eyeCamera.Key);
      }
    }

    void OnPostRender() {
      if (connected && renderTexture != null) {
        var nativeTexturePtr = renderTexture.GetNativeTexturePtr();
        SendFrame(nativeTexturePtr, ref headPose, timestamp, bitRates[(int)BitRate]);
        GL.IssuePluginEvent(renderEventFunc, 69);
      }
    }

    void EnsureCamera(Camera camera) {
      // renderTexture might still be null so this creates and assigns it.
      if (renderTexture == null) {
        if (OutputResolution != Resolutions.WindowSized) {
          var selectedResolutionSize = resolutionSizes[(int)OutputResolution];
          ResizeRenderTexture(selectedResolutionSize.width, selectedResolutionSize.height);
        } else {
          ResizeRenderTexture(Screen.width, Screen.height);
        }
      }

      EyeCamera eyeCamera;

      if (!eyeCameras.TryGetValue(camera, out eyeCamera)) {
        eyeCamera = new EyeCamera();
        eyeCameras.Add(camera, eyeCamera);
      }

      EnsureEyeCamera(camera, ":Instant Preview Left", new Rect(0.0f, 0.0f, 0.5f, 1.0f), ref eyeCamera.leftEyeCamera);
      EnsureEyeCamera(camera, ":Instant Preview Right", new Rect(0.5f, 0.0f, 0.5f, 1.0f), ref eyeCamera.rightEyeCamera);
    }

    private void CheckRemoveCameras(List<Camera> cameras) {
      // Any cameras that were here last frame and not here this frame need removing from eyeCameras.
      foreach (Camera oldCamera in camerasLastFrame) {

        if (!cameras.Contains(oldCamera)) {
          // Destroys the eye cameras.
          EyeCamera curEyeCamera;
          if (eyeCameras.TryGetValue(oldCamera, out curEyeCamera)) {
            if (curEyeCamera.leftEyeCamera != null) {
              Destroy(curEyeCamera.leftEyeCamera.gameObject);
            }
            if (curEyeCamera.rightEyeCamera != null) {
              Destroy(curEyeCamera.rightEyeCamera.gameObject);
            }
          }

          // Removes eye camera entry from dictionary.
          eyeCameras.Remove(oldCamera);
        }
      }

      camerasLastFrame = cameras;
    }

    bool EnsureCameras() {
      var mainCamera = Camera.main;
      if (!mainCamera) {
        // If the main camera doesn't exist, destroys a remaining render texture and exits.
        if (renderTexture != null) {
          Destroy(renderTexture);
          renderTexture = null;
        }
        return false;
      }

      // Find all the cameras and make sure any non-Instant Preview cameras have left/right eyes attached.
      var cameras = new List<Camera>(ValidCameras());
      CheckRemoveCameras(cameras);

      // Now go and make sure that all cameras that are to be driven by Instant Preview have the correct setup.
      foreach (Camera camera in cameras) {
        // Skips the Instant Preview camera, which is used for a
        // convenience preview.
        if (camera.gameObject == gameObject) {
          continue;
        }

        EnsureCamera(camera);
      }

      return true;
    }

    void EnsureEyeCamera(Camera mainCamera, String eyeCameraName, Rect rect, ref Camera eyeCamera) {
      // Creates eye camera object if it doesn't exist.
      if (eyeCamera == null) {
        var eyeCameraObject = new GameObject(mainCamera.gameObject.name + eyeCameraName);
        eyeCamera = eyeCameraObject.AddComponent<Camera>();
        eyeCameraObject.transform.SetParent(mainCamera.gameObject.transform, false);
      }

      eyeCamera.CopyFrom(mainCamera);
      eyeCamera.rect = rect;
      eyeCamera.targetTexture = renderTexture;

      // Match child camera's skyboxes to main camera.
      Skybox monoCameraSkybox = mainCamera.gameObject.GetComponent<Skybox>();
      Skybox customSkybox = eyeCamera.GetComponent<Skybox>();
      if (monoCameraSkybox != null) {
        if (customSkybox == null) {
          customSkybox = eyeCamera.gameObject.AddComponent<Skybox>();
        }
        customSkybox.material = monoCameraSkybox.material;
      } else if (customSkybox != null) {
        Destroy(customSkybox);
      }
    }

    void ResizeRenderTexture(int width, int height) {
      var newRenderTexture = new RenderTexture(width, height, 16);
      newRenderTexture.antiAliasing = multisampleCounts[(int)MultisampleCount];
      if (renderTexture != null) {
        foreach (KeyValuePair<Camera, EyeCamera> camera in eyeCameras) {
          if (camera.Value.leftEyeCamera != null) {
            camera.Value.leftEyeCamera.targetTexture = null;
          }
          if (camera.Value.rightEyeCamera != null) {
            camera.Value.rightEyeCamera.targetTexture = null;
          }
        }

        Destroy(renderTexture);
      }
      renderTexture = newRenderTexture;
    }

    private static void SetEditorEmulatorsEnabled(bool enabled) {
      foreach (var editorEmulator in FindObjectsOfType<GvrEditorEmulator>()) {
        editorEmulator.enabled = enabled;
      }
    }

    private static Matrix4x4 PerspectiveMatrixFromUnityRect(UnityRect rect, float near, float far) {
      if (rect.left == rect.right || rect.bottom == rect.top || near == far ||
        near <= 0f || far <= 0f) {
        return Matrix4x4.identity;
      }
      rect.left *= near;
      rect.right *= near;
      rect.top *= near;
      rect.bottom *= near;
      var X = (2 * near) / (rect.right - rect.left);
      var Y = (2 * near) / (rect.top - rect.bottom);
      var A = (rect.right + rect.left) / (rect.right - rect.left);
      var B = (rect.top + rect.bottom) / (rect.top - rect.bottom);
      var C = (near + far) / (near - far);
      var D = (2 * near * far) / (near - far);

      var perspectiveMatrix = new Matrix4x4();
      perspectiveMatrix[0, 0] = X;
      perspectiveMatrix[0, 2] = A;
      perspectiveMatrix[1, 1] = Y;
      perspectiveMatrix[1, 2] = B;
      perspectiveMatrix[2, 2] = C;
      perspectiveMatrix[2, 3] = D;
      perspectiveMatrix[3, 2] = -1f;
      return perspectiveMatrix;
    }

    private static void SetTransformFromMatrix(Transform transform, Matrix4x4 matrix) {
      var position = matrix.GetRow(3);
      position.x *= -1;
      transform.localPosition = position;
      transform.localRotation = Quaternion.LookRotation(matrix.GetColumn(2), matrix.GetColumn(1));
    }

    private static void StartInstantPreviewActivity(string adbPath) {
      string output;
      string errors;
      RunCommand(adbPath, "shell monkey -p com.google.instantpreview -c android.intent.category.LAUNCHER 1", out output, out errors);

      // Early outs if no device is connected.
      if (string.Compare(errors, NoDevicesFoundAdbResult) == 0) {
        return;
      }
    }

    private static void RunCommand(string fileName, string arguments, out string output, out string errors) {
      using (var process = new System.Diagnostics.Process()) {
        System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo(fileName, arguments);
        startInfo.UseShellExecute = false;
        startInfo.RedirectStandardError = true;
        startInfo.RedirectStandardOutput = true;

        startInfo.CreateNoWindow = true;
        process.StartInfo = startInfo;

        var outputBuilder = new StringBuilder();
        var errorBuilder = new StringBuilder();
        process.OutputDataReceived += (o, ef) => outputBuilder.AppendLine(ef.Data);
        process.ErrorDataReceived += (o, ef) => errorBuilder.AppendLine(ef.Data);

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        process.WaitForExit();
        process.Close();

        // Trims the output strings to make comparison easier.
        output = outputBuilder.ToString().Trim();
        errors = errorBuilder.ToString().Trim();
      }
    }

    // Gets active, stereo, non-eye cameras in the scene.
    private IEnumerable<Camera> ValidCameras() {
      foreach (var camera in Camera.allCameras) {
        if (!camera.enabled || camera.stereoTargetEye == StereoTargetEyeMask.None) {
          continue;
        }

        // Skips camera if it is determined to be an eye camera.
        var parent = camera.transform.parent;
        if (parent != null) {
          var parentCamera = parent.GetComponent<Camera>();
          if (parentCamera != null) {
            EyeCamera parentEyeCamera;
            if (eyeCameras.TryGetValue(parentCamera, out parentEyeCamera)) {
              if (camera == parentEyeCamera.leftEyeCamera || camera == parentEyeCamera.rightEyeCamera) {
                continue;
              }
            }
          }
        }

        yield return camera;
      }
    }

#else
    public bool IsCurrentlyConnected { get { return false; } }
#endif
  }
}
