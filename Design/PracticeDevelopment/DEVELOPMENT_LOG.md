# FIRST180 Unity practice development

This is an AI-assisted personal learning project, not a competition entry or medical qualification. Procedural original geometry is used; no Sketchfab assets were downloaded or purchased.

## 2026-09-05 — Inspection and environment checkpoint

- Active project: `New Unity Project`, Unity 6000.5.10f1. The separate `UnityProject` prototype is not the active project.
- Existing lobby and its materials are preserved. The lobby had an overview camera but no functional runtime player rig or scenario routing.
- Added `Assets/Practice/Scenes/TrainingHub.unity`, which loads the original lobby additively. Runtime portal components are added to its five existing exhibition roots.
- Added separate cafe, dining hall, roadside, workshop and snowy shelter scenes. Original primitive furniture, patients, ROOK robot model, lighting and return portals.
- Added isolated practice session, encounter scoring and desktop movement scaffolding. These systems require further interaction and headset validation before being called complete.
- Unity batch generation exited successfully with `PRACTICE_ENVIRONMENTS_BUILT=5`. No script compiler errors in the first build.
- Original lobby SHA256 after generation: `20C490AA566238B8107A2073ACDB4C76F121303317FA6B5ECCE5B04465D6A0D5`.
- Actual Editor screenshot: `C:/Users/kanag/Pictures/Screenshots/FIRST180-01-cafe-editor.png` (Scene and Hierarchy).

## Checkpoint 2 — Modular patient and equipment systems

- Added `PatientManager`, `TreatmentManager`, `TreatmentTool`, `TreatmentTarget`, `TrainingManager` and per-case `ScenarioConfig` assets.
- Added OpenXR package 1.16.1, XR Management 4.5.3, PC loader, Oculus Touch and Valve Index profiles. Retained legacy desktop input alongside the new input backend.
- Added tracked controllers, equipment gripping, held-tool contact, controller rays, snap turns, locomotion and floor teleport. Headset behavior is not hardware-verified.
- Scene upgrade completed for six scenes. Fixed an obsolete legacy camera API found by the compiler; subsequent script import and scene upgrade completed.
- Actual full Editor screenshot: `C:/Users/kanag/Pictures/Screenshots/FIRST180-02-patient-inspector.png`. Shows patient state fields in the Inspector and the equipment group in the Hierarchy; does not prove end-to-end treatment behavior.

## References consulted

Clinical content is simplified, adult-only game logic with compressed time. Equipment gestures are not a substitute for hands-on instruction, local protocols or device instructions. A qualified instructor must review it before use for actual first-aid teaching.

- American Red Cross, Anaphylaxis: https://www.redcross.org/take-a-class/resources/learn-first-aid/allergic-reaction-anaphylaxis
- American Red Cross, Adult and child choking: https://www.redcross.org/take-a-class/resources/learn-first-aid/adult-child-choking
- American Red Cross, Life-threatening external bleeding: https://www.redcross.org/take-a-class/resources/learn-first-aid/bleeding-life-threatening-external
- American Red Cross, Frostbite: https://www.redcross.org/take-a-class/resources/learn-first-aid/frostbite
- North West Ambulance Service, Heavy bleeding: https://www.nwas.nhs.uk/services/emergency-advice/heavy-bleeding/
- Sketchfab, Emergency Room by Ethan Cragun: https://sketchfab.com/3d-models/emergency-room-ac11cafbc4da4317b495e2bfb3382c28 (reference discovery only; no downloaded geometry, textures or audio).
- Existing lobby inspiration supplied by user: https://sketchfab.com/3d-models/vr-gallery-mind-storm-upd-122022-e5a86e9d8f584a1992b5bb6d0765d4d2

This log records development actions. It does not fabricate student work logs, hand sketches, permission letters or competition eligibility.

## 2026-11-23 — Blender cast integrated into the five scenario rooms

The five supplied designs are four humans and the heart-badge robot. The boy was recovered from `Boy_Model_Real.blend1`. The other source files were `Girl_Model_Real.blend`, `Man_Model_Real.blend`, `Women_Model_Real.blend` and `Man_Model_2_Bot.blend`. Each source contains static geometry. The supplied Blender files are the source for future rigging.

Each Unity room has three human placements and one ROOK instance. Adult models lead the adult cases, with children in secondary roles. The character prefabs in `Assets/Practice/Characters` share a colour palette. Each visible character uses one mesh renderer. The model exports exclude studio floors and reference images. The workshop has a separate, capped forearm mesh for the incident. Character motion in this checkpoint consists of procedural breathing and ROOK's existing follow/hover behaviour.

Treatment targets were fitted to the new bodies and checked with front and rear physics rays. The frostbite cue follows the affected hand. Unity passed 1,152 scene and logic assertions, including 120 anatomical ray tests. The Play Mode harness passed 95 checks covering training, certification, analytics and lobby routing. A separate Play Mode check verified that the workshop swaps appearance after onset. Headset performance and physical controller reach still need an on-device test.

