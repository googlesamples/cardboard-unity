
// Copyright (C) 2016 Google Inc. All Rights Reserved.
//
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//
//  http://www.apache.org/licenses/LICENSE-2.0
//
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//    limitations under the License.

using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Runtime.InteropServices;
using System;
using System.Collections.Generic;

/// <summary>
/// Plays video using Exoplayer rendering it on the main texture.
/// </summary>
public class GvrVideoPlayerTexture : MonoBehaviour {

  private const int MIN_BUFFER_SIZE = 2;
  private const int MAX_BUFFER_SIZE = 15;

  /// <summary>
  /// The video texture array used as a circular buffer to get the video image.
  /// </summary>
  private Texture2D[] videoTextures;
  private int currentTexture;

  /// <summary>
  /// The video player pointer used to uniquely identify the player instance.
  /// </summary>
  private IntPtr videoPlayerPtr;

  /// <summary>
  /// The video player event base.
  /// </summary>
  /// <remarks>This is added to the event id when issues events to
  ///     the plugin.
  /// </remarks>
  private int videoPlayerEventBase;

  private Texture initialTexture;

  private bool initialized;
  private int texWidth = 1024;
  private int texHeight = 1024;
  private long lastBufferedPosition;
  private float framecount = 0;

  private Graphic graphicComponent;
  private Renderer rendererComponent;

  /// <summary>
  /// The render event function.
  /// </summary>
  private IntPtr renderEventFunction;

  private bool processingRunning;

  /// <summary>List of callbacks to invoke when the video is ready.</summary>
  private List<Action<int>> onEventCallbacks;

  /// <summary>List of callbacks to invoke on exception.</summary>
  /// <remarks>The first parameter is the type of exception,
  ///     the second is the message.
  /// </remarks>
  private List<Action<string, string>> onExceptionCallbacks;

  private readonly static Queue<Action> ExecuteOnMainThread = new Queue<Action>();

  // Attach a text component to get some debug status info.
  public Text statusText;

  /// <summary>
  /// Video type.
  /// </summary>
  public enum VideoType {
    Dash = 0,
    HLS = 2,
    Other = 3
  };

  public enum VideoResolution {
    Lowest = 0,
    _720 = 720,
    _1080 = 1080,
    _2048 = 2048,
    Highest = 4096
  };

  /// <summary>
  /// Video player state.
  /// </summary>
  public enum VideoPlayerState {
    Idle = 1,
    Preparing = 2,
    Buffering = 3,
    Ready = 4,
    Ended = 5
  };

  public enum VideoEvents {
    VideoReady = 1,
    VideoStartPlayback = 2,
    VideoFormatChanged = 3,
    VideoSurfaceSet = 4,
    VideoSizeChanged = 5
  };

  /// <summary>
  /// Plugin render commands.
  /// </summary>
  /// <remarks>
  /// These are added to the eventbase for the specific player object and
  ///   issued to the plugin.
  /// </remarks>
  private enum RenderCommand {
    None = -1,
    InitializePlayer = 0,
    UpdateVideo = 1,
    RenderMono = 2,
    RenderLeftEye = 3,
    RenderRightEye = 4,
    Shutdown = 5
  };

  // The circular buffer has to be at least 2,
  // but in some cases that is too small, so set some reasonable range
  // so a slider shows up in the property inspector.
  [Range(MIN_BUFFER_SIZE, MAX_BUFFER_SIZE)]
  public int bufferSize;

  /// <summary>
  /// The type of the video.
  /// </summary>
  public VideoType videoType;
  public string videoURL;
  public string videoContentID;
  public string videoProviderId;

  public VideoResolution initialResolution = VideoResolution.Highest;

  /// <summary>
  /// True for adjusting the aspect ratio of the renderer.
  /// </summary>
  public bool adjustAspectRatio;

  /// <summary>
  /// The use secure path for DRM protected video.
  /// </summary>
  public bool useSecurePath;

