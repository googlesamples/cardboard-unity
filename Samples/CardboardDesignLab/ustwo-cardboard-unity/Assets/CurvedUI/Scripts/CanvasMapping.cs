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
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(Canvas))]
public abstract class CanvasMapping : MonoBehaviour
{
	// Take a point on the screen and map it to the canvas
	public abstract bool MapScreenToCanvas(Vector2 screenCoord, out Vector2 o_canvasCoord);

	// Set parameters on a material to perform the canvas to screen mapping
	public abstract void SetCanvasToScreenParameters(Material material);

	protected virtual void Awake()
	{
		m_canvas = GetComponent<Canvas>();
	}

	protected virtual void Reset()
	{
		SetMaterialsDirty();
	}

	protected virtual void OnValidate()
	{
		SetMaterialsDirty();
	}

	protected void SetMaterialsDirty()
	{
		Graphic[] graphics = GetComponentsInChildren<Graphic>();
		foreach (Graphic graphic in graphics)
		{
			graphic.SetMaterialDirty();
		}
	}

	protected Canvas m_canvas;
}
