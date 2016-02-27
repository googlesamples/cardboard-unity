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

[RequireComponent(typeof(StereoController))]
public class StereoCamera : MonoBehaviour {

	static StereoCamera instance;
	public static StereoCamera Instance{
		get{
			return instance;
		}
	}

	[SerializeField] new Camera camera;
	[SerializeField] CardboardEye leftEye;
	[SerializeField] CardboardEye rightEye;

	[SerializeField] Transform environment;

  private StereoController controller;

	void Awake(){
		instance = this;
    controller = GetComponent<StereoController>();
	}

	public float FarClipPlane{
		get{
			return camera.farClipPlane;
		}
		set{
			camera.farClipPlane = value;
      controller.UpdateStereoValues();
		}
	}

	public Vector3 EnvironmentScale{
		get{
			return environment.localScale;
		}
		set{
			environment.localScale = value;
		}
	}

}
