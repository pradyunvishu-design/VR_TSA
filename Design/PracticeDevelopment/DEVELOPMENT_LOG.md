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
