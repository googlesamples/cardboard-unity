// Copyright 2015 Google Inc. All rights reserved.
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

#if !(UNITY_4_5 || UNITY_4_6 || UNITY_4_7 || UNITY_5_0 || UNITY_5_1 || UNITY_IOS)
// Cardboard native audio spatializer plugin is only supported by Unity 5.2+.
// If you get script compile errors in this file, comment out the line below.
#define USE_SPATIALIZER_PLUGIN
#endif

using UnityEngine;
using System.Collections;

/// Cardboard audio source component that enhances AudioSource to provide advanced spatial audio
/// features.
[AddComponentMenu("Cardboard/Audio/CardboardAudioSource")]
public class CardboardAudioSource : MonoBehaviour {
  /// Directivity pattern shaping factor.
  public float directivityAlpha = 0.0f;

  /// Directivity pattern order.
  public float directivitySharpness = 1.0f;

  /// Input gain in decibels.
  public float gainDb = 0.0f;

  /// Occlusion effect toggle.
  public bool occlusionEnabled = false;

  /// Play source on awake.
  public bool playOnAwake = true;

  /// Volume rolloff model with respect to the distance.
  public AudioRolloffMode rolloffMode = AudioRolloffMode.Logarithmic;

  /// The default AudioClip to play.
  public AudioClip clip {
    get { return clip; }
    set {
      sourceClip = value;
      if (audioSource != null) {
        audioSource.clip = sourceClip;
      }
    }
  }
  [SerializeField]
  private AudioClip sourceClip = null;

  /// Is the clip playing right now (Read Only)?
  public bool isPlaying {
    get {
      if (audioSource != null) {
        return audioSource.isPlaying;
      }
      return false;
    }
  }

  /// Is the audio clip looping?
  public bool loop {
    get { return sourceLoop; }
    set {
      sourceLoop = value;
      if (audioSource != null) {
        audioSource.loop = sourceLoop;
      }
    }
  }
  [SerializeField]
  private bool sourceLoop = false;

  /// Un- / Mutes the source. Mute sets the volume=0, Un-Mute restore the original volume.
  public bool mute {
    get { return sourceMute; }
    set {
      sourceMute = value;
      if (audioSource != null) {
        audioSource.mute = sourceMute;
      }
    }
  }
  [SerializeField]
  private bool sourceMute = false;

  /// The pitch of the audio source.
  public float pitch {
    get { return sourcePitch; }
    set {
      sourcePitch = value;
      if (audioSource != null) {
        audioSource.pitch = sourcePitch;
      }
    }
  }
  [SerializeField]
  [Range(-3.0f, 3.0f)]
  private float sourcePitch = 1.0f;

  /// The volume of the audio source (0.0 to 1.0).
  public float volume {
    get { return sourceVolume; }
    set {
      sourceVolume = value;
      if (audioSource != null) {
        audioSource.volume = sourceVolume;
      }
    }
  }
  [SerializeField]
  [Range(0.0f, 1.0f)]
  private float sourceVolume = 1.0f;

  /// MaxDistance is the distance a sound stops attenuating at.
  public float maxDistance {
    get { return sourceMaxDistance; }
    set {
      sourceMaxDistance = Mathf.Clamp(sourceMaxDistance,
                                      sourceMinDistance + CardboardAudio.distanceEpsilon,
                                      CardboardAudio.maxDistanceLimit);
    }
  }
  [SerializeField]
  private float sourceMaxDistance = 500.0f;

  /// Within the Min distance the CardboardAudioSource will cease to grow louder in volume.
  public float minDistance {
    get { return sourceMinDistance; }
    set {
      sourceMinDistance = Mathf.Clamp(value, 0.0f, CardboardAudio.minDistanceLimit);
    }
  }
  [SerializeField]
  private float sourceMinDistance = 0.0f;

  /// Binaural (HRTF) rendering toggle.
  [SerializeField]
  private bool hrtfEnabled = true;

  // Unique source id.
  private int id = -1;

  // Current occlusion value;
  private float currentOcclusion = 0.0f;

  // Target occlusion value.
  private float targetOcclusion = 0.0f;

  // Next occlusion update time in seconds.
  private float nextOcclusionUpdate = 0.0f;

  // Unity audio source attached to the game object.
  private AudioSource audioSource = null;

  // Denotes whether the source is currently paused or not.
  private bool isPaused = false;

  void Awake () {
    audioSource = gameObject.AddComponent<AudioSource>();
    audioSource.hideFlags = HideFlags.HideInInspector;
    audioSource.playOnAwake = false;
    audioSource.bypassReverbZones = true;
#if UNITY_4_5 || UNITY_4_6 || UNITY_4_7
    audioSource.panLevel = 0.0f;
#else
    audioSource.spatialBlend = 0.0f;
#endif
    OnValidate();
  }

  void OnEnable () {
    audioSource.enabled = true;
    if (playOnAwake && !isPlaying) {
      Play();
    }
  }

  void Start () {
    if (playOnAwake && !isPlaying) {
      Play();
    }
  }

  void OnDisable () {
    Stop();
    audioSource.enabled = false;
  }

  void OnDestroy () {
    Destroy(audioSource);
  }

