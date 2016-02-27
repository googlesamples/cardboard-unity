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
using System;
using System.Collections;
using System.IO;
using System.Runtime.InteropServices;

/// This is the main Cardboard audio class that communicates with the native code implementation of
/// the audio system. Native functions of the system can only be called through this class to
/// preserve the internal system functionality. Public function calls are *not* thread-safe.
public static class CardboardAudio {
  /// Audio system rendering quality.
  public enum Quality {
    Low = 0,  /// Stereo-only rendering
    Medium = 1,  /// Binaural rendering (first-order HRTF)
    High = 2  /// Binaural rendering (third-order HRTF)
  }

  /// System sampling rate.
  public static int SampleRate {
    get { return sampleRate; }
  }
  private static int sampleRate = -1;

  /// System number of output channels.
  public static int NumChannels {
    get { return numChannels; }
  }
  private static int numChannels = -1;

  /// System number of frames per buffer.
  public static int FramesPerBuffer {
    get { return framesPerBuffer; }
  }
  private static int framesPerBuffer = -1;

  /// Initializes the audio system with the current audio configuration.
  /// @note This should only be called from the main Unity thread.
  public static void Initialize (CardboardAudioListener listener, Quality quality) {
    if (!initialized) {
      // Initialize the audio system.
#if UNITY_4_5 || UNITY_4_6 || UNITY_4_7
      sampleRate = AudioSettings.outputSampleRate;
      numChannels = (int)AudioSettings.speakerMode;
      int numBuffers = -1;
      AudioSettings.GetDSPBufferSize(out framesPerBuffer, out numBuffers);
#else
      AudioConfiguration config = AudioSettings.GetConfiguration();
      sampleRate = config.sampleRate;
      numChannels = (int)config.speakerMode;
      framesPerBuffer = config.dspBufferSize;
#endif
      if (numChannels != (int)AudioSpeakerMode.Stereo) {
        Debug.LogError("Only 'Stereo' speaker mode is supported by Cardboard.");
        return;
      }
      Initialize(quality, sampleRate, numChannels, framesPerBuffer);
      listenerTransform = listener.transform;
      initialized = true;

      Debug.Log("Cardboard audio system is initialized (Quality: " + quality + ", Sample Rate: " +
                sampleRate + ", Channels: " + numChannels + ", Frames Per Buffer: " +
                framesPerBuffer + ").");
    } else if (listener.transform != listenerTransform) {
      Debug.LogError("Only one CardboardAudioListener component is allowed in the scene.");
      CardboardAudioListener.Destroy(listener);
    }
  }

  /// Shuts down the audio system.
  /// @note This should only be called from the main Unity thread.
  public static void Shutdown (CardboardAudioListener listener) {
    if (initialized && listener.transform == listenerTransform) {
      initialized = false;

      Shutdown();
      sampleRate = -1;
      numChannels = -1;
      framesPerBuffer = -1;

      Debug.Log("Cardboard audio system is shutdown.");
    }
  }

  /// Processes the next |data| buffer of the audio system.
  /// @note This should only be called from the audio thread.
  public static void ProcessAudioListener (float[] data, int length) {
    if (initialized) {
      ProcessListener(data, length);
    }
  }

  /// Updates the audio listener.
  /// @note This should only be called from the main Unity thread.
  public static void UpdateAudioListener (float globalGainDb, float worldScale) {
    if (initialized) {
      worldScaleInverse = 1.0f / worldScale;
      float globalGain = ConvertAmplitudeFromDb(globalGainDb);
      Vector3 position = listenerTransform.position;
      Quaternion rotation = listenerTransform.rotation;
      ConvertAudioTransformFromUnity(ref position, ref rotation);
      // Pass listener properties to the system.
      SetListenerGain(globalGain);
      SetListenerTransform(position.x, position.y, position.z, rotation.x, rotation.y, rotation.z,
                           rotation.w);
    }
  }

  /// Creates a new audio source with a unique id.
  /// @note This should only be called from the main Unity thread.
  public static int CreateAudioSource (bool hrtfEnabled) {
    int sourceId = -1;
    if (initialized) {
      sourceId = CreateSource(hrtfEnabled);
    }
    return sourceId;
  }

  /// Destroys the audio source with given |id|.
  /// @note This should only be called from the main Unity thread.
  public static void DestroyAudioSource (int id) {
    if (initialized) {
      DestroySource(id);
    }
  }

#if !USE_SPATIALIZER_PLUGIN
  /// Processes the next |data| buffer of the audio source with given |id|.
  /// @note This should only be called from the audio thread.
  public static void ProcessAudioSource (int id, float[] data, int length) {
    if (initialized) {
      ProcessSource(id, length, data, data);
    }
  }
#endif

