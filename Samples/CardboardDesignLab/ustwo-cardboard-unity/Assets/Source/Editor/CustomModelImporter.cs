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

/*using UnityEngine;
using UnityEditor;
using System.Collections;

public class CustomModelImporter : AssetPostprocessor { 

	void OnPostprocessModel(GameObject go){
		Apply(go.transform);

	}
	void Apply (Transform trans) {
		MeshFilter mf = trans.GetComponent<MeshFilter>();
		if (mf != null){
			Mesh m = mf.sharedMesh;
			Vector3[] vertices = m.vertices;
			int[] triangles = m.triangles;
			Vector2[] uv = m.uv;
			Vector3[] normals = m.normals;
		}


		// Recurse
		foreach (Transform t in trans){
			Apply(t);
		}

	}
}*/
