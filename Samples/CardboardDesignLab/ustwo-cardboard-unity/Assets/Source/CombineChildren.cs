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

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]

/// <summary>
/// A script written intended for editor use to create a mesh from children, for batching purposes.  Not very robust, intended only for meshes using the same material.
/// </summary>
public class CombineChildren : MonoBehaviour {
    public Mesh GetCombineMesh() {
        MeshFilter[] meshFilters = GetComponentsInChildren<MeshFilter>();
        CombineInstance[] combine = new CombineInstance[meshFilters.Length -1];
        int i = 0;
        int idx = 0;
        Debug.Log(meshFilters.Length);
        while (i < meshFilters.Length) {
            if(meshFilters[i] != GetComponent<MeshFilter>()){
                combine[idx].mesh = meshFilters[i].sharedMesh;
                Debug.Log(meshFilters[i].sharedMesh);
                combine[idx].transform = transform.worldToLocalMatrix * meshFilters[i].transform.localToWorldMatrix;
                idx ++;
            }
            //meshFilters[i].gameObject.active = false;
            i++;
        }
        Mesh mesh = new Mesh();
        mesh.CombineMeshes(combine);
           
        return mesh;
    }

    
}