  /// Updates the audio source with given |id| and its properties.
  /// @note This should only be called from the main Unity thread.
  public static void UpdateAudioSource (int id, Transform transform, float gainDb,
                                        AudioRolloffMode rolloffMode, float minDistance,
                                        float maxDistance, float alpha, float sharpness,
                                        float occlusion) {
    if (initialized) {
      float gain = ConvertAmplitudeFromDb(gainDb);
      Vector3 position = transform.position;
      Quaternion rotation = transform.rotation;
      float scale = worldScaleInverse * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y,
                                                  transform.lossyScale.z);
      ConvertAudioTransformFromUnity(ref position, ref rotation);
      // Pass the source properties to the audio system.
      SetSourceDirectivity(id, alpha, sharpness);
      SetSourceGain(id, gain);
      SetSourceOcclusionIntensity(id, occlusion);
      if (rolloffMode != AudioRolloffMode.Custom) {
        SetSourceDistanceAttenuationMethod(id, rolloffMode, minDistance, maxDistance);
      }
      SetSourceTransform(id, position.x, position.y, position.z, rotation.x, rotation.y, rotation.z,
                         rotation.w, scale);
    }
  }

  /// Creates a new room with a unique id.
  /// @note This should only be called from the main Unity thread.
  public static int CreateAudioRoom () {
    int roomId = -1;
    if (initialized) {
      roomId = CreateRoom();
    }
    return roomId;
  }

  /// Destroys the room with given |id|.
  /// @note This should only be called from the main Unity thread.
  public static void DestroyAudioRoom (int id) {
    if (initialized) {
      DestroyRoom(id);
    }
  }

  /// Updates the room effects of the environment with given |room| properties.
  /// @note This should only be called from the main Unity thread.
  public static void UpdateAudioRoom (int id, Transform transform,
                                      CardboardAudioRoom.SurfaceMaterial[] materials,
                                      float reflectivity, float reverbGainDb,
                                      float reverbBrightness, float reverbTime, Vector3 size) {
    if (initialized) {
      // Update room transform.
      Vector3 position = transform.position;
      Quaternion rotation = transform.rotation;
      Vector3 scale = Vector3.Scale(size, transform.lossyScale);
      scale = worldScaleInverse * new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y),
                                              Mathf.Abs(scale.z));
      ConvertAudioTransformFromUnity(ref position, ref rotation);
      float reverbGain = ConvertAmplitudeFromDb(reverbGainDb);
      SetRoomProperties(id, position.x, position.y, position.z, rotation.x, rotation.y, rotation.z,
                        rotation.w, scale.x, scale.y, scale.z, materials, reflectivity, reverbGain,
                        reverbBrightness, reverbTime);
    }
  }

  /// Computes the occlusion intensity of a given |source| using point source detection.
  /// @note This should only be called from the main Unity thread.
  public static float ComputeOcclusion (Transform sourceTransform) {
    float occlusion = 0.0f;
    if (initialized) {
      Vector3 listenerPosition = listenerTransform.position;
      Vector3 sourceFromListener = sourceTransform.position - listenerPosition;
      RaycastHit[] hits =
          Physics.RaycastAll(listenerPosition, sourceFromListener, sourceFromListener.magnitude);
      foreach (RaycastHit hit in hits) {
        if (hit.transform != listenerTransform && hit.transform != sourceTransform) {
          occlusion += 1.0f;
        }
      }
    }
    return occlusion;
  }

  /// Generates a set of points to draw a 2D polar pattern.
  public static Vector2[] Generate2dPolarPattern (float alpha, float order, int resolution) {
    Vector2[] points = new Vector2[resolution];
    float interval = 2.0f * Mathf.PI / resolution;
    for (int i = 0; i < resolution; ++i) {
      float theta = i * interval;
      // Magnitude |r| for |theta| in radians.
      float r = Mathf.Pow(Mathf.Abs((1 - alpha) + alpha * Mathf.Cos(theta)), order);
      points[i] = new Vector2(r * Mathf.Sin(theta), r * Mathf.Cos(theta));
    }
    return points;
  }

  /// Minimum distance threshold between |minDistance| and |maxDistance|.
  public const float distanceEpsilon = 0.01f;

  /// Max distance limit that can be set for volume rolloff.
  public const float maxDistanceLimit = 1000000.0f;

  /// Min distance limit that can be set for volume rolloff.
  public const float minDistanceLimit = 990099.0f;

  /// Maximum allowed gain value in decibels.
  public const float maxGainDb = 24.0f;

  /// Minimum allowed gain value in decibels.
  public const float minGainDb = -24.0f;

  /// Maximum allowed real world scale with respect to Unity.
  public const float maxWorldScale = 1000.0f;

  /// Minimum allowed real world scale with respect to Unity.
  public const float minWorldScale = 0.001f;

  /// Maximum allowed reverb brightness modifier value.
  public const float maxReverbBrightness = 1.0f;

  /// Minimum allowed reverb brightness modifier value.
  public const float minReverbBrightness = -1.0f;

  /// Maximum allowed reverb time modifier value.
  public const float maxReverbTime = 3.0f;

  /// Maximum allowed reflectivity multiplier of a room surface material.
  public const float maxReflectivity = 2.0f;

  /// Source occlusion detection rate in seconds.
  public const float occlusionDetectionInterval = 0.2f;

  /// Occlusion interpolation amount approximately per second.
  public const float occlusionLerpSpeed = 6.0f;

  /// Number of surfaces in a room.
  public const int numRoomSurfaces = 6;

  /// Converts given |db| value to its amplitude equivalent where 'dB = 20 * log10(amplitude)'.
  private static float ConvertAmplitudeFromDb (float db) {
    return Mathf.Pow(10.0f, 0.05f * db);
  }

  // Converts given |position| and |rotation| from Unity space to audio space.
  private static void ConvertAudioTransformFromUnity (ref Vector3 position,
                                                      ref Quaternion rotation) {
    pose.SetRightHanded(Matrix4x4.TRS(position, rotation, Vector3.one));
    position = pose.Position * worldScaleInverse;
    rotation = pose.Orientation;
  }

  // Denotes whether the system is initialized properly.
  private static bool initialized = false;

  // Listener transform.
  private static Transform listenerTransform = null;

  // 3D pose instance to be used in transform space conversion.
  private static MutablePose3D pose = new MutablePose3D();

  // Inverted world scale.
  private static float worldScaleInverse = 1.0f;

