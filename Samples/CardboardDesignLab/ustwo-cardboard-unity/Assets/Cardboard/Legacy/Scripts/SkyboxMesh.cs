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

// Original code by Nora
// http://stereoarts.jp
//
// Retrieved from:
// https://developer.oculusvr.com/forums/viewtopic.php?f=37&t=844#p28982
//
// Modified by Google:
//   Combined the 6 separate meshes into a single mesh with submeshes.

using UnityEngine;

/// @ingroup LegacyScripts
/// This script builds a stereo correct version of the skybox.
///
/// Unity 4's built-in skybox backgrounds do not work correctly for stereo rendering.
/// Since the same exact image is rendered to each eye, the background has zero
/// parallax.  Given the optics of Cardboard, this yields an effective stereo depth
/// that is likely right in the middle, or worse, front of the scene.  The results
/// in a visually painful parallax clash with other objects in the scene.
///
/// This script should be attached to the Camera which represents the primary
/// viewpoint of the player (such as _Main Camera_).  At runtime it builds a mesh that
/// will be textured with the camera's skybox material.  The mesh is scaled up to
/// just fit inside the far clipping plane, and kept centered on the camera's
/// position.
[RequireComponent(typeof(Camera))]
public class SkyboxMesh : MonoBehaviour {
#if UNITY_5
  void Awake() {
    Debug.Log("SkyboxMesh is not needed in Unity 5");
    Component.Destroy(this);
  }
#else
  /// The overall shape of the generated sky mesh.
  public enum Shape {
    Sphere,
    Cube,
  }

  /// The overall shape of the generated sky mesh.  The mesh has 6 sides regardless
  /// of shape, one for each texture in the skybox material.  The `shape` simply
  /// determines whether those sides are convex or flat.
  public Shape shape = Shape.Sphere;

  /// Controls the mesh resolution:  Each side of the mesh is an NxN grid of
  /// triangles, where N = `segments`.
  public int segments = 32;

  /// The skybox mesh will be set to this layer.  If you wish for only certain
  /// cameras to see the skybox mesh, choose a layer for it and make sure that
  /// only those cameras render that layer.
  public int layer = 0;

  private GameObject skybox;

  void Awake() {
    var sky = GetComponent<Skybox>();
    Material skymat = sky != null ? sky.material : RenderSettings.skybox;
    if (skymat == null) {
      enabled = false;
      return;
    }

    skybox = new GameObject(skymat.name + "SkyMesh");
    skybox.transform.parent = transform;
    skybox.transform.localPosition = Vector3.zero;
    skybox.layer = layer;

    var filter = skybox.AddComponent<MeshFilter>();
    filter.mesh = _CreateSkyboxMesh();

    Material mat = new Material(Shader.Find("Cardboard/SkyboxMesh"));
    var render = skybox.AddComponent<MeshRenderer>();
    render.sharedMaterials = new Material[] {
        new Material(mat) { mainTexture = skymat.GetTexture("_FrontTex") },
        new Material(mat) { mainTexture = skymat.GetTexture("_LeftTex")  },
        new Material(mat) { mainTexture = skymat.GetTexture("_BackTex")  },
        new Material(mat) { mainTexture = skymat.GetTexture("_RightTex") },
        new Material(mat) { mainTexture = skymat.GetTexture("_UpTex")    },
        new Material(mat) { mainTexture = skymat.GetTexture("_DownTex")  }
    };
    render.castShadows = false;
    render.receiveShadows = false;
  }

  void LateUpdate() {
    var camera = GetComponent<Camera>();
    // Only visible if the owning camera needs it.
    skybox.GetComponent<Renderer>().enabled =
      camera.enabled && (camera.clearFlags == CameraClearFlags.Skybox);
    // Scale up to just fit inside the far clip plane.
    Vector3 scale = transform.lossyScale;
    float far = camera.farClipPlane * 0.95f;
    if (shape == Shape.Cube) {
      far /= Mathf.Sqrt(3);  // Corners need to fit inside the frustum.
    }
    scale.x = far / scale.x;
    scale.y = far / scale.y;
    scale.z = far / scale.z;
    // Center on the camera.
    skybox.transform.localPosition = Vector3.zero;
    skybox.transform.localScale = scale;
    // Keep orientation fixed in the world.
    skybox.transform.rotation = Quaternion.identity;
  }

