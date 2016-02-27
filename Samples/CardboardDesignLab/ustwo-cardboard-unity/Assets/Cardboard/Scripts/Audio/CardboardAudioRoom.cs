// Copyright 2015 Google Inc. All rights reserved.
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

/// Cardboard audio room component that simulates environmental effects of a room with respect to
/// the properties of the attached game object.
[AddComponentMenu("Cardboard/Audio/CardboardAudioRoom")]
public class CardboardAudioRoom : MonoBehaviour {
  /// Material type that determines the acoustic properties of a room surface.
  public enum SurfaceMaterial {
    Transparent = 0,
    AcousticCeilingTiles = 1,
    BrickBare = 2,
    BrickPainted = 3,
    ConcreteBlockCoarse = 4,
    ConcreteBlockPainted = 5,
    CurtainHeavy = 6,
    FiberglassInsulation = 7,
    GlassThin = 8,
    GlassThick = 9,
    Grass = 10,
    LinoleumOnConcrete = 11,
    Marble = 12,
    ParquetOnConcrete = 13,
    PlasterRough = 14,
    PlasterSmooth = 15,
    PlywoodPanel = 16,
    PolishedConcreteOrTile = 17,
    Sheetrock = 18,
    WaterOrIceSurface = 19,
    WoodCeiling = 20,
    WoodPanel = 21
  }

  /// Room surface material in negative x direction.
  public SurfaceMaterial leftWall = SurfaceMaterial.ConcreteBlockCoarse;

  /// Room surface material in positive x direction.
  public SurfaceMaterial rightWall = SurfaceMaterial.ConcreteBlockCoarse;

  /// Room surface material in negative y direction.
  public SurfaceMaterial floor = SurfaceMaterial.ParquetOnConcrete;

  /// Room surface material in positive y direction.
  public SurfaceMaterial ceiling = SurfaceMaterial.PlasterRough;

  /// Room surface material in negative z direction.
  public SurfaceMaterial backWall = SurfaceMaterial.ConcreteBlockCoarse;

  /// Room surface material in positive z direction.
  public SurfaceMaterial frontWall = SurfaceMaterial.ConcreteBlockCoarse;

  /// Reflectivity scalar for each surface of the room.
  public float reflectivity = 1.0f;

  /// Reverb gain modifier in decibels.
  public float reverbGainDb = 0.0f;

  /// Reverb brightness modifier.
  public float reverbBrightness = 0.0f;

  /// Reverb time modifier.
  public float reverbTime = 1.0f;

  /// Size of the room (normalized with respect to scale of the game object).
  public Vector3 size = Vector3.one;

  /// Unique room id.
  private int id = -1;

  /// Surface materials holder.
  private SurfaceMaterial[] surfaceMaterials = null;

  void Awake () {
    surfaceMaterials = new SurfaceMaterial[CardboardAudio.numRoomSurfaces];
  }

  void OnEnable () {
    InitializeRoom();
  }

  void Start () {
    InitializeRoom();
  }

  void OnDisable () {
    ShutdownRoom();
  }

  void Update () {
    CardboardAudio.UpdateAudioRoom(id, transform, GetSurfaceMaterials(), reflectivity, reverbGainDb,
                                   reverbBrightness, reverbTime, size);
  }

  /// Returns a list of surface materials of the room.
  public SurfaceMaterial[] GetSurfaceMaterials () {
    surfaceMaterials[0] = leftWall;
    surfaceMaterials[1] = rightWall;
    surfaceMaterials[2] = floor;
    surfaceMaterials[3] = ceiling;
    surfaceMaterials[4] = backWall;
    surfaceMaterials[5] = frontWall;
    return surfaceMaterials;
  }

  private void InitializeRoom () {
    if (id < 0) {
      id = CardboardAudio.CreateAudioRoom();
      CardboardAudio.UpdateAudioRoom(id, transform, GetSurfaceMaterials(), reflectivity,
                                     reverbGainDb, reverbBrightness, reverbTime, size);
    }
  }

  private void ShutdownRoom () {
    if (id >= 0) {
      CardboardAudio.DestroyAudioRoom(id);
      id = -1;
    }
  }

  void OnDrawGizmosSelected () {
    // Draw shoebox model wireframe of the room.
    Gizmos.color = Color.yellow;
    Gizmos.matrix = transform.localToWorldMatrix;
    Gizmos.DrawWireCube(Vector3.zero, size);
  }
}