  public bool VideoReady {
    get {
      return videoPlayerPtr != IntPtr.Zero && IsVideoReady(videoPlayerPtr);
    }
  }

  public long CurrentPosition {
    get {
      return videoPlayerPtr != IntPtr.Zero ? GetCurrentPosition(videoPlayerPtr) : 0;
    }
    set {
      // If the position is being set to 0, reset the framecount as well.
      // This allows the texture swapping to work correctly at the beginning
      // of the stream.
      if (value == 0) {
        framecount = 0;
      }

      SetCurrentPosition(videoPlayerPtr, value);
    }
  }

  public long VideoDuration {
    get {
      return videoPlayerPtr != IntPtr.Zero ? GetDuration(videoPlayerPtr) : 0;
    }
  }

  public long BufferedPosition {
    get {
      return videoPlayerPtr != IntPtr.Zero ? GetBufferedPosition(videoPlayerPtr) : 0;
    }
  }

  public int BufferedPercentage {
    get {
      return videoPlayerPtr != IntPtr.Zero ? GetBufferedPercentage(videoPlayerPtr) : 0;
    }
  }

  public bool IsPaused {
    get {
      return !initialized || videoPlayerPtr == IntPtr.Zero || IsVideoPaused(videoPlayerPtr);
    }
  }

  public VideoPlayerState PlayerState {
    get {
      return videoPlayerPtr != IntPtr.Zero ? (VideoPlayerState)GetPlayerState(videoPlayerPtr) : VideoPlayerState.Idle;
    }
  }

  public int MaxVolume {
    get {
      return videoPlayerPtr != IntPtr.Zero ? GetMaxVolume(videoPlayerPtr) : 0;
    }
  }

  public int CurrentVolume {
    get {
      return videoPlayerPtr != IntPtr.Zero ? GetCurrentVolume(videoPlayerPtr) : 0;
    }
    set {
      SetCurrentVolume(value);
    }
  }

  /// Create the video player instance and the event base id.
  void Awake() {
    bufferSize = bufferSize < MIN_BUFFER_SIZE ? MIN_BUFFER_SIZE : bufferSize;
    videoTextures = new Texture2D[bufferSize];
    currentTexture = 0;
    videoPlayerPtr = CreateVideoPlayer();
    videoPlayerEventBase = GetVideoPlayerEventBase(videoPlayerPtr);
    Debug.Log(" -- " + gameObject.name + " created with base " +
      videoPlayerEventBase);

    SetOnVideoEventCallback((eventId) => {
      Debug.Log("------------- E V E N T " + eventId + " -----------------");
      UpdateStatusText();
    });

    SetOnExceptionCallback((type, msg) => {
      Debug.LogError("Exception: " + type + ": " + msg);
    });

    // find the components to set the video texture on
    graphicComponent = GetComponent<Graphic>();
    rendererComponent = GetComponent<Renderer>();

    initialized = false;

    if (rendererComponent != null) {
      initialTexture = rendererComponent.material.mainTexture;
    } else if (graphicComponent) {
      initialTexture = graphicComponent.mainTexture;
    }
  }

  IEnumerator Start() {
    CreateTextureForVideoMaybe();
    renderEventFunction = GetRenderEventFunc();
    if (renderEventFunction != IntPtr.Zero) {
      IssuePlayerEvent(RenderCommand.InitializePlayer);
      yield return StartCoroutine(CallPluginAtEndOfFrames());
    }
  }

  void OnDisable() {
    if (videoPlayerPtr != IntPtr.Zero) {
      if (GetPlayerState(videoPlayerPtr) == (int)VideoPlayerState.Ready) {
        PauseVideo(videoPlayerPtr);
      }
    }
  }

  /// <summary>
  /// Sets the display texture.
  /// </summary>
  /// <param name="texture">Texture to display.
  //    If null, the initial texture of the renderer is used.</param>
  public void SetDisplayTexture(Texture texture) {
    if (texture == null) {
      texture = initialTexture;
    }

    if (texture == null) {
      return;
    }

    if (rendererComponent != null) {
      rendererComponent.sharedMaterial.mainTexture = initialTexture;
    } else if (graphicComponent != null) {
      graphicComponent.material.mainTexture = initialTexture;
    }
  }

