Photon Fusion Advanced KCC Sample - Readme

Controls
================================================================

1) Keyboard + Mouse (PC, Editor)
----------------------------------------
Mouse            - Look
W,S,A,D          - Move
Shift            - Run
Space            - Jump
Tab              - Dash
+,-              - Toggle speed
Enter            - Lock/unlock cursor
Ctrl + Shift + M - Simulate app pause/resume
Q,E              - Strafe + look for testing smoothness
F4               - Toggle input smoothing
F5               - Toggle target frame rate
F6               - Toggle quality
F7               - Toggle vertical synchronization
F9               - Toggle recorders (player position / camera / input smoothing)
F12              - Disconnect from current session

2) Touch (Mobile)
----------------------------------------
Left virtual joystick  - Move
Right virtual joystick - Look
Double tap             - Jump
Sprint is triggered when Move joystick is far enough from touch origin.

3) Controllers (VR, Editor)
----------------------------------------
Left joystick  - Move
Right joystick - Look

4) Gamepad (All platforms)
----------------------------------------
Left joystick  - Move
Right joystick - Look
Left trigger   - Sprint
Right trigger  - Jump
A Button       - Dash

5) UI
----------------------------------------
Speed           - Toggle player speed (character base speed will be slower/faster)
Input Smoothing - Toggle input smoothing (look rotation will be smoother at the cost of increased input lag)
FPS             - Toggle target frame rate
Quality         - Toggle quality settings (URP settings, post-processing, lighting, ...)
V-Sync          - Toggle vertical synchronization
Recording       - Toggle recorders (player position / camera / input smoothing)
Cursor          - Toggle cursor visibility


Testing
================================================================
The Showcase scene represents a common composition of block prefabs without optimizations. This is a default testing scene.
Playground scenes (in feature folders) are used for testing specific KCC features or cases (combinations of Box/Sphere/Mesh colliders, various angles, ...).
Other scenes usually represent single feature or set of similar features.


Known bugs
================================================================
1) Incorrect depenetration from MeshCollider with Convex option enabled. This is fixed in Unity 2023.2.
https://issuetracker.unity3d.com/issues/physics-dot-computepenetration-detects-a-collision-with-capsulecollider-if-meshcollider-convex-is-enabled-when-there-is-no-collision
As a workaround, don't use Convex toggle on MeshColliders or enable "Suppress Convex Mesh Colliders" in KCC settings.
