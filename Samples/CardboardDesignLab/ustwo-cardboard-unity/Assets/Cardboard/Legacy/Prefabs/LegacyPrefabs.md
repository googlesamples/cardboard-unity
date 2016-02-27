# Legacy Prefabs

This section describes the prefabs that are provided by the package for legacy
purposes.

## CardboardGUI

This prefab captures the _OnGUI_ pass of a frame into a texture.  It draws the
texture on a quad in the scene, along with cursor image for the mouse pointer,
if you specify one.  It can move the mouse by tracking the user's gaze and it
can read the trigger to "click" UI elements.

To use it, add an instance of the prefab to the scene.  It should not be placed
under a `CardboardHead` if you want the user's gaze to control the mouse, but can
be if you use an alternative input device.  The prefab contains one child called
GUIScreen, which by default draws the entire screen's OnGUI layer on a single
quad.

It is possible to add more such children to the CardboardGUI object.  Just
duplicate the `GUIScreen` child in the Editor as needed, adjust each child's
_Rect_ to pick out a different region of the OnGUI layer, and set its
_Transform_ to place that region in the scene.  In this way you can completely
rearrange the GUI layout when in VR Mode without changing any `OnGUI()` functions'
code or behavior.

`CardboardGUI` and its Window children have visibility controls that you can use
to convert a non-transient (i.e. always on) UI to a transient (popup) UI when in
VR Mode.  Non-transient UI is usually placed along the edges of the screen,
which in HMD stereo systems are extremely hard to see.  For VR Mode, you will
likely need to rearrange the UI to appear front-and-center, perhaps depending on
which way the user looks.  This obscures the scene itself, unless the UI can be
hidden when not needed.