  public void CleanupVideo() {
    Debug.Log("Cleaning Up video!");
    if (videoPlayerPtr != IntPtr.Zero) {
      DestroyVideoPlayer(videoPlayerPtr);
      videoPlayerPtr = IntPtr.Zero;
    }
    if (rendererComponent != null) {
      rendererComponent.sharedMaterial.mainTexture = initialTexture;
    } else if (graphicComponent != null) {
      graphicComponent.material.mainTexture = initialTexture;
    }
  }

  public void ReInitializeVideo() {
    if (rendererComponent != null) {
      rendererComponent.sharedMaterial.mainTexture = initialTexture;
    } else if (graphicComponent != null) {
      graphicComponent.material.mainTexture = initialTexture;
    }

    if (videoPlayerPtr == IntPtr.Zero) {
      Awake();
      IssuePlayerEvent(RenderCommand.InitializePlayer);
    }
    if (Init()) {
      StartCoroutine(CallPluginAtEndOfFrames());
    }
  }

  void OnEnable() {
    if (videoPlayerPtr != IntPtr.Zero) {
      StartCoroutine(CallPluginAtEndOfFrames());
    }
  }

  void OnDestroy() {
    if (videoPlayerPtr != IntPtr.Zero) {
      DestroyVideoPlayer(videoPlayerPtr);
    }
    foreach (Texture2D t in videoTextures) {
      Destroy(t);
    }
  }

  void OnValidate() {
    Renderer r = GetComponent<Renderer>();
    Graphic g = GetComponent<Graphic>();
    if (g == null && r == null) {
      Debug.LogError("TexturePlayer object must have either " +
        "a Renderer component or a Graphic component.");
    }
  }

  void OnApplicationPause(bool bPause) {
    if (videoPlayerPtr != IntPtr.Zero) {
      if (bPause) {
        PauseVideo(videoPlayerPtr);
      } else {
        PlayVideo(videoPlayerPtr);
      }
    }
  }

  void OnRenderObject() {

    // Don't render if not initialized.
    if (videoPlayerPtr == IntPtr.Zero || videoTextures[0] == null) {
      return;
    }

    Texture newTex = videoTextures[currentTexture];

    // Handle either the renderer component or the graphic component.
    if (rendererComponent != null) {

      // Don't render the first texture from the player, it is unitialized.
      if (currentTexture <= 1 && framecount <= 1) {
        return;
      }

      // Don't swap the textures if the video ended.
      if (PlayerState == VideoPlayerState.Ended) {
        return;
      }

      // Unity may build new a new material instance when assigning
      // material.x which can lead to duplicating materials each frame
      // whereas using the shared material will modify the original material.
      if (rendererComponent.material.mainTexture != null) {
        IntPtr currentTexId =
          rendererComponent.sharedMaterial.mainTexture.GetNativeTexturePtr();

        // Update the material's texture if it is different.
        if (currentTexId != newTex.GetNativeTexturePtr()) {
          rendererComponent.sharedMaterial.mainTexture = newTex;
          framecount += 1f;
        }
      } else {
        rendererComponent.sharedMaterial.mainTexture = newTex;
      }

    } else if (graphicComponent != null) {
      if (graphicComponent.material.mainTexture != null) {
        IntPtr currentTexId =
          graphicComponent.material.mainTexture.GetNativeTexturePtr();

        // Update the material's texture if it is different.
        if (currentTexId != newTex.GetNativeTexturePtr()) {
          graphicComponent.material.mainTexture = newTex;
          framecount += 1f;
        }
      } else {
        graphicComponent.material.mainTexture = newTex;
      }
    }
  }

