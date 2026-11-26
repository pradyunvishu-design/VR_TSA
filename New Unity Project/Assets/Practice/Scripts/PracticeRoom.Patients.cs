using UnityEngine;

namespace First180.Practice
{
    public partial class PracticeRoom
    {
        // Pairs of floor marks for the left, middle and right aisles as seen from the entrance.
        // Keep the equipment station, treatment approaches and alpine shelter doorway clear.
        static readonly Vector2[,] patientMarks =
        {
            { new(-4.7f, .2f), new(-3.7f, 1.4f), new(-.6f, -1.4f), new(.75f, .75f), new(4.7f, 1.8f), new(4.1f, -.8f) },    // Café
            { new(-5f, .55f), new(-4.4f, -1.65f), new(-.6f, 1.85f), new(.7f, -.45f), new(4.85f, .7f), new(3.6f, -1.65f) }, // Dining
            { new(-4.35f, .45f), new(-5.2f, -.8f), new(-.8f, 1.4f), new(.7f, -1.35f), new(4.65f, 1.6f), new(4f, -1.3f) },  // Road
            { new(-4.5f, 1.25f), new(-3.75f, -1.55f), new(-.55f, -.85f), new(.55f, 1.6f), new(3.85f, 1.7f), new(4.9f, -.8f) }, // Workshop
            { new(-4.75f, -.55f), new(-3.5f, -1.6f), new(-.8f, -.6f), new(.65f, 1.8f), new(4.7f, .7f), new(3.8f, -1.2f) }     // Trail
        };

        public Vector3 PatientMark(int slot, int variation)
        {
            var mark = patientMarks[scenarioId, slot * 2 + variation];
            return new Vector3(mark.x, 0f, mark.y);
        }

        void PlacePatients()
        {
            // Shuffle the physical assignments only. Clinical IDs and their attached anatomy stay together.
            int[] slots = { 0, 1, 2 };
            for (int i = slots.Length - 1; i > 0; i--)
            {
                int other = Random.Range(0, i + 1);
                (slots[i], slots[other]) = (slots[other], slots[i]);
            }

            foreach (var patient in patients)
            {
                int slot = slots[patient.patientId];
                patient.transform.position = PatientMark(slot, Random.Range(0, 2));
                string label = ((char)('A' + slot)).ToString();
                patientLabels[patient.patientId] = label;
                var marker = patient.transform.Find("Patient marker");
                if (marker && marker.TryGetComponent<TextMesh>(out var text)) text.text = "PATIENT " + label;
            }
            Physics.SyncTransforms();
        }
    }
}
