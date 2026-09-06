using UnityEngine;

namespace First180.Practice
{
    [RequireComponent(typeof(PracticeAudio))]
    public partial class PracticeRoom : MonoBehaviour
    {
        public int scenarioId=-1;
        public ScenarioConfig config;
        public bool earlyResponse;
        public string[] patientLabels={"A","B","C"};
        public GameObject incidentRoot;
        public PracticePatient[] patients;
        public Encounter encounter;
        [System.NonSerialized] public CaseResult result;
        public float elapsed, calmTime;
        public bool Paused { get; private set; }
        public void TogglePause(){Paused=!Paused;}
        public bool EmergencyActive { get; private set; }
        ScenarioDefinition runtimeDefinition;
        public ScenarioDefinition Definition => scenarioId<0?null:runtimeDefinition??ScenarioCatalog.All[scenarioId];
        public string feedback="", observation="";
        public int inspected=-1;
        public bool showHelp=true;
        float actionCooldown;
        PracticeSession session;
        PracticeAudio audioSystem;
        void Start()
        {
            result=null;
            session=PracticeSession.Get();audioSystem=GetComponent<PracticeAudio>();
            if(scenarioId>=0)
            {
                runtimeDefinition=JsonUtility.FromJson<ScenarioDefinition>(JsonUtility.ToJson(ScenarioCatalog.All[scenarioId]));
                runtimeDefinition.timeLimit=config?config.certificationSeconds:180;
                earlyResponse=session.certification && (config==null || config.allowEarlyResponseVariant) && (scenarioId==1 || scenarioId==2) && Random.value>.5f;
                if(earlyResponse)
                {
                    var actions=new System.Collections.Generic.List<string>(runtimeDefinition.actions);
                    actions.Remove(scenarioId==1?"thrusts":"tourniquet");runtimeDefinition.actions=actions.ToArray();
                }
                encounter=new Encounter(scenarioId,session.certification,Definition);
                calmTime=session.certification?Random.Range(config?config.onsetSeconds.x:8,config?config.onsetSeconds.y:18):2f;
                foreach(var p in patients)
                {
                    var state=p.GetComponent<PatientManager>();if(!state)state=p.gameObject.AddComponent<PatientManager>();
                    state.Initialize(this,p);state.deteriorationRate=session.certification?Random.Range(config?config.deteriorationPerSecond.x:.22f,config?config.deteriorationPerSecond.y:.38f):.08f;
                    if(session.certification && p.patientId==0)state.stability=Random.Range(75f,96f);
                }
                feedback="ROOK online. Observe the room.";
                if(patients!=null && patients.Length==3 && session.certification)
                {
                    // Patient identities move between positions, so the severe case is not always centered.
                    Vector3[] positions={patients[0].transform.position,patients[1].transform.position,patients[2].transform.position};
                    int shift=Random.Range(0,3);
                    for(int i=0;i<3;i++) patients[i].transform.position=positions[(i+shift)%3];
                    for(int i=0;i<3;i++)patientLabels[i]=((char)('A'+(i+shift)%3)).ToString();
                }
                for(int i=0;i<patients.Length;i++)foreach(var t in patients[i].GetComponentsInChildren<TextMesh>())if(t.name=="Patient marker")t.text="PATIENT "+patientLabels[i];
            }
            if(incidentRoot) incidentRoot.SetActive(false);
        }
        void Update()
        {
            if(Input.GetKeyDown(KeyCode.Escape)) Paused=!Paused;
            if(scenarioId<0 || Paused || result!=null || session.loading) return;
            if(!EmergencyActive)
            {
                calmTime-=Time.deltaTime;
                if(calmTime<=0) BeginEmergency();
                return;
            }
            elapsed+=Time.deltaTime;
            if(session.certification && elapsed>=Definition.timeLimit) Complete(true);
        }
        public void BeginEmergency()
        {
            if(scenarioId<0 || EmergencyActive) return;
            EmergencyActive=true;elapsed=0;
            if(incidentRoot) incidentRoot.SetActive(true);
            audioSystem.Alarm();
            feedback="ROOK: Inspect the patients and choose who needs care first.";
            audioSystem.Speak("intro");
        }
        public void Inspect(int id)
        {
            if(!EmergencyActive || result!=null || id<0 || id>2) return;
            inspected=id;
            observation=id==0?Definition.symptoms:id==1?"Alert, speaking in full sentences. Small superficial hand scrape, no heavy bleeding.":"Walking and talking normally. Shaken by the incident but reports no injury.";
        }
        public void ChoosePatient(int id)
        {
            if(!EmergencyActive || result!=null || encounter.SelectedPatient>=0) return;
            bool correct=encounter.ChoosePatient(id,elapsed);
            feedback=session.certification?"Patient selection recorded.":correct?"ROOK: Correct priority. Identify the main condition.":"ROOK: Breathing compromise or uncontrolled bleeding takes priority over minor injuries. Inspect again.";
            if(!session.certification) audioSystem.Speak(correct?"diagnose":"priority_wrong");
        }
        public void Diagnose(int id)
        {
            if(!EmergencyActive || result!=null || encounter.SelectedPatient<0 || encounter.DiagnosedAs>=0) return;
            bool correct=encounter.Diagnose(id,elapsed);
            feedback=session.certification?"Assessment recorded.":correct?"ROOK: Correct. "+Definition.instructions[0]:"ROOK: Recheck the observed signs. That diagnosis does not fit this case.";
            if(!session.certification) audioSystem.Speak(correct?"case"+scenarioId+"_0":"diagnosis_wrong");
        }
        public void Perform(string action)
        {
            if(!EmergencyActive || result!=null || encounter.DiagnosedAs<0 || Time.unscaledTime<actionCooldown) return;
            actionCooldown=Time.unscaledTime+0.3f;
            bool correct=encounter.Perform(action,elapsed);
            audioSystem.Feedback(session.certification?true:correct);
            if(patients!=null)patients[0].GetComponent<PatientManager>().Apply(action,correct);
            if(session.certification) feedback="Action recorded.";
            else if(correct)
            {
                feedback="ROOK: Good. "+(encounter.Step<Definition.actions.Length?Definition.instructions[encounter.Step]:"Care sequence complete.");
                audioSystem.Speak(encounter.Step<Definition.actions.Length?"case"+scenarioId+"_"+encounter.Step:"complete");
            }
            else { feedback="ROOK: "+ScenarioCatalog.WhyWrong(action)+" "+Definition.instructions[encounter.Step];audioSystem.Speak("wrong"); }
            if(correct && action=="shelter" && patients!=null) patients[0].transform.position=new Vector3(-5,.2f,3);
            if(correct && earlyResponse && (action=="backblows" || action=="pressure")) observation=scenarioId==1?"The obstruction has cleared. The patient is breathing and speaking.":"Bleeding has stopped with maintained pressure. Continue monitoring.";
            if(correct && action=="monitor" && patients!=null && patients[0].injuryVisual) patients[0].injuryVisual.SetActive(false);
            if(encounter.Step>=Definition.actions.Length) Complete();
        }
        public void RecordPlacementError(string action)
        {
            if(encounter==null || result!=null || encounter.DiagnosedAs<0)return;
            encounter.PlacementError(action,elapsed);patients[0].GetComponent<PatientManager>().Apply(action,false);
            feedback=PracticeSession.Get().certification?"Action recorded.":"ROOK: Incorrect placement. Check the highlighted anatomical target and reposition the tool.";
            audioSystem.Speak("wrong");
        }
        public void Complete(bool timeout=false)
        {
            if(result!=null || encounter==null) return;
            result=encounter.Finish(elapsed,timeout);
            var patientState=patients[0].GetComponent<PatientManager>();
            result.patientStabilized=patientState.stabilized;result.patientWorsened=!patientState.stabilized && patientState.stability<70;
            audioSystem.Feedback(result.completed);
            session.Record(result);
        }
        public bool PointerOverUI(Vector3 point)
        {
            float x=point.x/Screen.width*1600, y=(Screen.height-point.y)/Screen.height*900;
            return Paused || session.dashboard || result!=null || x>1140 || y<85 || y>808;
        }
    }
}