  private void OnRestartVideoEvent(int eventId) {
    if (eventId == (int)VideoEvents.VideoReady) {
      Debug.Log("Restarting video complete.");
      RemoveOnVideoEventCallback(OnRestartVideoEvent);
    }
  }

  /// <summary>
  /// Resets the video player.
  /// </summary>
  public void RestartVideo() {
    SetOnVideoEventCallback(OnRestartVideoEvent);

    string theUrl = ProcessURL();

    InitVideoPlayer(videoPlayerPtr, (int) videoType, theUrl,
      videoContentID,
      videoProviderId,
      useSecurePath,
      true);
    framecount = 0;
  }

  public void SetCurrentVolume(int val) {
    SetCurrentVolume(videoPlayerPtr, val);
  }

  /// <summary>
  /// Initialize the video player.
  /// </summary>
  /// <returns>true if successful</returns>
  public bool Init() {
    if (initialized) {
      Debug.Log("Skipping initialization: video player already loaded");
      return true;
    }

    if (videoURL == null || videoURL.Length == 0) {
      Debug.LogError("Cannot initialize with null videoURL");
      return false;
    }

    videoURL = videoURL == null ? "" : videoURL.Trim();
    videoContentID = videoContentID == null ? "" : videoContentID.Trim();
    videoProviderId = videoProviderId == null ? "" : videoProviderId.Trim();

    SetInitialResolution(videoPlayerPtr, (int) initialResolution);

    string theUrl = ProcessURL();
    Debug.Log("Playing " + videoType + " " + theUrl);
    Debug.Log("videoContentID = " + videoContentID);
    Debug.Log("videoProviderId = " + videoProviderId);
    videoPlayerPtr = InitVideoPlayer(videoPlayerPtr, (int) videoType, theUrl,
              videoContentID, videoProviderId,
              useSecurePath, false);
    initialized = true;
    framecount = 0;
    return videoPlayerPtr != IntPtr.Zero;
  }

  public bool Play() {
    if (!initialized) {
      Init();
    } else if (!processingRunning) {
      StartCoroutine(CallPluginAtEndOfFrames());
    }
    if (videoPlayerPtr != IntPtr.Zero && IsVideoReady(videoPlayerPtr)) {
      return PlayVideo(videoPlayerPtr) == 0;
    } else {
      Debug.LogError("Video player not ready to Play!");
      return false;
    }
  }

  public bool Pause() {
    if (!initialized) {
      Init();
    }
    if (VideoReady) {
      return PauseVideo(videoPlayerPtr) == 0;
    } else {
      Debug.LogError("Video player not ready to Pause!");
      return false;
    }
  }

  /// <summary>
  /// Adjusts the aspect ratio.
  /// </summary>
  /// <remarks>
  /// This adjusts the transform scale to match the aspect
  ///     ratio of the texture.
  /// </remarks>
  private void AdjustAspectRatio() {
    float aspectRatio = texWidth / texHeight;

    // set the y scale based on the x value
    Vector3 newscale = transform.localScale;
    newscale.y = Mathf.Min(newscale.y, newscale.x / aspectRatio);

    transform.localScale = newscale;
  }

  /// <summary>
  /// Creates the texture for video if needed.
  /// </summary>
  private void CreateTextureForVideoMaybe() {
    if (videoTextures[0] == null || (texWidth != videoTextures[0].width ||
      texHeight != videoTextures[0].height)) {

      // Check the dimensions to make sure they are valid.
      if (texWidth < 0 || texHeight < 0) {
        // Maybe use the last dimension.  This happens when re-initializing the player.
        if (videoTextures != null && videoTextures[0].width > 0) {
          texWidth = videoTextures[0].width;
          texHeight = videoTextures[0].height;
        }
      }

      int[] tex_ids = new int[videoTextures.Length];
      for (int idx = 0; idx < videoTextures.Length; idx++) {
        // Destroy the existing texture if there.
        if (videoTextures[idx] != null) {
          Destroy(videoTextures[idx]);
        }
        videoTextures[idx] = new Texture2D(texWidth, texHeight,
          TextureFormat.RGBA32, false);
        videoTextures[idx].filterMode = FilterMode.Bilinear;
        videoTextures[idx].wrapMode = TextureWrapMode.Clamp;

        tex_ids[idx] = videoTextures[idx].GetNativeTexturePtr().ToInt32();
      }

      SetExternalTextures(videoPlayerPtr, tex_ids, tex_ids.Length,
        texWidth, texHeight);
      currentTexture = 0;
      UpdateStatusText();
    }

    if (adjustAspectRatio) {
      AdjustAspectRatio();
    }
  }

