using UnityEngine;

namespace First180.Practice
{
    public class PracticePatient : MonoBehaviour
    {
        public int patientId;
        public Transform body;
        public GameObject injuryVisual;
        Vector3 baseScale;
        Quaternion baseRotation;
        void Awake() { if(body!=null){baseScale=body.localScale;baseRotation=body.localRotation;} }
        void Update()
        {
            var room=FindFirstObjectByType<PracticeRoom>();
            bool emergency=room!=null && room.EmergencyActive;
            var state=GetComponent<PatientManager>();
            if(injuryVisual!=null) injuryVisual.SetActive(emergency && patientId==0 && (state==null || !state.stabilized));
            if(body!=null) body.localScale=baseScale+new Vector3(0,Mathf.Sin(Time.time*(emergency?5:2))*0.018f,0);
            if(body && patientId==0)body.localRotation=Quaternion.Slerp(body.localRotation,emergency && state && !state.stabilized?Quaternion.Euler(-10,0,10):baseRotation,Time.deltaTime*2);
        }
    }
}
