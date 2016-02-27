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

using System;
using UnityEngine;

/// @cond
/// Measurements of a particular phone in a particular Cardboard device.
[System.Serializable]
public class CardboardProfile {
  public CardboardProfile Clone() {
    return new CardboardProfile {
      screen = this.screen,
      device = this.device
    };
  }

  /// Information about the screen.  All distances are in meters, measured as the phone is expected
  /// to be placed in the Cardboard, i.e. landscape orientation.
  [System.Serializable]
  public struct Screen {
    public float width;   // The long edge of the phone.
    public float height;  // The short edge of the phone.
    public float border;  // Distance from bottom of the cardboard to the bottom edge of screen.
  }

  /// Information about the lens placement in the Cardboard.  All distances are in meters.
  [System.Serializable]
  public struct Lenses {
    public float separation;     // Center to center.
    public float offset;         // Offset of lens center from top or bottom of cardboard.
    public float screenDistance; // Distance from lens center to the phone screen.

    public int alignment;  // Determines whether lenses are placed relative to top, bottom or
                           // center.  It is actually a signum (-1, 0, +1) relating the scale of
                           // the offset's coordinates to the device coordinates.

    public const int AlignTop = -1;    // Offset is measured down from top of device.
    public const int AlignCenter = 0;  // Center alignment ignores offset, hence scale is zero.
    public const int AlignBottom = 1;  // Offset is measured up from bottom of device.
  }

  /// Information about the viewing angles through the lenses.  All angles in degrees, measured
  /// away from the optical axis, i.e. angles are all positive.  It is assumed that left and right
  /// eye FOVs are mirror images, so that both have the same inner and outer angles.  Angles do not
  /// need to account for the limits due to screen size.
  [System.Serializable]
  public struct MaxFOV {
    public float outer;  // Towards the side of the screen.
    public float inner;  // Towards the center line of the screen.
    public float upper;  // Towards the top of the screen.
    public float lower;  // Towards the bottom of the screen.
  }

  /// Information on how the lens distorts light rays.  Also used for the (approximate) inverse
  /// distortion.  Assumes a radially symmetric pincushion/barrel distortion model.
  [System.Serializable]
  public struct Distortion {
    public float k1;
    public float k2;

    public float distort(float r) {
      float r2 = r * r;
      return ((k2 * r2 + k1) * r2 + 1) * r;
    }

    public float distortInv(float radius) {
      // Secant method.
      float r0 = 0;
      float r1 = 1;
      float dr0 = radius - distort(r0);
      while (Mathf.Abs(r1 - r0) > 0.0001f) {
        float dr1 = radius - distort(r1);
        float r2 = r1 - dr1 * ((r1 - r0) / (dr1 - dr0));
        r0 = r1;
        r1 = r2;
        dr0 = dr1;
      }
      return r1;
    }
  }

  /// Information about a particular device, including specfications on its lenses, FOV,
  /// and distortion and inverse distortion coefficients.
  [System.Serializable]
  public struct Device {
    public Lenses lenses;
    public MaxFOV maxFOV;
    public Distortion distortion;
    public Distortion inverse;
  }

  /// Screen parameters of a Cardboard device.
  public Screen screen;

  /// Device parameters of a Cardboard device.
  public Device device;

  /// The vertical offset of the lens centers from the screen center.
  public float VerticalLensOffset {
    get {
      return (device.lenses.offset - screen.border - screen.height/2) * device.lenses.alignment;
    }
  }

  /// Some known screen profiles.
  public enum ScreenSizes {
    Nexus5,
    Nexus6,
    GalaxyS6,
    GalaxyNote4,
    LGG3,
    iPhone4,
    iPhone5,
    iPhone6,
    iPhone6p,
  };

  /// Parameters for a Nexus 5 device.
  public static readonly Screen Nexus5 = new Screen {
    width = 0.110f,
    height = 0.062f,
    border = 0.004f
  };

  /// Parameters for a Nexus 6 device.
  public static readonly Screen Nexus6 = new Screen {
    width = 0.133f,
    height = 0.074f,
    border = 0.004f
  };

  /// Parameters for a Galaxy S6 device.
  public static readonly Screen GalaxyS6 = new Screen {
    width = 0.114f,
    height = 0.0635f,
    border = 0.0035f
  };

  /// Parameters for a Galaxy Note4 device.
  public static readonly Screen GalaxyNote4 = new Screen {
    width = 0.125f,
    height = 0.0705f,
    border = 0.0045f
  };

