// Copyright 2017 Google Inc. All rights reserved.
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
using UnityEngine.EventSystems;

/// Draws a circular reticle in front of any object that the user points at.
/// The circle dilates if the object is clickable.
public class GvrReticlePointerImpl : GvrBasePointer {
  // The constants below are expsed for testing.
  // Minimum inner angle of the reticle (in degrees).
  public const float RETICLE_MIN_INNER_ANGLE = 0.0f;
  // Minimum outer angle of the reticle (in degrees).
  public const float RETICLE_MIN_OUTER_ANGLE = 0.5f;
  // Angle at which to expand the reticle when intersecting with an object
  // (in degrees).
  public const float RETICLE_GROWTH_ANGLE = 1.5f;

  // Minimum distance of the reticle (in meters).
  public const float RETICLE_DISTANCE_MIN = 0.45f;
  // Maximum distance of the reticle (in meters).
  public const float RETICLE_DISTANCE_MAX = 10.0f;

  // The time it takes for the reticle to reach its maximum size
  public float TimeToTargetReticleDiameter { private get; set; }

  // The time since the reticle's target size was last changed 
  private float timeSinceTargetChanged = 0.0f;

  public Material MaterialComp { private get; set; }

  // Current inner angle of the reticle (in degrees).
  // Exposed for testing.
  public float ReticleInnerAngle { get; private set; }

  // Current outer angle of the reticle (in degrees).
  // Exposed for testing.
  public float ReticleOuterAngle { get; private set; }

  // Current distance of the reticle (in meters).
  // Getter exposed for testing.
  public float ReticleDistanceInMeters { get; private set; }

  // Current inner and outer diameters of the reticle, before distance multiplication.
  // Getters exposed for testing.
  public float ReticleInnerDiameter { get; private set; }

  public float ReticleOuterDiameter { get; private set; }

  private Vector3 targetPoint = Vector3.zero;
  public override Vector3 LineEndPoint { get { return targetPoint; } }

  public override float MaxPointerDistance { get { return RETICLE_DISTANCE_MAX; } }

  public GvrReticlePointerImpl() {
    TimeToTargetReticleDiameter = 2.0f;
    ReticleInnerAngle = 0.0f;
    ReticleOuterAngle = 0.5f;
    ReticleDistanceInMeters = 10.0f;
    ReticleInnerDiameter = 0.0f;
    ReticleOuterDiameter = 0.0f;
  }

  public override void OnStart () {
    base.OnStart();
  }

  /// This is called when the 'BaseInputModule' system should be enabled.
  public override void OnInputModuleEnabled() {}

  /// This is called when the 'BaseInputModule' system should be disabled.
  public override void OnInputModuleDisabled() {}

  /// Called when the user is pointing at valid GameObject. This can be a 3D
  /// or UI element.
  ///
  /// The targetObject is the object the user is pointing at.
  /// The intersectionPosition is where the ray intersected with the targetObject.
  /// The intersectionRay is the ray that was cast to determine the intersection.
  public override void OnPointerEnter(RaycastResult rayastResult, Ray ray,
    bool isInteractive) {
    timeSinceTargetChanged = 0.0f;
    SetPointerTarget(rayastResult.worldPosition, isInteractive);
  }

  /// Called every frame the user is still pointing at a valid GameObject. This
  /// can be a 3D or UI element.
  ///
  /// The targetObject is the object the user is pointing at.
  /// The intersectionPosition is where the ray intersected with the targetObject.
  /// The intersectionRay is the ray that was cast to determine the intersection.
  public override void OnPointerHover(RaycastResult rayastResult, Ray ray,
    bool isInteractive) {
    SetPointerTarget(rayastResult.worldPosition, isInteractive);
  }

