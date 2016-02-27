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

/// <summary>
/// A simple FPS counter that writes to a text mesh.  Averages the past 60 samples to determine framerate
/// </summary>
public class FPSDisplay : MonoBehaviour {

	[SerializeField] TextMesh text;

	float[] samples = new float[60];
	int sampleIdx = 0;


	void Start () {

		text.text = "FPS: ";
	}
	

	void Update () {
		samples[sampleIdx] = Time.time;
		sampleIdx = (sampleIdx + 1)%60;

		if(sampleIdx == 0){
			///this is the current minus the previous
			float dt = Time.time - samples[sampleIdx];

			int fps = (int)(60.0f / dt);
			text.text = "FPS: " + fps;
		}

	}
}
