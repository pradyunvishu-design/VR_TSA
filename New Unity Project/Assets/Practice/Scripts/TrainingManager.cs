using UnityEngine;
namespace First180.Practice
{
    public class TrainingManager : MonoBehaviour
    {
        public string currentInstruction;
        public int attemptErrors,masteryScore;
        PracticeRoom room;TreatmentTarget[] targets;TreatmentTool[] tools;
        void Start(){room=GetComponent<PracticeRoom>();targets=FindObjectsByType<TreatmentTarget>(FindObjectsSortMode.None);tools=FindObjectsByType<TreatmentTool>(FindObjectsSortMode.None);}
        void Update()
        {
            bool hints=!PracticeSession.Get().certification && room.encounter!=null && room.encounter.DiagnosedAs>=0 && room.result==null;
            string action=hints?room.Definition.actions[Mathf.Min(room.encounter.Step,room.Definition.actions.Length-1)]:"";
            currentInstruction=hints?room.Definition.instructions[room.encounter.Step]:"";
            attemptErrors=room.encounter?.Mistakes??0;masteryScore=room.result?.score??0;
            foreach(var target in targets)if(target){var r=target.GetComponent<Renderer>();if(r)r.enabled=hints && target.patientId==0 && target.Accepts(action);}
            foreach(var tool in tools)if(tool){var light=tool.GetComponent<Light>();if(light)light.enabled=hints && tool.actionId==action;}
        }
    }
}