  /// Parameters for a LG G3 device.
  public static readonly Screen LGG3 = new Screen {
    width = 0.121f,
    height = 0.068f,
    border = 0.003f
  };

  /// Parameters for an iPhone 4 device.
  public static readonly Screen iPhone4 = new Screen {
    width = 0.075f,
    height = 0.050f,
    border = 0.0045f
  };

  /// Parameters for an iPhone 5 device.
  public static readonly Screen iPhone5 = new Screen {
    width = 0.089f,
    height = 0.050f,
    border = 0.0045f
  };

  /// Parameters for an iPhone 6 device.
  public static readonly Screen iPhone6 = new Screen {
    width = 0.104f,
    height = 0.058f,
    border = 0.005f
  };

  /// Parameters for an iPhone 6p device.
  public static readonly Screen iPhone6p = new Screen {
    width = 0.112f,
    height = 0.068f,
    border = 0.005f
  };

  /// Some known Cardboard device profiles.
  public enum DeviceTypes {
    CardboardJun2014,
    CardboardMay2015,
    GoggleTechC1Glass,
  };

  /// Parameters for a Cardboard v1.
  public static readonly Device CardboardJun2014 = new Device {
    lenses = {
      separation = 0.060f,
      offset = 0.035f,
      screenDistance = 0.042f,
      alignment = Lenses.AlignBottom,
    },
    maxFOV = {
      outer = 40.0f,
      inner = 40.0f,
      upper = 40.0f,
      lower = 40.0f
    },
    distortion = {
      k1 = 0.441f,
      k2 = 0.156f
    },
    inverse = ApproximateInverse(0.441f, 0.156f)
  };

  /// Parameters for a Cardboard v2.
  public static readonly Device CardboardMay2015 = new Device {
    lenses = {
      separation = 0.064f,
      offset = 0.035f,
      screenDistance = 0.039f,
      alignment = Lenses.AlignBottom,
    },
    maxFOV = {
      outer = 60.0f,
      inner = 60.0f,
      upper = 60.0f,
      lower = 60.0f
    },
    distortion = {
      k1 = 0.34f,
      k2 = 0.55f
    },
    inverse = ApproximateInverse(0.34f, 0.55f)
  };

  /// Parameters for a Go4D C1-Glass.
  public static readonly Device GoggleTechC1Glass = new Device {
    lenses = {
      separation = 0.065f,
      offset = 0.036f,
      screenDistance = 0.058f,
      alignment = Lenses.AlignBottom,
    },
    maxFOV = {
      outer = 50.0f,
      inner = 50.0f,
      upper = 50.0f,
      lower = 50.0f
    },
    distortion = {
      k1 = 0.3f,
      k2 = 0
    },
    inverse = ApproximateInverse(0.3f, 0)
  };

  /// Nexus 5 in a Cardboard v1.
  public static readonly CardboardProfile Default = new CardboardProfile {
    screen = Nexus5,
    device = CardboardJun2014
  };

  /// Returns a CardboardProfile with the given parameters.
  public static CardboardProfile GetKnownProfile(ScreenSizes screenSize, DeviceTypes deviceType) {
    Screen screen;
    switch (screenSize) {
      case ScreenSizes.Nexus6:
        screen = Nexus6;
        break;
      case ScreenSizes.GalaxyS6:
        screen = GalaxyS6;
        break;
      case ScreenSizes.GalaxyNote4:
        screen = GalaxyNote4;
        break;
      case ScreenSizes.LGG3:
        screen = LGG3;
        break;
      case ScreenSizes.iPhone4:
        screen = iPhone4;
        break;
      case ScreenSizes.iPhone5:
        screen = iPhone5;
        break;
      case ScreenSizes.iPhone6:
        screen = iPhone6;
        break;
      case ScreenSizes.iPhone6p:
        screen = iPhone6p;
        break;
      default:
        screen = Nexus5;
        break;
    }
    Device device;
    switch (deviceType) {
      case DeviceTypes.CardboardMay2015:
        device = CardboardMay2015;
        break;
      case DeviceTypes.GoggleTechC1Glass:
        device = GoggleTechC1Glass;
        break;
      default:
        device = CardboardJun2014;
        break;
    }
    return new CardboardProfile { screen = screen, device = device };
  }

