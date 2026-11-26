using UnityEngine;

namespace First180.Practice
{
    public sealed partial class ScenarioRoutine
    {
        // The four walking lanes flank the clinical route. Three more people stop at local landmarks.
        // Positions use the same floor plan as the south extension in all five scene files.
        static readonly string[,] PublicJobs =
        {
            { "Customer at entrance", "Coffee runner", "Guest at window", "Guest at window", "Customer heading to seats", "Returning customer", "Guest with espresso" },
            { "Guest at entrance", "Tray runner", "Diner by table", "Diner by table", "Guest heading to lunch", "Returning diner", "Diner with tray" },
            { "Visitor at entrance", "Dispatch runner", "Shift coordinator", "Delivery driver", "Loading attendant", "Courier", "Parcel receiver" },
            { "Visitor at entrance", "Parts runner", "Instructor", "Visiting technician", "Floor supervisor", "Staff member", "Tool inspector" },
            { "Walker at trailhead", "Trail guide", "Hiking companion", "Hiking companion", "Hiker on ridge", "Returning walker", "Hiker reading map" }
        };
        static readonly string[] Observations =
        {
            "They look toward you and answer in a clear voice.",
            "They carry their item steadily and respond when addressed.",
            "They are speaking in full sentences with a companion.",
            "They listen to their companion and nod in response.",
            "Their stride is even. They turn to answer you.",
            "They walk with a steady pace and answer clearly.",
            "They handle their item with both hands and look up when addressed."
        };

        // All variants keep the team's skin, hair, wound and dressing swatches identical.
        // Shuffling a balanced set gives multiple people each colour, independent of the incident.
        void DressCast(RoutineProps art)
        {
            var outfits = (Material[])art.shirts.Clone();
            for (int i = outfits.Length - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (outfits[i], outfits[j]) = (outfits[j], outfits[i]);
            }
            int next = 0;
            foreach (var patient in room.patients) Dress(patient.transform);
            foreach (var witness in witnesses) Dress(witness.walker.Actor);

            void Dress(Transform person)
            {
                var uniform = outfits[next++ % outfits.Length];
                foreach (var skin in person.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    if (skin.name == "ColdFingers" || skin.name == "HandDressing") continue;
                    skin.sharedMaterial = uniform;
                }
            }
        }

        void SetUpPublicArea(RoutineStations stations, RoutineProps art, Animator leadModel, Animator otherAdult,
            Animator childModel, GameObject alternateChild)
        {
            var extended = new Witness[9];
            witnesses.CopyTo(extended, 0);
            witnesses = extended;
            var id = room.scenarioId;
            bool familyVenue = id == 0 || id == 1 || id == 4;

            Add(0, Floor(-6.8f,-10.9f), Floor(-2.9f,-11.35f));
            Add(1, Floor(2.9f,-12.25f), Floor(6.5f,-12.25f));
            Add(2, Floor(-7.55f,-19.48f), Floor(-7.55f,-19.48f));
            Add(3, Floor(-6.72f,-20.37f), Floor(-6.72f,-20.37f));
            Add(4, Floor(6.45f,-14.55f), Floor(6.45f,-18.7f));
            Add(5, Floor(-6.38f,-23.05f), Floor(-2.35f,-23.05f));
            Add(6, Floor(6.67f,-22.62f), Floor(6.67f,-22.62f));

            void Add(int role, Vector3 start, Vector3 end)
            {
                bool young = familyVenue && (role == 0 || role == 3);
                Animator model = young ? childModel : role % 2 == 0 ? leadModel : otherAdult;
                var original = young && role == 0 ? alternateChild : model.gameObject;
                var outfit = original == alternateChild ? art.cast : null;
                var walker = AddWitness(original, model.transform.localRotation, outfit, PublicJobs[id, role], start, end,
                    Random.Range(.73f, 1.05f), Random.Range(1.5f, 3.2f), Observations[role]);
                witnesses[2 + role] = walker;
                walker.chatting = role == 2 || role == 3;
                walker.gesturePhase = Random.Range(0f, 6.28f);
                walker.poseCountdown = role * .007f;
                if (start != end)
                {
                    walker.turn = Random.value < .5f;
                    walker.walker.Actor.position = Vector3.Lerp(start, end, Random.Range(.05f, .68f));
                    walker.walker.Face((walker.turn ? start : end) - walker.walker.Position, 1f);
                }
                else
                {
                    Vector3 interest = role == 2 ? Floor(-6.72f,-20.37f)
                        : role == 3 ? Floor(-7.55f,-19.48f) : Floor(3.05f,-20.8f);
                    walker.walker.Face(interest - start, 1f);
                }
                if (role == 1 || role == 6)
                    walker.carried = stations.CrowdProp(id, walker.walker.Actor, walker.walker.GripCenter(false));
            }
        }
    }
}
