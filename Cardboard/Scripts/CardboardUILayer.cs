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

/// @ingroup Scripts
public class CardboardUILayer {
  // The gear has 6 identical sections, each spanning 60 degrees.
  const float kAnglePerGearSection = 60;

  // Half-angle of the span of the outer rim.
  const float kOuterRimEndAngle = 12;

  // Angle between the middle of the outer rim and the start of the inner rim.
  const float kInnerRimBeginAngle = 20;

  // Distance from center to outer rim, normalized so that the entire model
  // fits in a [-1, 1] x [-1, 1] square.
  const float kOuterRadius = 1;

  // Distance from center to depressed rim, in model units.
  const float kMiddleRadius = 0.75f;

  // Radius of the inner hollow circle, in model units.
  const float kInnerRadius = 0.3125f;

  // Center line thickness in DP.
  const float kCenterLineThicknessDp = 4;

  // Button width in DP.
  const int kButtonWidthDp = 28;

  // Factor to scale the touch area that responds to the touch.
  const float kTouchSlopFactor = 1.5f;

  static readonly float[] Angles = {
    0, kOuterRimEndAngle, kInnerRimBeginAngle,
    kAnglePerGearSection - kInnerRimBeginAngle, kAnglePerGearSection - kOuterRimEndAngle
  };

  private Material material;
  private float centerWidthPx;
  private float buttonWidthPx;
  private float xScale;
  private float yScale;
  private Matrix4x4 xfm;

  private void ComputeMatrix() {
    centerWidthPx = kCenterLineThicknessDp / 160.0f * DisplayMetricsAndroid.XDPI / 2;
    buttonWidthPx = kButtonWidthDp / 160.0f * DisplayMetricsAndroid.XDPI / 2;
    xScale = buttonWidthPx / Screen.width;
    yScale = buttonWidthPx / Screen.height;
    xfm = Matrix4x4.TRS(new Vector3(0.5f, yScale, 0), Quaternion.identity,
        new Vector3(xScale, yScale, 1));
  }

  public CardboardUILayer() {
    material = new Material(Shader.Find("Cardboard/SolidColor"));
    material.color = new Color(0.8f, 0.8f, 0.8f);
    if (!Application.isEditor) {
      ComputeMatrix();
    }
  }

  public void Draw() {
    if (Cardboard.SDK.NativeUILayerSupported) {
      return;
    }
    GL.Clear(true, false, Color.black);
    material.SetPass(0);
    if (Application.isEditor) {
      ComputeMatrix();
    }
    if (Cardboard.SDK.EnableAlignmentMarker) {
      int x = Screen.width / 2;
      int w = (int)centerWidthPx;
      int h = (int)(2 * kTouchSlopFactor * buttonWidthPx);
      GL.PushMatrix();
      GL.LoadPixelMatrix();
      GL.Begin(GL.QUADS);
      GL.Vertex3(x - w, h, 0);
      GL.Vertex3(x - w, Screen.height - h, 0);
      GL.Vertex3(x + w, Screen.height - h, 0);
      GL.Vertex3(x + w, h, 0);
      GL.End();
      GL.PopMatrix();
    }
#if UNITY_EDITOR
    if (Cardboard.SDK.EnableSettingsButton) {
      GL.PushMatrix();
      GL.LoadOrtho();
      GL.MultMatrix(xfm);
      GL.Begin(GL.TRIANGLE_STRIP);
      for (int i = 0, n = Angles.Length * 6; i <= n; i++) {
        float theta = (i / Angles.Length) * kAnglePerGearSection + Angles[i % Angles.Length];
        float angle = (90 - theta) * Mathf.Deg2Rad;
        float x = Mathf.Cos(angle);
        float y = Mathf.Sin(angle);
        float mod = Mathf.PingPong(theta, kAnglePerGearSection / 2);
        float lerp = (mod - kOuterRimEndAngle) / (kInnerRimBeginAngle - kOuterRimEndAngle);
        float r = Mathf.Lerp(kOuterRadius, kMiddleRadius, lerp);
        GL.Vertex3(kInnerRadius * x, kInnerRadius * y, 0);
        GL.Vertex3(r * x, r * y, 0);
      }
      GL.End();
      GL.PopMatrix();
    }
#endif
  }
}