| Unity Editor capture | Documented placement |
| --- | --- |
| [Café](CastScreenshots/01-Cafe.jpg) | Adult woman selected under Patient A in the anaphylaxis room |
| [Dining](CastScreenshots/02-Dining.jpg) | Adult man selected under Patient A in the airway room |
| [Road](CastScreenshots/03-Road.jpg) | Boy selected under Patient C in the roadside scene |
| [Workshop](CastScreenshots/04-Workshop.jpg) | Supplied robot selected under the existing ROOK companion |
| [Trail](CastScreenshots/05-Trail.jpg) | Girl selected under Patient B in the frostbite scene |

## 2026-11-23 — ROOK instructor popup

ROOK appears with the current training instruction, correction, or debrief. The portrait was rendered directly from the supplied heart robot in `Man_Model_2_Bot.blend`. The desktop card sits in the lower left. The VR version uses a world-space canvas positioned when the instruction arrives. The stationary card lets the player look away. The old companion object is inactive in all five scenario scenes. Certification uses neutral status text and omits the instructor.

The cast check verifies that all 15 scenario people use the supplied Boy, Girl, Man, or Woman meshes. It also checks for visible mannequin pieces. The workshop lead retains the incident variant derived from the Man mesh.

[Unity Editor Game view showing ROOK and the three café characters](CastScreenshots/06-Rook-instructor.jpg). The screenshot documents the desktop presentation. Headset placement and physical controller use still need an on-device check.

## 2026-11-25 — Five authored emergency locations

Saved five separate Unity scenes with custom layouts. Juniper Café has an espresso counter and an interrupted lunch. Commons Dining Hall has two serving windows and aisles around communal benches. Riverside places a stationary delivery vehicle and a coned shoulder against a freight yard. The fabrication workshop has a guarded drill station. The alpine trail has an open timber shelter aligned with the existing frostbite handoff.

Each of the five lobby windows shows a still photographed inside its destination scene. The windows are attached when the original gallery loads. All 15 patient and bystander placements keep the supplied team cast. ROOK uses the team's heart robot. The 33 interactive objects share 14 equipment designs and retain their existing grasp colliders and treatment actions. Small printed tray labels keep the equipment visible at hand height.

The four indoor and roadside sound loops are original synthetic recordings created for this practice project. The café has steam and ceramic transients, the commons has serving dishes, the roadside has distant engine and traffic noise, and the workshop has a ventilation hum. The trail uses the existing wind with a gradual volume transition inside the refuge. Training dialogue and the physiological audio remain on separate channels.

Environment asset credits and modification notes accompany the [interiors](../../New%20Unity%20Project/Assets/Practice/Art/Interiors/Sources.txt), [freight yard and workshop](../../New%20Unity%20Project/Assets/Practice/Art/Field/Credits.txt), and [alpine shelter](../../New%20Unity%20Project/Assets/Practice/Art/Alpine/Credits.txt). The new third-party scenery uses CC0 assets from Kenney and Poly Haven.

Unity passed 2,260 scene and logic assertions. A separate half-metre grid check found walkable approaches from each spawn to its three patients, every equipment slot and the return entrance, and to the alpine handoff. The automated Play Mode run exercises an incorrect placement, five perfect training runs, certification and export. See the [assertion report](logic-verification.txt), [Play Mode results](runtime-verification.txt), and [captioned Editor screenshots](EnvironmentScreenshots/README.md). Physical headset reach and stereo comfort still require an on-device session.

The normal Windows release booted and connected all five gallery entrances. The standalone diagnostics exposed a stripped shader used by the spatial decision panel. The final release explicitly includes a stereo-compatible shader for this panel and the hand rays. Both final diagnostic runs recorded zero managed errors. At 1600 × 1000, the lobby and all five rooms met the 60 FPS frame-time target. At 1920 × 1200, the alpine scene recorded 17.60 ms at the 95th percentile. See the [Windows measurements](Windows-frame-benchmark.md).

## November 25, 2026 — Gallery interaction and assessment playtest

Clicking a gallery photo exposed a floor collider extending into the air. The runtime lobby adapter now uses the visible mesh for the four shallow round surfaces. The gallery accepts input when its entrances finish loading. World lettering respects depth so gallery signs disappear behind the VR results board. Clicking the dining window, pausing with Escape and returning to the gallery were checked in the Editor.

The desktop interface now scales uniformly, displays an aiming mark and exposes patient reassessment. Exam patient buttons follow the A/B/C labels at their randomized positions. Reassessing a bystander during treatment preserves the chosen patient's findings. The spatial menus handle selections from either controller, block interactions behind the board and provide individual case results.

Automated Play Mode runs exercise both certification outcomes, wrong equipment placement, all five gallery windows, response-time reporting and JSON export with per-case action histories. Walkability checks cover every patient, equipment pickup and return portal across the five rooms. The automated attempts use temporary progress. Physical headset reach, stereo readability and comfort remain open for a headset playtest.