  private void UpdateStatusText() {
    float fps = CurrentPosition > 0 ?
      (framecount / (CurrentPosition / 1000f)) : CurrentPosition;
    string status = texWidth + " x " + texHeight + " buffer: " +
      (BufferedPosition / 1000) + " " + PlayerState + " fps: " + fps;
    if (statusText != null) {
      if (statusText.text != status) {
        statusText.text = status;
        Debug.Log("STATUS: " + status);
      }
    }
  }

  /// <summary>
  /// Issues the player event.
  /// </summary>
  /// <param name="evt">The event to send to the video player
  ///     instance.
  /// </param>
  private void IssuePlayerEvent(RenderCommand evt) {
    if (renderEventFunction != IntPtr.Zero && evt != RenderCommand.None) {
      GL.IssuePluginEvent(renderEventFunction,
        videoPlayerEventBase + (int) evt);
    }
  }

  void Update() {
    while (ExecuteOnMainThread.Count > 0) {
      ExecuteOnMainThread.Dequeue().Invoke();
    }
  }

  private IEnumerator CallPluginAtEndOfFrames() {
    if (processingRunning) {
      Debug.LogError("CallPluginAtEndOfFrames invoked while already running.");
      Debug.LogError(StackTraceUtility.ExtractStackTrace());
      yield return null;
    }

    // Only run while the video is playing.
    bool running = true;
    processingRunning = true;
    while (running) {
      // Wait until all frame rendering is done
      yield return new WaitForEndOfFrame();

      if (videoPlayerPtr != IntPtr.Zero) {
        CreateTextureForVideoMaybe();
      }

      IntPtr tex = GetRenderableTextureId(videoPlayerPtr);
      currentTexture = 0;
      for (int i = 0; i < videoTextures.Length; i++) {
        if (tex == videoTextures[i].GetNativeTexturePtr()) {
          currentTexture = i;
        }
      }

      if (!VideoReady) {
        continue;
      } else if (framecount > 1 && PlayerState == VideoPlayerState.Ended) {
        running = false;
      }

      IssuePlayerEvent(RenderCommand.UpdateVideo);
      IssuePlayerEvent(RenderCommand.RenderMono);

      int w = GetWidth(videoPlayerPtr);
      int h = GetHeight(videoPlayerPtr);
      if (w > 2560 && h > 10) {
        // Clamp the max resolution.
        w = 2560;
        h = 1440;
      }
      texWidth = w;
      texHeight = h;

      if ((int) framecount % 30 == 0) {
        UpdateStatusText();
      }

      long bp = BufferedPosition;
      if (bp != lastBufferedPosition) {
        lastBufferedPosition = bp;
        UpdateStatusText();
      }
    }
    processingRunning = false;
  }

  public void RemoveOnVideoEventCallback(Action<int> callback) {
    if (onEventCallbacks != null) {
      onEventCallbacks.Remove(callback);
    }
  }

  public void SetOnVideoEventCallback(Action<int> callback) {
    if (onEventCallbacks == null) {
      onEventCallbacks = new List<Action<int>>();
    }
    onEventCallbacks.Add(callback);
    SetOnVideoEventCallback(videoPlayerPtr, InternalOnVideoEventCallback,
      ToIntPtr(this));
  }

