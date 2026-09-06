using UnityEngine;
namespace First180.Practice
{
    public class TreatmentManager : MonoBehaviour
    {
        public TreatmentTool held;
        public string status="Pick up equipment: E / grip. Return equipment: Q / release grip.";
        public float progress;
        TreatmentTarget current;bool latched;
        public void Grab(TreatmentTool tool,Transform hand){if(held || !tool || tool.Held)return;held=tool;tool.Grab(hand);progress=0;latched=false;}
        public void Release(){if(held)held.Release();held=null;current=null;progress=0;latched=false;}
        public void Use(TreatmentTarget target,bool pressed,float dt)
        {
            var room=GetComponent<PracticeRoom>();
            if(!held || !room.EmergencyActive || room.result!=null || room.Paused)return;
            if(!pressed){progress=0;latched=false;current=null;return;}
            if(latched)return;
            bool standalone=held.actionId=="call";
            if(!standalone && (!target || target.patientId!=room.encounter.SelectedPatient)){progress=0;status="Bring the tool to the selected patient's relevant body area.";return;}
            if(current!=target){progress=0;current=target;}
            progress+=dt;status="Using "+held.name+" / "+Mathf.RoundToInt(100*progress/held.holdSeconds)+"%";
            if(progress<held.holdSeconds)return;
            latched=true;progress=0;
            if(!standalone && !target.Accepts(held.actionId)){room.RecordPlacementError(held.actionId);status="Placement recorded.";return;}
            room.Perform(held.actionId);status="Interaction recorded. Release trigger before another action.";
        }
        void OnDisable(){Release();}
    }
}
