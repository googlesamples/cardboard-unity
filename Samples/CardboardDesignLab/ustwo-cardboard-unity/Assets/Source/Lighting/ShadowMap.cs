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

[ExecuteInEditMode]
public class ShadowMap : MonoBehaviour {

	///lets only keep one of these around, eh?
	static RenderTexture rt;


	[SerializeField] new Camera camera;
	[SerializeField] Shader depthShader;
	static int shadowMapResolution = 1024;
	
	int lastShadowFrame = -1;

	void Start(){
		if(rt == null){
			rt = new RenderTexture(shadowMapResolution, shadowMapResolution, 16, RenderTextureFormat.ARGB32);
		}
		//rt.useMipMap = true;
		camera.targetTexture = rt;
		//camera.depthTextureMode = DepthTextureMode.Depth;
		camera.enabled = true;
		Shader.SetGlobalTexture("_ShadowDepth", rt);
		camera.SetReplacementShader(depthShader, "RenderType");
		camera.backgroundColor = Color.black;
	}

	void OnDestroy(){
		//if(rt != null){
		//	rt.Release();
		//	DestroyImmediate(rt);
		//}
		
	}

	public bool singleFrameMode = true;
	bool recalculate = true;

	public void SetFieldOfView(float val){
		if(val != camera.fieldOfView){
			camera.fieldOfView = val;
			Recalculate();

		}
		
	}

	public void SetNearFarClip(float near, float far){
		if(near != camera.nearClipPlane || far != camera.farClipPlane){
			camera.nearClipPlane = near;
			camera.farClipPlane = far;
			Recalculate();
		}
	}

	public void Recalculate(){
		recalculate = true;
		lastShadowFrame = Time.frameCount;
	}

	void Update () {
		#if UNITY_EDITOR
			Shader.SetGlobalMatrix("_ShadowMatrix",camera.projectionMatrix*camera.worldToCameraMatrix);
			camera.enabled = true;
		#endif

		if(!singleFrameMode || recalculate){
			Shader.SetGlobalMatrix("_ShadowMatrix",camera.projectionMatrix*camera.worldToCameraMatrix);
			camera.enabled = true;

		}
	}

	void OnPostRender(){
		if(singleFrameMode && Time.frameCount > lastShadowFrame){
			recalculate = false;
			camera.enabled = false;
		}
	}

}
