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
using UnityEditor;
using System.Collections;

[CustomEditor(typeof(TimeOfDayLighting))]
[CanEditMultipleObjects]
public class TimeOfDayLightingEditor : Editor {


	public override void OnInspectorGUI() 
	{
		base.OnInspectorGUI();
		TimeOfDayLighting td = target as TimeOfDayLighting;
		if(GUILayout.Button("Paste Data")){
			td.TimeSetting = LightingEditor.lightingValues;
			EditorUtility.SetDirty(td);
		}
	}
	
	[MenuItem("Assets/Create/TimeOfDayLighting")] 
    public static void CreateLighting() { 
		CustomAssetMaker.CreateAsset<TimeOfDayLighting>("New TimeOfDayLighting");			
    }
}