  /// Calculates the tan-angles from the maximum FOV for the left eye for the
  /// current device and screen parameters.
  public void GetLeftEyeVisibleTanAngles(float[] result) {
    // Tan-angles from the max FOV.
    float fovLeft = Mathf.Tan(-device.maxFOV.outer * Mathf.Deg2Rad);
    float fovTop = Mathf.Tan(device.maxFOV.upper * Mathf.Deg2Rad);
    float fovRight = Mathf.Tan(device.maxFOV.inner * Mathf.Deg2Rad);
    float fovBottom = Mathf.Tan(-device.maxFOV.lower * Mathf.Deg2Rad);
    // Viewport size.
    float halfWidth = screen.width / 4;
    float halfHeight = screen.height / 2;
    // Viewport center, measured from left lens position.
    float centerX = device.lenses.separation / 2 - halfWidth;
    float centerY = -VerticalLensOffset;
    float centerZ = device.lenses.screenDistance;
    // Tan-angles of the viewport edges, as seen through the lens.
    float screenLeft = device.distortion.distort((centerX - halfWidth) / centerZ);
    float screenTop = device.distortion.distort((centerY + halfHeight) / centerZ);
    float screenRight = device.distortion.distort((centerX + halfWidth) / centerZ);
    float screenBottom = device.distortion.distort((centerY - halfHeight) / centerZ);
    // Compare the two sets of tan-angles and take the value closer to zero on each side.
    result[0] = Math.Max(fovLeft, screenLeft);
    result[1] = Math.Min(fovTop, screenTop);
    result[2] = Math.Min(fovRight, screenRight);
    result[3] = Math.Max(fovBottom, screenBottom);
  }

  /// Calculates the tan-angles from the maximum FOV for the left eye for the
  /// current device and screen parameters, assuming no lenses.
  public void GetLeftEyeNoLensTanAngles(float[] result) {
    // Tan-angles from the max FOV.
    float fovLeft = device.distortion.distortInv(Mathf.Tan(-device.maxFOV.outer * Mathf.Deg2Rad));
    float fovTop = device.distortion.distortInv(Mathf.Tan(device.maxFOV.upper * Mathf.Deg2Rad));
    float fovRight = device.distortion.distortInv(Mathf.Tan(device.maxFOV.inner * Mathf.Deg2Rad));
    float fovBottom = device.distortion.distortInv(Mathf.Tan(-device.maxFOV.lower * Mathf.Deg2Rad));
    // Viewport size.
    float halfWidth = screen.width / 4;
    float halfHeight = screen.height / 2;
    // Viewport center, measured from left lens position.
    float centerX = device.lenses.separation / 2 - halfWidth;
    float centerY = -VerticalLensOffset;
    float centerZ = device.lenses.screenDistance;
    // Tan-angles of the viewport edges, as seen through the lens.
    float screenLeft = (centerX - halfWidth) / centerZ;
    float screenTop = (centerY + halfHeight) / centerZ;
    float screenRight = (centerX + halfWidth) / centerZ;
    float screenBottom = (centerY - halfHeight) / centerZ;
    // Compare the two sets of tan-angles and take the value closer to zero on each side.
    result[0] = Math.Max(fovLeft, screenLeft);
    result[1] = Math.Min(fovTop, screenTop);
    result[2] = Math.Min(fovRight, screenRight);
    result[3] = Math.Max(fovBottom, screenBottom);
  }

  /// Calculates the screen rectangle visible from the left eye for the
  /// current device and screen parameters.
  public Rect GetLeftEyeVisibleScreenRect(float[] undistortedFrustum) {
    float dist = device.lenses.screenDistance;
    float eyeX = (screen.width - device.lenses.separation) / 2;
    float eyeY = VerticalLensOffset + screen.height / 2;
    float left = (undistortedFrustum[0] * dist + eyeX) / screen.width;
    float top = (undistortedFrustum[1] * dist + eyeY) / screen.height;
    float right = (undistortedFrustum[2] * dist + eyeX) / screen.width;
    float bottom = (undistortedFrustum[3] * dist + eyeY) / screen.height;
    return new Rect(left, bottom, right - left, top - bottom);
  }

  public static float GetMaxRadius(float[] tanAngleRect) {
    float x = Mathf.Max(Mathf.Abs(tanAngleRect[0]), Mathf.Abs(tanAngleRect[2]));
    float y = Mathf.Max(Mathf.Abs(tanAngleRect[1]), Mathf.Abs(tanAngleRect[3]));
    return Mathf.Sqrt(x * x + y * y);
  }