  internal void FireVideoEvent(int eventId) {
    if (onEventCallbacks == null) {
      return;
    }

    // Copy the collection so the callbacks can remove themselves from the list.
    Action<int>[] cblist = onEventCallbacks.ToArray();
    foreach (Action<int> cb in cblist) {
      try {
        cb(eventId);
      } catch (Exception e) {
        Debug.LogError("exception calling callback: " + e);
      }
    }
  }

  [AOT.MonoPInvokeCallback(typeof(OnVideoEventCallback))]
  static void InternalOnVideoEventCallback(IntPtr cbdata, int eventId) {
    if (cbdata == IntPtr.Zero) {
      return;
    }

    GvrVideoPlayerTexture player;
    var gcHandle = GCHandle.FromIntPtr(cbdata);
    try {
      player = (GvrVideoPlayerTexture) gcHandle.Target;
    }
    catch (InvalidCastException e) {
      Debug.LogError("GC Handle pointed to unexpected type: " +
        gcHandle.Target + ". Expected " +
        typeof(GvrVideoPlayerTexture));
      throw e;
    }

    if (player != null) {
      ExecuteOnMainThread.Enqueue(() => player.FireVideoEvent(eventId));
    }
  }

  public void SetOnExceptionCallback(Action<string, string> callback) {
    if (onExceptionCallbacks == null) {
      onExceptionCallbacks = new List<Action<string, string>>();
      SetOnExceptionCallback(videoPlayerPtr, InternalOnExceptionCallback,
        ToIntPtr(this));
    }
    onExceptionCallbacks.Add(callback);
  }


  [AOT.MonoPInvokeCallback(typeof(OnExceptionCallback))]
  static void InternalOnExceptionCallback(string type, string msg,
                      IntPtr cbdata) {
    if (cbdata == IntPtr.Zero) {
      return;
    }

    GvrVideoPlayerTexture player;
    var gcHandle = GCHandle.FromIntPtr(cbdata);
    try {
      player = (GvrVideoPlayerTexture) gcHandle.Target;
    }
    catch (InvalidCastException e) {
      Debug.LogError("GC Handle pointed to unexpected type: " +
           gcHandle.Target + ". Expected " +
           typeof(GvrVideoPlayerTexture));
      throw e;
    }

    if (player != null) {
      ExecuteOnMainThread.Enqueue(() => player.FireOnException(type, msg));
    }
  }

  internal void FireOnException(string type, string msg) {
    if (onExceptionCallbacks == null) {
      return;
    }

    foreach (Action<string, string> cb in onExceptionCallbacks) {
      try {
        cb(type, msg);
      } catch (Exception e) {
        Debug.LogError("exception calling callback: " + e);
      }
    }
  }

  internal static IntPtr ToIntPtr(System.Object obj) {
    GCHandle handle = GCHandle.Alloc(obj);
    return GCHandle.ToIntPtr(handle);
  }

  internal string ProcessURL() {
    return videoURL.Replace("${Application.dataPath}", Application.dataPath);
  }

  internal delegate void OnVideoEventCallback(IntPtr cbdata, int eventId);

  internal delegate void OnExceptionCallback(string type, string msg,
                         IntPtr cbdata);

#if UNITY_ANDROID && !UNITY_EDITOR
  private const string dllName = "gvrvideo";
  [DllImport(dllName)]
  private static extern IntPtr GetRenderEventFunc();

  [DllImport(dllName)]
  private static extern void SetExternalTextures(IntPtr videoPlayerPtr,
                                                 int[] texIds,
                                                 int size,
                                                 int w,
                                                 int h);

  [DllImport(dllName)]
  private static extern IntPtr GetRenderableTextureId(IntPtr videoPlayerPtr);

  // Keep public so we can check for the dll being present at runtime.
  [DllImport(dllName)]
  public static extern IntPtr CreateVideoPlayer();

  // Keep public so we can check for the dll being present at runtime.
  [DllImport(dllName)]
  public static extern void DestroyVideoPlayer(IntPtr videoPlayerPtr);

  [DllImport(dllName)]
  private static extern int GetVideoPlayerEventBase(IntPtr videoPlayerPtr);