  void Update () {
    // Update occlusion state.
    if (!occlusionEnabled) {
      targetOcclusion = 0.0f;
    } else if (Time.time >= nextOcclusionUpdate) {
      nextOcclusionUpdate = Time.time + CardboardAudio.occlusionDetectionInterval;
      targetOcclusion = CardboardAudio.ComputeOcclusion(transform);
    }
    currentOcclusion = Mathf.Lerp(currentOcclusion, targetOcclusion,
                                  CardboardAudio.occlusionLerpSpeed * Time.deltaTime);
    // Update source.
    if (!isPlaying && !isPaused) {
      Stop();
    } else {
      CardboardAudio.UpdateAudioSource(id, transform, gainDb, rolloffMode, sourceMinDistance,
                                       sourceMaxDistance, directivityAlpha, directivitySharpness,
                                       currentOcclusion);
    }
  }

#if !USE_SPATIALIZER_PLUGIN
  void OnAudioFilterRead (float[] data, int channels) {
    if (id >= 0) {
      // Pass the next buffer to the audio system.
      CardboardAudio.ProcessAudioSource(id, data, data.Length);
    } else {
      // Fill the buffer with zeros if the source has not been initialized yet.
      for (int i = 0; i < data.Length; ++i) {
        data[i] = 0.0f;
      }
    }
  }
#endif

  /// Pauses playing the clip.
  public void Pause () {
    if (audioSource != null) {
      isPaused = true;
      audioSource.Pause();
    }
  }

  /// Plays the clip.
  public void Play () {
    if (audioSource != null && InitializeSource()) {
      audioSource.Play();
      isPaused = false;
    }
  }

  /// Plays the clip with a delay specified in seconds.
  public void PlayDelayed (float delay) {
    if (audioSource != null && InitializeSource()) {
      audioSource.PlayDelayed(delay);
      isPaused = false;
    }
  }

  /// Plays an AudioClip.
  public void PlayOneShot (AudioClip clip) {
    PlayOneShot(clip, 1.0f);
  }

  /// Plays an AudioClip, and scales its volume.
  public void PlayOneShot (AudioClip clip, float volume) {
    if (audioSource != null && InitializeSource()) {
      audioSource.PlayOneShot(clip, volume);
      isPaused = false;
    }
  }

  /// Stops playing the clip.
  public void Stop () {
    if (audioSource != null) {
      audioSource.Stop();
      ShutdownSource();
      isPaused = false;
    }
  }

#if !(UNITY_4_5 || UNITY_4_6 || UNITY_4_7)
  /// Unpauses the paused playback.
  public void UnPause () {
    if (audioSource != null) {
      audioSource.UnPause();
      isPaused = false;
    }
  }
#endif

  // Initializes the source.
  private bool InitializeSource () {
    if (id < 0) {
      id = CardboardAudio.CreateAudioSource(hrtfEnabled);
      if (id >= 0) {
        CardboardAudio.UpdateAudioSource(id, transform, gainDb, rolloffMode, sourceMinDistance,
                                         sourceMaxDistance, directivityAlpha, directivitySharpness,
                                         currentOcclusion);
#if USE_SPATIALIZER_PLUGIN
        audioSource.spatialize = true;
        audioSource.SetSpatializerFloat(0, id);
#endif
      }
    }
    return id >= 0;
  }

  // Shuts down the source.
  private void ShutdownSource () {
    if (id >= 0) {
#if USE_SPATIALIZER_PLUGIN
      audioSource.SetSpatializerFloat(0, -1.0f);
      audioSource.spatialize = false;
#endif
      CardboardAudio.DestroyAudioSource(id);
      id = -1;
    }
  }

  void OnValidate () {
    clip = sourceClip;
    loop = sourceLoop;
    mute = sourceMute;
    pitch = sourcePitch;
    volume = sourceVolume;
    minDistance = sourceMinDistance;
    maxDistance = sourceMaxDistance;
  }

#if !(UNITY_4_5 || UNITY_4_6 || UNITY_4_7)
  void OnDrawGizmosSelected () {
    Gizmos.color = new Color(0.75f, 0.75f, 1.0f, 0.5f);
    DrawDirectivityGizmo(180);
  }

  // Draws a 3D gizmo in the Scene View that shows the selected directivity pattern.
  private void DrawDirectivityGizmo (int resolution) {
    Vector2[] points = CardboardAudio.Generate2dPolarPattern(directivityAlpha, directivitySharpness,
                                                             resolution);
    // Compute |vertices| from the polar pattern |points|.
    int numVertices = resolution + 1;
    Vector3[] vertices = new Vector3[numVertices];
    vertices[0] = Vector3.zero;
    for (int i = 0; i < points.Length; ++i) {
      vertices[i + 1] = new Vector3(points[i].x, 0.0f, points[i].y);
    }
    // Generate |triangles| from |vertices|. Two triangles per each sweep to avoid backface culling.
    int[] triangles = new int[6 * numVertices];
    for (int i = 0; i < numVertices - 1; ++i) {
      int index = 6 * i;
      if (i < numVertices - 2) {
        triangles[index] = 0;
        triangles[index + 1] = i + 1;
        triangles[index + 2] = i + 2;
      } else {
        // Last vertex is connected back to the first for the last triangle.
        triangles[index] = 0;
        triangles[index + 1] = numVertices - 1;
        triangles[index + 2] = 1;
      }
      // The second triangle facing the opposite direction.
      triangles[index + 3] = triangles[index];
      triangles[index + 4] = triangles[index + 2];
      triangles[index + 5] = triangles[index + 1];
    }
    // Construct a new mesh for the gizmo.
    Mesh directivityGizmoMesh = new Mesh();
    directivityGizmoMesh.hideFlags = HideFlags.DontSaveInEditor;
    directivityGizmoMesh.vertices = vertices;
    directivityGizmoMesh.triangles = triangles;
    directivityGizmoMesh.RecalculateNormals();
    // Draw the mesh.
    Vector3 scale = 2.0f * Mathf.Max(transform.lossyScale.x, transform.lossyScale.z) * Vector3.one;
    Gizmos.DrawMesh(directivityGizmoMesh, transform.position, transform.rotation, scale);
  }
#endif
}