  // Solves a least-squares matrix equation.  Given the equation A * x = y, calculate the
  // least-square fit x = inverse(A * transpose(A)) * transpose(A) * y.  The way this works
  // is that, while A is typically not a square matrix (and hence not invertible), A * transpose(A)
  // is always square.  That is:
  //   A * x = y
  //   transpose(A) * (A * x) = transpose(A) * y   <- multiply both sides by transpose(A)
  //   (transpose(A) * A) * x = transpose(A) * y   <- associativity
  //   x = inverse(transpose(A) * A) * transpose(A) * y  <- solve for x
  // Matrix A's row count (first index) must match y's value count.  A's column count (second index)
  // determines the length of the result vector x.
  private static double[] solveLeastSquares(double[,] matA, double[] vecY) {
    int numSamples = matA.GetLength(0);
    int numCoefficients = matA.GetLength(1);
    if (numSamples != vecY.Length) {
      Debug.LogError("Matrix / vector dimension mismatch");
      return null;
    }
    if (numCoefficients != 2) {
      Debug.LogError("Only 2 coefficients supported.");
      return null;
    }

    // Calculate transpose(A) * A
    double[,] matATA = new double[numCoefficients, numCoefficients];
    for (int k = 0; k < numCoefficients; ++k) {
      for (int j = 0; j < numCoefficients; ++j) {
        double sum = 0.0;
        for (int i = 0; i < numSamples; ++i) {
          sum += matA[i, j] * matA[i, k];
        }
        matATA[j, k] = sum;
      }
    }

    // Calculate the inverse of transpose(A) * A.  Inverting isn't recommended for numerical
    // stability, but should be ok for small and well-behaved data sets.  Using manual matrix
    // inversion here (hence the restriction of numCoefficients to 2 in this function).
    double[,] matInvATA = new double[numCoefficients, numCoefficients];
    double det = matATA[0, 0] * matATA[1, 1] - matATA[0, 1] * matATA[1, 0];
    matInvATA[0, 0] = matATA[1, 1] / det;
    matInvATA[1, 1] = matATA[0, 0] / det;
    matInvATA[0, 1] = -matATA[1, 0] / det;
    matInvATA[1, 0] = -matATA[0, 1] / det;

    // Calculate transpose(A) * y
    double[] vecATY = new double[numCoefficients];
    for (int j = 0; j < numCoefficients; ++j) {
      double sum = 0.0;
      for (int i = 0; i < numSamples; ++i) {
        sum += matA[i, j] * vecY[i];
      }
      vecATY[j] = sum;
    }

    // Now matrix multiply the previous values to get the result.
    double[] vecX = new double[numCoefficients];
    for (int j = 0; j < numCoefficients; ++j) {
      double sum = 0.0;
      for (int i = 0; i < numCoefficients; ++i) {
        sum += matInvATA[i, j] * vecATY[i];
      }
      vecX[j] = sum;
    }
    return vecX;
  }

  /// Calculates an approximate inverse to the given radial distortion parameters.
  public static Distortion ApproximateInverse(float k1, float k2, float maxRadius = 1,
                                              int numSamples = 10) {
    return ApproximateInverse(new Distortion { k1=k1, k2=k2 }, maxRadius, numSamples);
  }

  /// Calculates an approximate inverse to the given radial distortion parameters.
  public static Distortion ApproximateInverse(Distortion distort, float maxRadius = 1,
                                              int numSamples = 10) {
    const int numCoefficients = 2;

    // R + k1*R^3 + k2*R^5 = r, with R = rp = distort(r)
    // Repeating for numSamples:
    //   [ R0^3, R0^5 ] * [ K1 ] = [ r0 - R0 ]
    //   [ R1^3, R1^5 ]   [ K2 ]   [ r1 - R1 ]
    //   [ R2^3, R2^5 ]            [ r2 - R2 ]
    //   [ etc... ]                [ etc... ]
    // That is:
    //   matA * [K1, K2] = y
    // Solve:
    //   [K1, K2] = inverse(transpose(matA) * matA) * transpose(matA) * y
    double[,] matA = new double[numSamples, numCoefficients];
    double[] vecY = new double[numSamples];
    for (int i = 0; i < numSamples; ++i) {
      float r = maxRadius * (i + 1) / (float) numSamples;
      double rp = distort.distort(r);
      double v = rp;
      for (int j = 0; j < numCoefficients; ++j) {
        v *= rp * rp;
        matA[i, j] = v;
      }
      vecY[i] = r - rp;
    }
    double[] vecK = solveLeastSquares(matA, vecY);
    return new Distortion {
      k1 = (float)vecK[0],
      k2 = (float)vecK[1]
    };
  }
}
/// @endcond