  [DllImport(dllName)]
  private static extern IntPtr InitVideoPlayer(IntPtr videoPlayerPtr,
                                               int videoType,
                                               string videoURL,
                                               string contentID,
                                               string providerId,
                                               bool useSecurePath,
                                               bool useExisting);

  [DllImport(dllName)]
  private static extern void SetInitialResolution(IntPtr videoPlayerPtr,
                                                  int initialResolution);

  [DllImport(dllName)]
  private static extern int GetPlayerState(IntPtr videoPlayerPtr);

  [DllImport(dllName)]
  private static extern int GetWidth(IntPtr videoPlayerPtr);

  [DllImport(dllName)]
  private static extern int GetHeight(IntPtr videoPlayerPtr);

  [DllImport(dllName)]
  private static extern int PlayVideo(IntPtr videoPlayerPtr);

  [DllImport(dllName)]
  private static extern int PauseVideo(IntPtr videoPlayerPtr);

  [DllImport(dllName)]
  private static extern bool IsVideoReady(IntPtr videoPlayerPtr);

  [DllImport(dllName)]
  private static extern bool IsVideoPaused(IntPtr videoPlayerPtr);

  [DllImport(dllName)]
  private static extern long GetDuration(IntPtr videoPlayerPtr);

  [DllImport(dllName)]
  private static extern long GetBufferedPosition(IntPtr videoPlayerPtr);

  [DllImport(dllName)]
  private static extern long GetCurrentPosition(IntPtr videoPlayerPtr);

  [DllImport(dllName)]
  private static extern void SetCurrentPosition(IntPtr videoPlayerPtr,
                                                long pos);

  [DllImport(dllName)]
  private static extern int GetBufferedPercentage(IntPtr videoPlayerPtr);

  [DllImport(dllName)]
  private static extern int GetMaxVolume(IntPtr videoPlayerPtr);

  [DllImport(dllName)]
  private static extern int GetCurrentVolume(IntPtr videoPlayerPtr);

  [DllImport(dllName)]
  private static extern void SetCurrentVolume(IntPtr videoPlayerPtr,
      int value);

  [DllImport(dllName)]
  private static extern bool SetVideoPlayerSupportClassname(
      IntPtr videoPlayerPtr,
      string classname);

  [DllImport(dllName)]
  private static extern IntPtr GetRawPlayer(IntPtr videoPlayerPtr);

  [DllImport(dllName)]
  private static extern  void SetOnVideoEventCallback(IntPtr videoPlayerPtr,
      OnVideoEventCallback callback,
      IntPtr callback_arg);

  [DllImport(dllName)]
  private static extern void SetOnExceptionCallback(IntPtr videoPlayerPtr,
      OnExceptionCallback callback,
      IntPtr callback_arg);
#else
  private const string NOT_IMPLEMENTED_MSG =
    "Not implemented on this platform";

  private static IntPtr GetRenderEventFunc() {
    Debug.Log(NOT_IMPLEMENTED_MSG);
    return IntPtr.Zero;
  }

  private static void SetExternalTextures(IntPtr videoPlayerPtr,
                      int[] texIds,
                      int size,
                      int w,
                      int h) {
    Debug.Log(NOT_IMPLEMENTED_MSG);
  }

  private static IntPtr GetRenderableTextureId(IntPtr videoPlayerPtr) {
    return IntPtr.Zero;
  }

  // Make this public so we can test the loading of the DLL.
  public static IntPtr CreateVideoPlayer() {
    Debug.Log(NOT_IMPLEMENTED_MSG);
    return IntPtr.Zero;
  }


  // Make this public so we can test the loading of the DLL.
  public static void DestroyVideoPlayer(IntPtr videoPlayerPtr) {
    Debug.Log(NOT_IMPLEMENTED_MSG);
  }


  private static int GetVideoPlayerEventBase(IntPtr videoPlayerPtr) {
    Debug.Log(NOT_IMPLEMENTED_MSG);
    return 0;
  }