  /// Called when the user's look no longer intersects an object previously
  /// intersected with a ray projected from the camera.
  /// This is also called just before **OnInputModuleDisabled** and may have have any of
  /// the values set as **null**.
  public override void OnPointerExit(GameObject previousObject) {
    timeSinceTargetChanged = 0.0f;
    ReticleDistanceInMeters = RETICLE_DISTANCE_MAX;
    ReticleInnerAngle = RETICLE_MIN_INNER_ANGLE;
    ReticleOuterAngle = RETICLE_MIN_OUTER_ANGLE;
  }

  /// Called when a trigger event is initiated. This is practically when
  /// the user begins pressing the trigger.
  public override void OnPointerClickDown() {}

  /// Called when a trigger event is finished. This is practically when
  /// the user releases the trigger.
  public override void OnPointerClickUp() {}

  public override void GetPointerRadius(out float enterRadius, out float exitRadius) {
    float min_inner_angle_radians = Mathf.Deg2Rad * RETICLE_MIN_INNER_ANGLE;
    float max_inner_angle_radians = Mathf.Deg2Rad * (RETICLE_MIN_INNER_ANGLE + RETICLE_GROWTH_ANGLE);

    enterRadius = 2.0f * Mathf.Tan(min_inner_angle_radians);
    exitRadius = 2.0f * Mathf.Tan(max_inner_angle_radians);
  }

  public void UpdateDiameters() {
    ReticleDistanceInMeters =
      Mathf.Clamp(ReticleDistanceInMeters, RETICLE_DISTANCE_MIN, RETICLE_DISTANCE_MAX);

    if (ReticleInnerAngle < RETICLE_MIN_INNER_ANGLE) {
      ReticleInnerAngle = RETICLE_MIN_INNER_ANGLE;
    }

    if (ReticleOuterAngle < RETICLE_MIN_OUTER_ANGLE) {
      ReticleOuterAngle = RETICLE_MIN_OUTER_ANGLE;
    }

    float inner_half_angle_radians = Mathf.Deg2Rad * ReticleInnerAngle * 0.5f;
    float outer_half_angle_radians = Mathf.Deg2Rad * ReticleOuterAngle * 0.5f;

    float inner_diameter = 2.0f * Mathf.Tan(inner_half_angle_radians);
    float outer_diameter = 2.0f * Mathf.Tan(outer_half_angle_radians);

    timeSinceTargetChanged += Time.deltaTime;
    float reticleLerpProportion = timeSinceTargetChanged/TimeToTargetReticleDiameter;
    
    ReticleInnerDiameter =
        Mathf.Lerp(ReticleInnerDiameter, inner_diameter, reticleLerpProportion);
    ReticleOuterDiameter =
        Mathf.Lerp(ReticleOuterDiameter, outer_diameter, reticleLerpProportion);

    MaterialComp.SetFloat("_InnerDiameter", ReticleInnerDiameter * ReticleDistanceInMeters);
    MaterialComp.SetFloat("_OuterDiameter", ReticleOuterDiameter * ReticleDistanceInMeters);
    MaterialComp.SetFloat("_DistanceInMeters", ReticleDistanceInMeters);
  }

  private bool SetPointerTarget(Vector3 target, bool interactive) {
    if (base.PointerTransform == null) {
      Debug.LogWarning("Cannot operate on a null pointer transform");
      return false;
    }
    targetPoint = target;
    Vector3 targetLocalPosition = base.PointerTransform.InverseTransformPoint(target);

    ReticleDistanceInMeters =
        Mathf.Clamp(targetLocalPosition.z, RETICLE_DISTANCE_MIN, RETICLE_DISTANCE_MAX);
    if (interactive) {
      ReticleInnerAngle = RETICLE_MIN_INNER_ANGLE + RETICLE_GROWTH_ANGLE;
      ReticleOuterAngle = RETICLE_MIN_OUTER_ANGLE + RETICLE_GROWTH_ANGLE;
    } else {
      ReticleInnerAngle = RETICLE_MIN_INNER_ANGLE;
      ReticleOuterAngle = RETICLE_MIN_OUTER_ANGLE;
    }
    return true;
  }
}
