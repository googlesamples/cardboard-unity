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

using UnityEditor;
using UnityEngine;
using System.Collections;

[CustomEditor(typeof(CombineChildren))]
public class CombineChildrenEditor : Editor {

	public override void OnInspectorGUI() 
	{
		base.OnInspectorGUI();
		CombineChildren l = target as CombineChildren;

		//EditorUtility.SetDirty(l);

		if(GUILayout.Button("Bake Mesh")){
			Mesh m = l.GetCombineMesh();
			AssetDatabase.CreateAsset(m, "Assets/mesh.asset");
		}
	}
	
}