  private static IntPtr InitVideoPlayer(IntPtr videoPlayerPtr, int videoType,
                      string videoURL,
                      string contentID,
                      string providerId,
                      bool useSecurePath,
                      bool useExisting) {
    Debug.Log(NOT_IMPLEMENTED_MSG);
    return IntPtr.Zero;
  }

  private static void SetInitialResolution(IntPtr videoPlayerPtr,
                       int initialResolution) {
    Debug.Log(NOT_IMPLEMENTED_MSG);
  }

  private static int GetPlayerState(IntPtr videoPlayerPtr) {
    Debug.Log(NOT_IMPLEMENTED_MSG);
    return -1;
  }

  private static int GetWidth(IntPtr videoPlayerPtr) {
    Debug.Log(NOT_IMPLEMENTED_MSG);
    return -1;
  }

  private static int GetHeight(IntPtr videoPlayerPtr) {
    Debug.Log(NOT_IMPLEMENTED_MSG);
    return -1;
  }

  private static int PlayVideo(IntPtr videoPlayerPtr) {
    Debug.Log(NOT_IMPLEMENTED_MSG);
    return 0;
  }


  private static int PauseVideo(IntPtr videoPlayerPtr) {
    Debug.Log(NOT_IMPLEMENTED_MSG);
    return 0;
  }

  private static bool IsVideoReady(IntPtr videoPlayerPtr) {
    Debug.Log(NOT_IMPLEMENTED_MSG);
    return false;
  }

  private static bool IsVideoPaused(IntPtr videoPlayerPtr) {
    Debug.Log(NOT_IMPLEMENTED_MSG);
    return true;
  }

  private static long GetDuration(IntPtr videoPlayerPtr) {
    Debug.Log(NOT_IMPLEMENTED_MSG);
    return -1;
  }

  private static long GetBufferedPosition(IntPtr videoPlayerPtr) {
    Debug.Log(NOT_IMPLEMENTED_MSG);
    return -1;
  }

  private static long GetCurrentPosition(IntPtr videoPlayerPtr) {
    Debug.Log(NOT_IMPLEMENTED_MSG);
    return -1;
  }

  private static void SetCurrentPosition(IntPtr videoPlayerPtr, long pos) {
    Debug.Log(NOT_IMPLEMENTED_MSG);
  }

  private static int GetBufferedPercentage(IntPtr videoPlayerPtr) {
    Debug.Log(NOT_IMPLEMENTED_MSG);
    return 0;
  }

  private static int GetMaxVolume(IntPtr videoPlayerPtr) {
    Debug.Log(NOT_IMPLEMENTED_MSG);
    return 0;
  }

  private static int GetCurrentVolume(IntPtr videoPlayerPtr) {
    Debug.Log(NOT_IMPLEMENTED_MSG);
    return 0;
  }

  private static void SetCurrentVolume(IntPtr videoPlayerPtr, int value) {
    Debug.Log(NOT_IMPLEMENTED_MSG);
  }

  private static bool SetVideoPlayerSupportClassname(IntPtr videoPlayerPtr,
                             string classname) {
    Debug.Log(NOT_IMPLEMENTED_MSG);
    return false;
  }

  private static IntPtr GetRawPlayer(IntPtr videoPlayerPtr) {
    Debug.Log(NOT_IMPLEMENTED_MSG);
    return IntPtr.Zero;
  }

  private static void SetOnVideoEventCallback(IntPtr videoPlayerPtr,
    OnVideoEventCallback callback,
    IntPtr callback_arg) {
    Debug.Log(NOT_IMPLEMENTED_MSG);
  }

  private static void SetOnExceptionCallback(IntPtr videoPlayerPtr,
    OnExceptionCallback callback,
    IntPtr callback_arg) {
    Debug.Log(NOT_IMPLEMENTED_MSG);
  }
#endif  // UNITY_ANDROID && !UNITY_EDITOR
}

