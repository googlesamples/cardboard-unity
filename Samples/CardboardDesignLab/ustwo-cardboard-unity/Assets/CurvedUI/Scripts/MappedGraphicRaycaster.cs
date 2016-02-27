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
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(CanvasMapping))]
public class MappedGraphicRaycaster : GraphicRaycaster
{
	protected override void Awake()
	{
		base.Awake();
		m_mapping = GetComponent<CanvasMapping>();
	}

	public override void Raycast(PointerEventData eventData, List<RaycastResult> resultAppendList)
	{
		// Remap position
		Vector2 remappedPosition;
		if (!m_mapping.MapScreenToCanvas(eventData.position, out remappedPosition))
		{
			// Invalid, do nothing
			return;
		}

		// Update event data
		eventData.position = remappedPosition;

		// Use base class raycast method
		base.Raycast(eventData, resultAppendList);
	}

	CanvasMapping m_mapping;
}