#if UNITY_IOS
  private const string pluginName = "__Internal";
#else
  private const string pluginName = "audiopluginvrunity";
#endif

  // Listener handlers.
  [DllImport(pluginName)]
  private static extern void ProcessListener ([In, Out] float[] data, int bufferSize);

  [DllImport(pluginName)]
  private static extern void SetListenerGain (float gain);

  [DllImport(pluginName)]
  private static extern void SetListenerTransform (float px, float py, float pz, float qx, float qy,
                                                   float qz, float qw);

  // Source handlers.
  [DllImport(pluginName)]
  private static extern int CreateSource (bool enableHrtf);

  [DllImport(pluginName)]
  private static extern void DestroySource (int sourceId);

#if !USE_SPATIALIZER_PLUGIN
  [DllImport(pluginName)]
  private static extern void ProcessSource (int sourceId, int bufferSize, [In, Out] float[] input,
                                            [In, Out] float[] output);
#endif

  [DllImport(pluginName)]
  private static extern void SetSourceDirectivity (int sourceId, float alpha, float order);

  [DllImport(pluginName)]
  private static extern void SetSourceDistanceAttenuationMethod (int sourceId,
                                                                 AudioRolloffMode rolloffMode,
                                                                 float minDistance,
                                                                 float maxDistance);

  [DllImport(pluginName)]
  private static extern void SetSourceGain (int sourceId, float gain);

  [DllImport(pluginName)]
  private static extern void SetSourceOcclusionIntensity (int sourceId, float intensity);

  [DllImport(pluginName)]
  private static extern void SetSourceTransform (int sourceId, float px, float py, float pz,
                                                 float qx, float qy, float qz, float qw,
                                                 float scale);

  // Room handlers.
  [DllImport(pluginName)]
  private static extern int CreateRoom ();

  [DllImport(pluginName)]
  private static extern void DestroyRoom (int roomId);

  [DllImport(pluginName)]
  private static extern void SetRoomProperties (int roomId, float px, float py, float pz, float qx,
                                                float qy, float qz, float qw, float dx, float dy,
                                                float dz,
                                                CardboardAudioRoom.SurfaceMaterial[] materialNames,
                                                float reflectionScalar, float reverbGain,
                                                float reverbBrightness, float reverbTime);

  // System handlers.
  [DllImport(pluginName)]
  private static extern void Initialize (Quality quality, int sampleRate, int numChannels,
                                         int framesPerBuffer);

  [DllImport(pluginName)]
  private static extern void Shutdown ();
}
