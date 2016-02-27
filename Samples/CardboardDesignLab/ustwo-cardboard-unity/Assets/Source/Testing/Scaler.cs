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
/// Another test script, this one scales things
/// </summary>
public class Scaler : MonoBehaviour {

	public float scale=1;

	void Start(){
		float invScale = 1.0f/scale;

		transform.localScale *= scale;
		MeshRenderer[] meshes = gameObject.GetComponentsInChildren<MeshRenderer>();
		for(int i=0; i<meshes.Length; i++){
			meshes[i].transform.localScale *= invScale;
		}
	}
}
