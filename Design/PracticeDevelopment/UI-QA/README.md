# FIRST180 playtest and UI screenshots

These captures are from the live Unity Editor on Windows. The two certification outcomes use temporary test progress. They exercise the same scoring and treatment handlers as the player. The spatial results preview renders the actual VR panel with a desktop camera.

| Capture | What it shows |
| --- | --- |
| [Ready hub](01-Training-hub.jpg) | The gallery displays its destination windows. The panel lists all five training cases and the mastery requirement for certification. |
| [ROOK correction](02-Training-correction.jpg) | The instructor responds to an incorrect equipment placement. The task and accumulated error remain visible. |
| [Passing assessment](03-Certification-pass.jpg) | The desktop dashboard shows the outcome, four metric bars and one row per case. |
| [Failed assessment](04-Certification-timeout.jpg) | An automated attempt includes wrong triage, wrong diagnosis and missed deadlines. Timeouts are labeled in the table. |
| [Spatial results preview](05-Spatial-results-preview.jpg) | The in-world board displays certification metrics and controls to review individual cases. The camera was positioned for inspection in the Editor. |
| [Clicked gallery window](06-Dining-window-entry.jpg) | Clicking the destination photo in the original gallery opens Commons Dining Hall. ROOK appears as the training case begins. |
| [Paused in the dining hall](07-Pause-menu.jpg) | Escape opens the pause menu. The return button explains that leaving the room ends the current attempt. |
| [Mouse look in the gallery](08-Mouse-look.jpg) | After a click on the floor and a rightward mouse movement, the tree is left of the aiming mark and the frostbite window is visible. The footer shows Tab releases the menu cursor. |

## Verification

- [Logic and serialized-scene checks](../logic-verification.txt)
- [Play Mode checks](../runtime-verification.txt), including all five gallery round trips, 100% mastery, correct and incorrect equipment use, passing and failing certification, JSON export and synthetic two-controller input.
- Walkable approach and eye-height ray checks also ran in all five rooms. They exercise pickup distance, obstruction by scene colliders and treatment targets. Physical controller reach and headset comfort still need a headset.
