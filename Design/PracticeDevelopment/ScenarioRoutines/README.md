# PULSE scenario routines

These are camera captures from live Windows Unity Editor Play Mode, run in the background. Each pair shows an ordinary activity followed by an emergency on a separate visit with a different adult. The camera frames the scene for documentation. The learner HUD is outside these images.

[View all ten frames together](Overview.jpg) · [First café pilot capture](Cafe-pilot.jpg) · [Dispatcher pushing a loaded parcel trolley](Service-road-trolley.jpg)

| Scenario | Before the incident | On the next visit |
| --- | --- | --- |
| Switchback Coffee | [Customer carrying a drink from the counter](01-cafe-pickup.jpg) | [Another customer shows distress](02-cafe-incident.jpg) |
| Relay Hall | [Diner carrying lunch](03-dining-routine.jpg) | [Airway emergency in the dining hall](04-dining-incident.jpg) |
| Service road | [Parcel collection at dispatch](05-road-routine.jpg) | [Bleeding emergency in the loading area](06-road-incident.jpg) |
| Workshop | [Worker collecting a fitting tool](07-workshop-routine.jpg) | [Injured worker at a care mark](08-workshop-incident.jpg) |
| Alpine trail | [Hiker collecting a route map](09-trail-routine.jpg) | [Hiker showing hand symptoms near the refuge](10-trail-incident.jpg) |

To capture the Editor interface, open the relevant scenario scene, enter Play Mode, and expand the object with the **Practice Room** component in the Hierarchy. Select **Scenario Routine** in the Inspector before onset. For the clinical handoff, select the affected patient and inspect **Case Patient Animation** while the Game view shows the change.

The production cast comes from the team's Blender files. The alternate workshop injury and alpine hands derive from PracticeWoman.blend and PracticeWorkshopMan.blend and use the team's CharacterPalette texture. The new stations reuse existing project materials. See the [asset register](../Room-asset-register.md) for the source listings and licenses of the reused props. The five incident sounds are synthesized in PracticeAudio.cs.

## Checks

[Play Mode encounter results](play-mode-checks.txt) · [Scene, route and integration results](integration-checks.txt) · [Offline route clearance by room](route-clearance.txt)

The encounter run exercises a Training visit and an audible Certification visit per room, plus muted Certification in the café. It checks the visible adult mesh, both walkers collecting props, sampled body clearance, the treatment mark, cue timing, signal amplitude, and equipment-based stabilization. The two fitted clinical variations also have direct renderer checks. Integration covers the lobby, mastery, certification, results and return portals. The offline clearance check samples 480 assignments at 30 ms intervals. A physical headset and subjective headphone localization still need a hands-on pass.

Unity 6000.5.10f1 reported a search-index initialization exception inside UnityEditor.Search in the isolated batch Editor before Play Mode. The scene, Play Mode and build results are recorded separately.
