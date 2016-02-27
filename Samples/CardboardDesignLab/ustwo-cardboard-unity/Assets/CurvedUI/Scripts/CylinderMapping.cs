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

#define DEBUG_VISUALISE

using UnityEngine;
using System.Collections;

public class CylinderMapping : CanvasMapping
{
	protected override void Awake()
	{
		base.Awake();

		if (m_canvas.renderMode != RenderMode.ScreenSpaceCamera)
		{
			Debug.LogWarning("Cylinder mapping works best in ScreenSpaceCamera mode", this);
		}
	}
	#region CanvasMapping
	
	public override bool MapScreenToCanvas(Vector2 screenCoord, out Vector2 o_canvasCoord)
	{
		Camera worldCamera = m_canvas.worldCamera;
		if (worldCamera != null)
		{
			// Get the camera transform
			Transform worldCameraTransform = worldCamera.transform;

			// Get a ray from the camera through the point on the screen
			Ray ray3D = worldCamera.ScreenPointToRay(screenCoord);

			// Transform the ray direction into view space
			Vector3 localRayDirection = worldCameraTransform.InverseTransformDirection(ray3D.direction);

			// Calculate the view space size of the canvas
			float thetaFOVH = worldCamera.fieldOfView * 0.5f * Mathf.Deg2Rad;
			float tanFOVH = Mathf.Tan(thetaFOVH);
			float tanFOVW = tanFOVH * worldCamera.aspect;

			// Flatten cylinder and ray to 2D so this becomes a ray circle intersection
			Vector2 rayDirection2D = new Vector2(localRayDirection.x, localRayDirection.z);
			Vector2 circlePosition = new Vector2(0.0f, 1.0f + (tanFOVW * m_depth));
			float circleRadius = tanFOVW * m_radius;

			// Determine the far intersection, if there is one
			float farIntersection;
			if (RayCircle2D(Vector2.zero, rayDirection2D, circlePosition, circleRadius, out farIntersection))
			{
				// Intersection point on the XZ plane
				Vector2 cylinderXZ = (rayDirection2D * farIntersection) - circlePosition;

				// XZ -> angle around cylinder
				float cylinderTheta = Mathf.Atan2(cylinderXZ.x, cylinderXZ.y);

				// Y intersection
				float cylinderY = localRayDirection.y * farIntersection;

				// To viewport [-1, 1]
				float viewportX = cylinderTheta / (m_angle * 0.5f * Mathf.Deg2Rad);
				float viewportY = cylinderY / tanFOVH;

				// To canvas
				Vector2 canvasSize = m_canvas.pixelRect.size;
				o_canvasCoord.x = ((viewportX * 0.5f) + 0.5f) * canvasSize.x;
				o_canvasCoord.y = ((viewportY * 0.5f) + 0.5f) * canvasSize.y;

				#if DEBUG_VISUALISE
				{
					Vector3 intersection3D = worldCameraTransform.TransformPoint(localRayDirection * (farIntersection * m_canvas.planeDistance));
					Debug.DrawLine(worldCameraTransform.position, intersection3D);
				}
				#endif

				return true;
			}
		}

		o_canvasCoord = Vector2.zero;
		return false;
	}
	
	public override void SetCanvasToScreenParameters(Material material)
	{
		material.SetFloat("Cylinder_Depth", m_depth * 0.5f);
		material.SetFloat("Cylinder_Angle", m_angle * Mathf.Deg2Rad);
		material.SetFloat("Cylinder_Radius", m_radius * 0.5f);
	}
	
	#endregion

	bool RayCircle2D(Vector2 rayStart, Vector2 rayDirection, Vector2 circlePosition, float circleRadius, out float o_farIntersection)
	{
		Vector2 f = rayStart - circlePosition;
		
		float a = Vector2.Dot(rayDirection, rayDirection);
		float b = 2.0f * Vector2.Dot(f, rayDirection);
		float c = Vector2.Dot(f, f) - (circleRadius * circleRadius);
		
		float discriminantSq = (b * b) - (4.0f * a * c);
		if (discriminantSq < 0.0f)
		{
			// No intersection
			o_farIntersection = 0.0f;
			return false;
		}
		else
		{
			float discriminant = Mathf.Sqrt(discriminantSq);
			o_farIntersection = (-b + discriminant) / (2.0f * a);
			return true;
		}
	}

	[SerializeField]
	[Range(-10.0f, 10.0f)]
	float m_depth = -1.0f;

	[SerializeField]
	[Range(0.0f, 360.0f)]
	float m_angle = 180.0f;

	[SerializeField]
	[Range(0.0f, 10.0f)]
	float m_radius = 1.0f;
}
