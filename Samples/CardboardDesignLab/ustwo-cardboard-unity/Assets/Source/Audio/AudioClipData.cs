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
/// An audio clip data wrapper to hold a base volume level - this allows designers to 'fix' audio that is too loud everywhere, particularly when clips get replaced.
/// This volume gets multiplied by subsequent volumes.
/// </summary>
public class AudioClipData : ScriptableObject {

	public AudioClip clip;
	public float volume;
}
