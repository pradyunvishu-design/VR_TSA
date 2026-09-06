using UnityEngine;
namespace First180.Practice
{
    // Deliberately normalized game physiology. These numbers are not medical predictions.
    public class PatientManager : MonoBehaviour
    {
        public float heartRate=76, breathing=1, bloodLoss, airway=1, consciousness=1, pain, severity, stability=100;
        public bool treated, stabilized, bleedingControlled;
        public string treatmentStatus="Awaiting assessment";
        public float deteriorationRate=.3f;
        PracticeRoom room; PracticePatient patient;
        public void Initialize(PracticeRoom owner,PracticePatient p){room=owner;patient=p;}
        public void Tick(float dt)
        {
            if(room==null || patient.patientId!=0 || !room.EmergencyActive || room.Paused || room.result!=null)return;
            stability=Mathf.Clamp(stability+(stabilized?2:treated?.2f:-deteriorationRate)*dt,0,100);
            severity=100-stability;pain=stabilized?2:Mathf.Lerp(4,9,severity/100);
            bool respiratory=room.scenarioId<2;
            if(respiratory){airway=treated?1:Mathf.Clamp01(.5f-severity*.007f);breathing=airway;}
            if(room.scenarioId==2 || room.scenarioId==3)bloodLoss=Mathf.Clamp01(bloodLoss+(bleedingControlled?0:.003f)*dt);
            heartRate=stabilized?Mathf.MoveTowards(heartRate,80,dt*4):stability<25?Mathf.Lerp(48,100,stability/25):Mathf.Lerp(150,95,stability/100);
            consciousness=stability<20?.25f:1;
            if(stability<=0)room.Complete(true);
        }
        void Update(){Tick(Time.deltaTime);}
        public void Apply(string action,bool correct)
        {
            if(!correct){stability=Mathf.Max(0,stability-6);treatmentStatus="Condition worsened after incorrect action";return;}
            treatmentStatus=ScenarioCatalog.Label(action);
            if(action=="epinephrine" || action=="thrusts" || action=="shelter" || action=="protect")treated=true;
            if(action=="tourniquet"){treated=true;bleedingControlled=true;}
            if(action=="pressure")bleedingControlled=room.earlyResponse;
            if(action=="backblows" && room.earlyResponse)treated=true;
            if(action=="monitor"){stabilized=true;treated=true;stability=Mathf.Max(65,stability);}
        }
    }
}
