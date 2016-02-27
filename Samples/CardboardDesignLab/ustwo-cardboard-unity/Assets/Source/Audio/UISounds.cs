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
/// Just a static place to keep global UI sound effects
/// </summary>
public class UISounds : MonoBehaviour {

	static UISounds instance;
	public static UISounds Instance{
		get{
			return instance;
		}
	}

	[SerializeField] AudioSource complete;
	[SerializeField] AudioSource click;

	void Awake () {
		instance = this;
	}
	
	public void PlayComplete(){
		complete.Play();
	}

	public void PlayClick(){
		click.Play();
	}
}
