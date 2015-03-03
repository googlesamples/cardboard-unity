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

public class CardboardGUIMouse : MonoBehaviour {

  // If the app supports the use of some other pointing device, e.g. a gamepad
  // or bluetooth mouse, then this field can be left null.  When set to a
  // CardboardHead instance, then the user's head in effect becomes the mouse
  // pointer and the Cardboard trigger becomes the mouse button.  The user
  // looks at a GUI button and pulls the trigger to click it.
  [Tooltip("The CardboardHead which drives the simulated mouse.")]
  public CardboardHead head;

  // The image to draw into the captured GUI texture representing the current
  // mouse position.
  [Tooltip("What to draw on the GUI surface for the simulated mouse pointer.")]
  public Texture pointerImage;

  [Tooltip("The size to draw the pointer in screen coordinates. " +
           "Leave at 0,0 to use actual image size.")]
  public Vector2 pointerSize;

  [Tooltip("The screen pixel of the image to position over the mouse point.")]
  public Vector2 pointerSpot;

  // Whether to draw a pointer on the GUI surface, so the user can see
  // where they are about to click.
  private bool pointerVisible;

  void LateUpdate() {
    if (head == null) {  // Pointer not being controlled by user's head, so we bail.
      pointerVisible = true;  // Draw pointer wherever Unity thinks the mouse is.
      return;
    }
    if (!CardboardGUI.IsGUIVisible) {
      pointerVisible = false;  // No GUI == no pointer to worry about.
      return;
    }
    // Find which CardboardGUIWindow the user's gaze intersects first, if any.
    Ray ray = head.Gaze;
    CardboardGUIWindow hitWindow = null;
    float minDist = Mathf.Infinity;
    Vector2 pos = Vector2.zero;
    foreach (var guiWindow in GetComponentsInChildren<CardboardGUIWindow>()) {
      RaycastHit hit;
      if (guiWindow.GetComponent<Collider>().Raycast(ray, out hit, Mathf.Infinity)
          && hit.distance < minDist) {
        minDist = hit.distance;
        hitWindow = guiWindow;
        pos = hit.textureCoord;
      }
    }
    if (hitWindow == null) {
      // Don't draw the pointer unless the user is looking at a pixel of the GUI screen.
      pointerVisible = false;
      return;
    }
    // Convert from the intersected window's texture coordinates to screen coordinates.
    pos.x = hitWindow.rect.xMin + hitWindow.rect.width * pos.x;
    pos.y = hitWindow.rect.yMin + hitWindow.rect.height * pos.y;
    int x = (int)(pos.x * Screen.width);
    // Unity GUI Y-coordinates ascend top-to-bottom, as do the quad's texture coordinates,
    // while screen Y-coordinates ascend bottom-to-top.
    int y = (int)((1 - pos.y) * Screen.height);
    // Send the necessary event to Unity - next frame it will update the mouse.
    if (Cardboard.SDK.CardboardTriggered) {
      Cardboard.SDK.InjectMouseClick(x, y);
    } else {
      Cardboard.SDK.InjectMouseMove(x, y);
    }
    // OK to draw the pointer image.
    pointerVisible = true;
  }

  // Draw the fake mouse pointer.  Called by CardboardGUI after the rest of the UI is done.
  public void DrawPointerImage() {
    if (pointerImage == null || !pointerVisible || !enabled) {
      return;
    }
    Vector2 pos = (Vector2)Input.mousePosition;
    Vector2 spot = pointerSpot;
    Vector2 size = pointerSize;
    if (size.sqrMagnitude < 1) {  // If pointerSize was left == 0, just use size of image.
      size.x = pointerImage.width;
      size.y = pointerImage.height;
    }
    GUI.DrawTexture(new Rect(pos.x - spot.x, Screen.height - pos.y - spot.y, size.x, size.y),
                    pointerImage, ScaleMode.StretchToFill);
  }
}