  Mesh _CreateMesh() {
    Mesh mesh = new Mesh();

    int hvCount2 = this.segments + 1;
    int hvCount2Half = hvCount2 / 2;
    int numVertices = hvCount2 * hvCount2;
    int numTriangles = this.segments * this.segments * 6;
    Vector3[] vertices = new Vector3[numVertices];
    Vector2[] uvs = new Vector2[numVertices];
    int[] triangles = new int[numTriangles];

    float scaleFactor = 2.0f / (float)this.segments;
    float uvFactor = 1.0f / (float)this.segments;

    if (this.segments <= 1 || this.shape == Shape.Cube) {
      float ty = 0.0f, py = -1.0f;
      int index = 0;
      for (int y = 0; y < hvCount2; ++y, ty += uvFactor, py += scaleFactor) {
        float tx = 0.0f, px = -1.0f;
        for (int x = 0; x < hvCount2; ++x, ++index, tx += uvFactor, px += scaleFactor) {
          vertices[index] = new Vector3(px, py, 1.0f);
          uvs[index] = new Vector2(tx, ty);
        }
      }
    } else {
      float ty = 0.0f, py = -1.0f;
      int index = 0, indexY = 0;
      for (int y = 0; y <= hvCount2Half;
          ++y, indexY += hvCount2, ty += uvFactor, py += scaleFactor) {
        float tx = 0.0f, px = -1.0f, py2 = py * py;
        int x = 0;
        for (; x <= hvCount2Half; ++x, ++index, tx += uvFactor, px += scaleFactor) {
          float d = Mathf.Sqrt(px * px + py2 + 1.0f);
          float theta = Mathf.Acos(1.0f / d);
          float phi = Mathf.Atan2(py, px);
          float sinTheta = Mathf.Sin(theta);
          vertices[index] = new Vector3(
              sinTheta * Mathf.Cos(phi),
              sinTheta * Mathf.Sin(phi),
              Mathf.Cos(theta));
          uvs[index] = new Vector2(tx, ty);
        }
        int indexX = hvCount2Half - 1;
        for (; x < hvCount2; ++x, ++index, --indexX, tx += uvFactor, px += scaleFactor) {
          Vector3 v = vertices[indexY + indexX];
          vertices[index] = new Vector3(-v.x, v.y, v.z);
          uvs[index] = new Vector2(tx, ty);
        }
      }
      indexY = (hvCount2Half - 1) * hvCount2;
      for (int y = hvCount2Half + 1; y < hvCount2;
          ++y, indexY -= hvCount2, ty += uvFactor, py += scaleFactor) {
        float tx = 0.0f, px = -1.0f;
        int x = 0;
        for (; x <= hvCount2Half; ++x, ++index, tx += uvFactor, px += scaleFactor) {
          Vector3 v = vertices[indexY + x];
          vertices[index] = new Vector3(v.x, -v.y, v.z);
          uvs[index] = new Vector2(tx, ty);
        }
        int indexX = hvCount2Half - 1;
        for (; x < hvCount2; ++x, ++index, --indexX, tx += uvFactor, px += scaleFactor) {
          Vector3 v = vertices[indexY + indexX];
          vertices[index] = new Vector3(-v.x, -v.y, v.z);
          uvs[index] = new Vector2(tx, ty);
        }
      }
    }

    for (int y = 0, index = 0, ofst = 0; y < this.segments; ++y, ofst += hvCount2) {
      int y0 = ofst, y1 = ofst + hvCount2;
      for (int x = 0; x < this.segments; ++x, index += 6) {
        triangles[index + 0] = y0 + x;
        triangles[index + 1] = y1 + x;
        triangles[index + 2] = y0 + x + 1;
        triangles[index + 3] = y1 + x;
        triangles[index + 4] = y1 + x + 1;
        triangles[index + 5] = y0 + x + 1;
      }
    }

    mesh.vertices = vertices;
    mesh.uv = uvs;
    mesh.triangles = triangles;
    return mesh;
  }

  CombineInstance _CreatePlane(Mesh mesh, Quaternion rotation) {
    return new CombineInstance() {
        mesh = mesh,
        subMeshIndex = 0,
        transform = Matrix4x4.TRS(Vector3.zero, rotation, Vector3.one)};
  }

  Mesh _CreateSkyboxMesh() {
    Mesh mesh = _CreateMesh();
    var comb = new CombineInstance[] {
        _CreatePlane(mesh, Quaternion.identity),
        _CreatePlane(mesh, Quaternion.Euler(0.0f, 90.0f, 0.0f)),
        _CreatePlane(mesh, Quaternion.Euler(0.0f, 180.0f, 0.0f)),
        _CreatePlane(mesh, Quaternion.Euler(0.0f, 270.0f, 0.0f)),
        _CreatePlane(mesh, Quaternion.Euler(-90.0f, 0.0f, 0.0f)),
        _CreatePlane(mesh, Quaternion.Euler(90.0f, 0.0f, 0.0f))};
    Mesh skymesh = new Mesh();
    skymesh.CombineMeshes(comb, false, true);
    return skymesh;
  }
#endif
}