See the [captioned Unity Editor screenshots](UI-QA/README.md), [Play Mode results](runtime-verification.txt) and [logic and scene assertions](logic-verification.txt).
The final run passed 2,260 logic and scene assertions and 186 Play Mode checks. In the rebuilt Windows player at 1600 × 900, clicking the dining window opened the obstructed-airway case and the right arrow turned the view. The packaged ZIP passed its file integrity check.

### Desktop mouse movement

Clicking scenery or pressing Tab captures the mouse for first-person turning in the lobby and scenario rooms. Middle-button dragging moves the character sideways through its existing collision controller. Tab releases the pointer for menus. In the Unity Editor, clicking the lobby floor enabled mouse look, moving the mouse turned the view, and releasing the pointer with Tab allowed the café menu button to open the case. [Editor screenshot](UI-QA/08-Mouse-look.jpg)

### November 25, 2026 — Switchback Coffee and Relay Hall

The café now has two window tables, each with a pair of outward-facing chairs, and a folded-line inlay under the counter. Relay Hall has numbered serving hatches, a lengthwise table and a crosswise table. Each dining table’s benches, dishes and food move with it. Both rooms use the existing Kenney furniture and food meshes. The revised room signs, case titles and two gallery window photos use the new identities.

Unity passed 2,265 logic and scene checks and the walking and interaction route checks for all five scenarios. The four window-side chairs were checked against the backs of the actual imported meshes. A physical headset playtest was outside this room revision.

Editor checkpoints: [Switchback Coffee and window seating](InteriorRevision/01-Switchback-Coffee.jpg), [Relay Hall and central aisle](InteriorRevision/02-Relay-Hall.jpg), [updated gallery windows in Play Mode](InteriorRevision/03-Updated-gallery-windows.jpg). The [room asset register](Room-asset-register.md) records the creators, licenses, edits and outstanding competition documentation.

## November 26, 2026 — PULSE branding

The project adopted **PULSE** with the subtitle *First Aid in VR*. The AI-assisted practice wordmark uses original geometric lettering and a heartbeat trace. Its mint glow is baked into a shared texture so the gallery signs, desktop header and world-space VR panel display it consistently without camera bloom. The gallery generator reproduces the wall, entrance and planter signs. Internal scene and save identifiers are preserved.

- [Lobby and desktop header in Play Mode](PulseBranding/01-Lobby-Game-view.png)
- [Gallery wall sign and subtitle in the Unity Editor](PulseBranding/02-Gallery-sign.png)

## November 26, 2026 — Enclosed PULSE simulation bays

All five scenarios now have a solid floor, four walls and a ceiling, a simulation-control observation window, and a numbered PULSE equipment sign. The roadway and alpine scenes use high ceilings for the vehicle and shelter sets. The workshop has additional overhead fill for patient visibility. Lobby window photographs were refreshed from the five saved scenes.

Training and Certification shuffle the three patient identities independently of case severity and choose one of two verified marks per aisle, giving 48 possible cast arrangements per room. Patient models, clinical targets and observations move together. The training UI identifies the inspected or selected patient, and ROOK delivers written corrections without speech. Heartbeat, breathing, ambience and treatment audio remain in the simulation.

The local Unity Editor supplied [seven screenshots with captions](EnclosedBays/README.md). An isolated copy of the same scene and practice-script files passed all five route checks, 2,523 serialized-scene and logic assertions, and 208 Play Mode checks. QA included all six possible marks per patient in each room, desktop and spatial patient panels, both certification outcomes, and returning to the lobby. The spatial UI was exercised through simulated controller input. Physical headset testing remains outstanding.

## November 26, 2026 — Open-air alpine trail

Removed the frostbite chamber and restored the view of the mountains. The timber refuge and its roof remain available for the shelter handoff. The lobby window now shows the outdoor scene. Unity checked the open sky, all six placement marks per patient, all seven tools, shelter approach and return portal. [Unity Editor screenshot](AlpineTrail/Open-air-frostbite.jpg)

## November 26, 2026 — Everyday activity and emergency discovery

The café pilot exercises a customer walking to the counter, reaching for a cup and carrying it to an incident location. The two eligible, team-authored adults trade patient roles on consecutive entries. Both adults now have pickup-and-carry routines at independently assigned stations. Two more people use venue-specific walking routes. The dining hall has lunch trays, the service road has parcel dispatch, the workshop has tool collection, and the open alpine trail has route maps at the lookout and refuge. Four existing enclosures and the lobby remain as authored.

Training waits until the learner sees the patient and silent ROOK can prompt the learner to look around. Certification can begin with an incident sound from the patient's location. Muted certification uses the visibility check. Treatment time starts at onset. Workshop injury geometry and alpine hand cues are fitted to both adult characters.

The Windows Unity Play Mode tests exercised real scene transitions, walking, prop collection, alternating visible meshes, treatment with equipment, scoring, recovery, local cues and the muted run. After an alpine crossing failed in Play Mode, pedestrian departures were spaced using predicted routes. A geometry sweep of 480 assignments found 0.99 m minimum separation between the two walking adults. Static scene tests also verified the response-equipment approaches, return portals and the open sky above the trail. [Play Mode captures and verification](ScenarioRoutines/README.md)
