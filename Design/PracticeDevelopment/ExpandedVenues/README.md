# Exploring the expanded PULSE scenarios

Players arrive in the new public area. Each scenario contains twelve team characters: two adults perform ordinary jobs and can become the priority patient, a third character has a clinical assessment, and nine other people can be approached and checked. Local activities include reading a ridge map, carrying food, inspecting tools and moving parcels. Clothing colors vary independently of the incident.

The patient can be in the original venue or its new south area. A/B/C identifiers are independent of location. A learner must get within 3.2 m and have the person in sight to obtain findings. Healthy bystanders can answer without entering the treatment sequence. Heartbeat and breathing sources follow the affected person. Each area has its own working response cart.

[See all five scenes together](Overview.jpg). These images were captured in Windows Unity Editor Play Mode using a documentation camera. The arrival image uses the player's starting viewpoint. The second frame shows a character during their ordinary activity.

| Scenario | New destination and visible features | Arrival | Activity |
| --- | --- | --- | --- |
| Switchback Coffee | A reading room with shelves, seating and a pastry area | [Reading room](01-arrival.jpg) | [Customer with coffee](01-routine.jpg) |
| Relay Hall | A south commons with communal seating and serving stations | [Commons](02-arrival.jpg) | [Diner with a tray](02-routine.jpg) |
| Service road | A parcel exchange with freight racks, staging lanes and a sorting station | [Parcel exchange](03-arrival.jpg) | [Parcel pickup](03-routine.jpg) |
| Workshop | An assembly floor with benches and tool stations | [Assembly floor](04-arrival.jpg) | [Worker collecting a tool](04-routine.jpg) |
| Alpine trail | An open ridge trail with snow paths, rope boundaries and a lookout | [Trail entrance](05-arrival.jpg) | [Hiker at the refuge](05-routine.jpg) |

## Verification

The route test starts at the actual player spawn and uses a half-metre grid to measure connected walking space south of z = −11. A separate Play Mode test moves the real player capsule across the map, then restores the start position. It also uses the controller-trigger handler to check a nearby bystander, refuses remote and backward-facing symptom checks, and treats both adult identities in the five rooms. [Verification results](verification.txt)

The Windows tests exercised simulated inputs. Physical headset comfort and headphone localization remain for hands-on testing. Furnishings reuse project materials and credited source models. [Room asset register](../Room-asset-register.md)
