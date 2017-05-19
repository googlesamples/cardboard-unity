using System;
using UnityEngine;

// Measurements of a particular phone in a particular Cardboard device.
public class CardboardProfile {
  public CardboardProfile Clone() {
    return new CardboardProfile {
      screen = this.screen,
      device = this.device
    };
  }

  // Information about the screen.  All distances in meters, measured as the phone is expected
  // to be placed in the Cardboard, i.e. landscape orientation.
  public struct Screen {
    public float width;
    public float height;
    public float border;  // Distance from bottom of the cardboard to the bottom edge of screen.
  }

  // Information about the lens placement in the Cardboard.  All distances in meters.
  public struct Lenses {
    public float separation;  // Center to center.
    public float height;      // Height of lens center from bottom of cardboard.
    public float screenDistance; // Distance from lens center to the phone screen.
  }

  // Information about the viewing angles through the lenses.  All angles in degrees, measured
  // away from the optical axis, i.e. angles are all positive.  It is assumed that left and right
  // eye FOVs are mirror images, so that both have the same inner and outer angles.  Angles do not
  // need to account for the limits due to screen size.
  public struct MaxFOV {
    public float outer;  // Towards the side of the screen.
    public float inner;  // Towards the center line of the screen.
    public float upper;  // Towards the top of the screen.
    public float lower;  // Towards the bottom of the screen.
  }

  // Information on how the lens distorts light rays.  Also used for the (approximate) inverse
  // distortion.  Assumes a radially symmetric pincushion/barrel distortion model.
  public struct Distortion {
    public float k1;
    public float k2;

    public float distort(float r) {
      float r2 = r * r;
      return ((k2 * r2 + k1) * r2 + 1) * r;
    }
  }

  public struct Device {
    public Lenses lenses;
    public MaxFOV maxFOV;
    public Distortion distortion;
    public Distortion inverse;
  }

  // The combined set of information about a Cardboard profile.
  public Screen screen;
  public Device device;

  // Some known profiles.

  public static readonly Screen Nexus5 = new Screen {
    width = 0.110f,
    height = 0.062f,
    border = 0.003f
  };

  public static readonly Device Original = new Device {
    lenses = {
      separation = 0.060f,
      height = 0.035f,
      screenDistance = 0.042f
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
    inverse = {
      k1 = -0.423f,
      k2 = 0.239f
    }
  };

  // Nexus 5 in an original Cardboard.
  public static readonly CardboardProfile Default = new CardboardProfile {
    screen = Nexus5,
    device = Original
  };
}
