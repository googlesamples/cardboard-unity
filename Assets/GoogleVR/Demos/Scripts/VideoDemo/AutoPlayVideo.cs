
// <copyright file="AutoPlayVideo.cs" company="Google Inc.">
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
// </copyright>

namespace GoogleVR.VideoDemo {
  using UnityEngine;

  /// <summary>
  /// Auto play video.
  /// </summary>
  /// <remarks>This script exposes a delay value in seconds to start playing the TexturePlayer
  /// component on the same object.
  /// </remarks>

  [RequireComponent(typeof(GvrVideoPlayerTexture))]
  public class AutoPlayVideo : MonoBehaviour {
    private bool done;
    private float t;
    private GvrVideoPlayerTexture player;

    public float delay = 2f;
    public bool loop = false;

    void Start() {
      t = 0;
      done = false;
      player = GetComponent<GvrVideoPlayerTexture>();
      if (player != null) {
        player.Init();
      }
    }

    void Update() {
      if (player == null) {
        return;
      } else if (player.PlayerState == GvrVideoPlayerTexture.VideoPlayerState.Ended && done && loop) {
        player.Pause();
        player.CurrentPosition = 0;
        done = false;
        t = 0f;
        return;
      }
      if (done) {
        return;
      }

      t += Time.deltaTime;
      if (t >= delay && player != null && player.Play()) {
        done = true;
      }
    }
  }
}
