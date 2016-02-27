# Prefabs

This section describes the prefabs that are provided by the package.

## CardboardCamera

This prefab simply contains an instance of the _%CardboardPreRender_ script and
an instance of the _%CardboardPostRender_ script, and a _Camera_ component for
each.

The CardboardPreRender script is drawn before any stereo rendering occurs.  It's
primary job is to clear the full screen, since in VR mode, the stereo cameras don't
always fill the entire frame.

The CardboardPostRender script is more substantial.  It is the last Camera to draw
during Unity's rendering pipeline.  It's primary purpose is to now manage distortion
correction.  When the new in-Unity distortion method is active, it renders a
predistorted mesh with the stereo screen as its texture.  If the old native C++
distortion method is active, it simply defers this activity to the native plugin.

Since CardboardPostRender now occurs during the normal Camera rendering phase, any
Screen Space Overlay canvases will be drawn on top of it rather than being covered
up as in prior versions of the SDK.

This prefab is bundled in the _CardboardMain_ prefab.  It is also generated at runtime
automatically if one doesn't exist.  You only need to manually include this in the
scene if you are _not_ using _CardboardMain_, and you wish to edit the Camera
component's settings in the Unity editor.

## CardboardMain

This prefab is intended to be a drop-in replacement for a normal Unity camera,
such as _Main Camera_, primarily when the camera is simple, i.e. not already
festooned with scripts.  A brand new Unity project is a good time to use it.  To
apply it in the Editor, delete the camera in question and replace it with an
instance of this prefab.

The prefab contains a top-level object called _CardboardMain_, which has a
Cardboard script attached to it for controlling VR Mode settings.  Under this is
the _Head_ object, which has a _%CardboardHead_ attached for tracking the user's
head motion.  That in turn has a _Main Camera_ child object, with the
StereoController script attached.  This camera is tagged as a _MainCamera_ so
that the Unity property `Camera.main` will find it.  Finally there are the Left
and Right stereo eye cameras at the bottom of the hierarchy.

## %CardboardHead

This prefab is a replacement for other cameras in the scene that also need
head-tracking.  It has a top-level object called _Head_ with a CardboardHead
script.  Under that is the _Camera_ child which has the StereoController script,
and then the Left and Right stereo eye cameras.  Unlike _CardboardMain_, the
camera in this prefab is not tagged _MainCamera_.

## CardboardAdapter

This prefab is for when you wish to keep your existing Camera object, usually
because it is heavily wired with game logic and already moves exactly as needed
for non-VR game play.  Place this prefab as a child of the camera, and then
execute _Update Stereo Cameras_ (or manually attach StereoController to the
camera) to complete the rig.

Unlike the other prefabs, this setup places the head-tracked node below the Main
Camera.  Therefore, only the stereo cameras are affected by the user's head
motion.

## General Prefab Notes

Each prefab is provided as a starting point for customization.  It is a
framework on which to attach additional scripts for your own needs.  Elements
under a Head node will maintain a fixed position on screen, thus acting like a
Heads-up Display (HUD).  Elements that are siblings of Head will maintain a
fixed position and orientation relative to the overall point of view, thus
acting more like a cockpit instrument panel.
