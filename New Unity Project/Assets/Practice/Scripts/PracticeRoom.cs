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
        public void TogglePause()
        {
            resetConfirm=false;
            Paused=!Paused;
        }
        public bool EmergencyActive { get; private set; }
        ScenarioDefinition runtimeDefinition;
        public ScenarioDefinition Definition => scenarioId<0?null:runtimeDefinition??ScenarioCatalog.All[scenarioId];
        public string feedback="", observation="";
        public int PatientInSlot(int slot)
        {
            // The A/B/C markers are ordered left to right from the entrance.
            return System.Array.IndexOf(patientLabels, ((char)('A' + slot)).ToString());
        }
        string updatedObservation;
        public int inspected=-1;
        public bool showHelp=true;
        float actionCooldown;
        PracticeSession session;
        PracticeAudio audioSystem;
        RookInstructor instructor;
        ScenarioRoutine routine;
        float visibleSeconds;
        float observationWait;
        bool promptedToLook;
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
                if (!session.certification)
                {
                    feedback = "Observe the room. I’ll guide you when someone needs help.";
                    instructor = gameObject.AddComponent<RookInstructor>();
                    instructor.Present(feedback);
                }
                PlacePatients();
                routine = ScenarioRoutine.StartFor(this);
            }
            if(incidentRoot) incidentRoot.SetActive(false);
        }
        void Update()
        {
            if(Input.GetKeyDown(KeyCode.Escape)) TogglePause();
            if(scenarioId<0 || Paused || result!=null || session.loading) return;
            if(!EmergencyActive)
            {
                calmTime-=Time.deltaTime;
                if(calmTime<=0 && (!routine || routine.ReadyForIncident))
                {
                    // A muted certification run also needs a perceptible first cue.
                    bool needsSight = !session.certification || session.muted;
                    if (needsSight)
                    {
                        var eye = Camera.main;
                        visibleSeconds = LeadInView(eye) ? visibleSeconds + Time.deltaTime : 0f;
                        observationWait += Time.deltaTime;
                        if (instructor && eye && !promptedToLook && observationWait > 5f)
                        {
                            Vector3 relative = eye.transform.InverseTransformPoint(patients[0].transform.position);
                            string direction = relative.z < 0f ? "behind you" : relative.x < 0f ? "to your left" : "to your right";
                            instructor.Present("Look " + direction + " and watch how the person moves.");
                            promptedToLook = true;
                        }
                    }
                    if (!needsSight || visibleSeconds >= .15f) BeginEmergency();
                }
                return;
            }
            elapsed+=Time.deltaTime;
            if(session.certification && elapsed>=Definition.timeLimit) Complete(true);
        }
        public void BeginEmergency()
        {
            if(scenarioId<0 || EmergencyActive) return;
            if (routine) routine.InterruptForCare();
            EmergencyActive=true;elapsed=0;
            if(incidentRoot) incidentRoot.SetActive(true);
            audioSystem.SignalIncident(patients[0].transform);
            feedback = session.certification ? "Incident active." : "ROOK: Inspect the patients and choose who needs care first.";
            PresentInstruction();
        }
        bool LeadInView(Camera eye)
        {
            if (!eye) return false;
            Transform patient = patients[0].transform;
            Vector3 target = patient.position + Vector3.up * 1.28f;
            Vector3 screen = eye.WorldToViewportPoint(target);
            // The desktop brief occupies the right quarter; keep the cue inside the clear viewing area.
            if (screen.z < eye.nearClipPlane || screen.x < .27f || screen.x > .71f ||
                screen.y < .15f || screen.y > .83f) return false;
            return !Physics.Linecast(eye.transform.position, target, out var hit, eye.cullingMask,
                       QueryTriggerInteraction.Ignore) || hit.transform.IsChildOf(patient);
        }
        void PresentInstruction()
        {
            if (!instructor || session.certification) return;
            instructor.Present(feedback);
        }
        public void Inspect(int id)
        {
            if(!EmergencyActive || result!=null || id<0 || id>2) return;
            if(encounter.SelectedPatient>=0 && encounter.SelectedPatient!=id) return;
            inspected=id;
            observation=id==0?(updatedObservation ?? Definition.symptoms):id==1?"Alert, speaking in full sentences. Small superficial hand scrape, no heavy bleeding.":"Walking and talking normally. Shaken by the incident but reports no injury.";
        }
        public void ChoosePatient(int id)
        {
            if(!EmergencyActive || result!=null || encounter.SelectedPatient>=0) return;
            bool correct=encounter.ChoosePatient(id,elapsed);
            if(encounter.SelectedPatient>=0) Inspect(encounter.SelectedPatient);
            feedback=session.certification?"Patient selection recorded.":correct?"ROOK: Correct priority. Identify the main condition.":"ROOK: Breathing compromise or uncontrolled bleeding takes priority over minor injuries. Inspect again.";
            PresentInstruction();
        }
        public void Diagnose(int id)
        {
            if(!EmergencyActive || result!=null || encounter.SelectedPatient<0 || encounter.DiagnosedAs>=0) return;
            bool correct=encounter.Diagnose(id,elapsed);
            feedback=session.certification?"Assessment recorded.":correct?"ROOK: Correct. "+Definition.instructions[0]:"ROOK: Recheck the observed signs. That diagnosis does not fit this case.";
            PresentInstruction();
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
                PresentInstruction();
            }
            else { feedback="ROOK: "+ScenarioCatalog.WhyWrong(action)+" "+Definition.instructions[encounter.Step];PresentInstruction(); }
            if(correct && action=="shelter" && patients!=null) patients[0].transform.position=new Vector3(-5,.2f,3);
            if(correct && earlyResponse && (action=="backblows" || action=="pressure")) observation=updatedObservation=scenarioId==1?"The obstruction has cleared. The patient is breathing and speaking.":"Bleeding has stopped with maintained pressure. Continue monitoring.";
            if(correct && action=="monitor" && patients!=null && patients[0].injuryVisual) patients[0].injuryVisual.SetActive(false);
            if(encounter.Step>=Definition.actions.Length) Complete();
        }
        public void RecordPlacementError(string action)
        {
            if(encounter==null || result!=null || encounter.DiagnosedAs<0)return;
            encounter.PlacementError(action,elapsed);patients[0].GetComponent<PatientManager>().Apply(action,false);
            feedback=PracticeSession.Get().certification?"Action recorded.":"ROOK: Incorrect placement. Check the highlighted anatomical target and reposition the tool.";
            PresentInstruction();
        }
        public void Complete(bool timeout=false)
        {
            if(result!=null || encounter==null) return;
            result=encounter.Finish(elapsed,timeout);
            var patientState=patients[0].GetComponent<PatientManager>();
            result.patientStabilized=patientState.stabilized;result.patientWorsened=!patientState.stabilized && patientState.stability<70;
            audioSystem.Feedback(result.completed);
            if (instructor)
                instructor.Present(result.score == 100 ? "Care sequence complete. You mastered this scenario."
                    : "Review your training score and retry the case to practice the missed steps.");
            session.Record(result);
        }
        public bool PointerOverUI(Vector3 point)
        {
            if (Paused || result!=null || (session && (session.dashboard || session.loading))) return true;
            var hudPoint=new Vector2((point.x-HudLeft)/HudScale,(Screen.height-point.y)/HudScale);
            for(int i=0;i<drawnPanels.Count;i++) if(drawnPanels[i].Contains(hudPoint)) return true;
            if (!instructor) return false;
            var display=instructor.transform.Find("ROOK instructor display");
            if(!display || !display.GetComponent<Canvas>().isActiveAndEnabled) return false;
            var card=display.Find("Guidance") as RectTransform;
            return card && RectTransformUtility.RectangleContainsScreenPoint(card,point);
        }
    }
}
