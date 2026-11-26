using UnityEngine;

namespace First180.Practice
{
    public class PracticePatient : MonoBehaviour
    {
        public int patientId;
        public Transform body;
        public GameObject injuryVisual;
        public MeshFilter characterVisual;
        public Mesh incidentAppearance;
        Vector3 baseScale;
        Quaternion baseRotation;
        Vector3 characterScale;
        PracticeRoom room;
        PatientManager state;
        CasePatientAnimation animationDriver;

        void Awake()
        {
            room = FindFirstObjectByType<PracticeRoom>();
            state = GetComponent<PatientManager>();
            animationDriver = GetComponent<CasePatientAnimation>();
            if (body) { baseScale = body.localScale; baseRotation = body.localRotation; }
            if (characterVisual) characterScale = characterVisual.transform.localScale;
        }
        void Update()
        {
            bool emergency=room!=null && room.EmergencyActive;
            if (!state) state = GetComponent<PatientManager>();
            animationDriver = GetComponent<CasePatientAnimation>();
            bool animated = animationDriver && animationDriver.DrivesVisual;
            if (characterVisual && !animated && (room == null || !room.Paused))
            {
                float rate = emergency && patientId == 0 && state && !state.stabilized ? 3.2f : 1.6f;
                float breath = Mathf.Sin(Time.time * rate + patientId) * .003f;
                characterVisual.transform.localScale = Vector3.Scale(characterScale, new Vector3(1, 1 + breath, 1));
                if (emergency && incidentAppearance && characterVisual.sharedMesh != incidentAppearance)
                    characterVisual.sharedMesh = incidentAppearance;
            }
            if (injuryVisual && (!animated || !room.Paused))
            {
                bool controlled = animated && room && (room.scenarioId == 2 || room.scenarioId == 3)
                    && state && state.bleedingControlled;
                injuryVisual.SetActive(emergency && patientId==0 && (state==null || !state.stabilized) && !controlled
                    && (!animated || !animationDriver.HasHandPresentation));
            }
            // Older scenes animate the mannequin itself. Cast scenes keep the inspection collider fixed.
            if (!characterVisual && body)
            {
                body.localScale = baseScale + Vector3.up * (Mathf.Sin(Time.time * (emergency ? 5 : 2)) * .018f);
                if (patientId == 0)
                {
                    Quaternion target = emergency && state && !state.stabilized ? Quaternion.Euler(-10, 0, 10) : baseRotation;
                    body.localRotation = Quaternion.Slerp(body.localRotation, target, Time.deltaTime * 2);
                }
            }
        }
    }
}


