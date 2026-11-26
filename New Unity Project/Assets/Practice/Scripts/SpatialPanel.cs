using System;
using UnityEngine;
using UnityEngine.UI;

namespace First180.Practice
{
    // The board stays in the room until recalled. World-space button colliders work with either hand ray.
    public class SpatialPanel : MonoBehaviour
    {
        const float Width = 1.36f;
        PracticeRoom room;
        PracticeSession session;
        TreatmentManager treatment;
        Transform content;
        RectTransform typeArea;
        Material backing, buttonFace;
        Font font;
        Texture2D pulseWordmark;
        Text body, patientIdentity;
        SpatialButton prioritize;
        string stage = "";
        float nextRefresh;
        int page;
        bool showingObservation;

        void Start()
        {
            room=GetComponent<PracticeRoom>();
            session=PracticeSession.Get();
            treatment=GetComponent<TreatmentManager>();
            pulseWordmark=Resources.Load<Texture2D>("PulseWordmark");
            font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var shader=Resources.Load<Shader>("PracticePanelSurface");
            backing=new Material(shader){color=new Color(.025f,.05f,.075f)};
            buttonFace=new Material(shader){color=new Color(.055f,.24f,.27f)};
            content=new GameObject("Spatial decision panel").transform;
            content.SetParent(transform,true);
            content.gameObject.SetActive(false);
        }

        public void Recenter()
        {
            if (!content || !Camera.main) return;
            Transform head=Camera.main.transform;
            var horizontal=Vector3.ProjectOnPlane(head.forward,Vector3.up);
            if (horizontal.sqrMagnitude < .001f) horizontal=Vector3.Cross(head.right,Vector3.up);
            var towardBoard=Quaternion.AngleAxis(24f,Vector3.up)*horizontal.normalized;
            content.SetPositionAndRotation(head.position+towardBoard*1.85f,Quaternion.LookRotation(towardBoard));
        }

        void Update()
        {
            if (Time.unscaledTime < nextRefresh) return;
            nextRefresh=Time.unscaledTime+.15f;
            RefreshPanel(VRInteractionManager.Active);
        }

        internal void RefreshPanel(bool visible)
        {
            if (!content) return;
            if (content.gameObject.activeSelf!=visible)
            {
                content.gameObject.SetActive(visible);
                if (visible) Recenter();
            }
            if (!visible) return;
            string next=room.Paused?"paused":session.dashboard?"dashboard":room.result!=null?"result":
                room.scenarioId<0?"hub":!room.EmergencyActive?"normal":room.encounter.SelectedPatient<0?"triage":
                room.encounter.DiagnosedAs<0?"diagnose":"care";
            if (stage!=next)
            {
                stage=next;
                page=0;
                showingObservation=false;
                Build();
            }
            if (prioritize) prioritize.SetAvailable(room.inspected>=0);
            if (patientIdentity) patientIdentity.text=room.PatientPanelIdentity;
            body.text=Body();
        }

        Text Label(string value,float left,float top,float width,float height,int fontSize, bool bold=false)
        {
            var go=new GameObject("Panel label",typeof(RectTransform),typeof(Text));
            var rect=(RectTransform)go.transform;
            rect.SetParent(typeArea,false);
            rect.anchorMin=rect.anchorMax=new Vector2(.5f,.5f);
            rect.pivot=new Vector2(0,1);
            rect.anchoredPosition=new Vector2(left,top);
            rect.sizeDelta=new Vector2(width,height);
            var text=go.GetComponent<Text>();
            text.font=font; text.fontSize=fontSize;
            text.fontStyle=bold?FontStyle.Bold:FontStyle.Normal;
            text.color=Color.white; text.raycastTarget=false;
            text.horizontalOverflow=HorizontalWrapMode.Wrap;
            text.verticalOverflow=VerticalWrapMode.Truncate;
            text.resizeTextForBestFit=true;
            text.resizeTextMinSize=24; text.resizeTextMaxSize=fontSize;
            text.text=value;
            return text;
        }

        SpatialButton Button(string label,int row,Action action,bool available=true)
        {
            float y=.055f-row*.117f;
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name=label;go.transform.SetParent(content,false);
            go.transform.localPosition=new Vector3(0,y,-.02f);
            go.transform.localScale=new Vector3(Width-.12f,.101f,.02f);
            go.GetComponent<Collider>().isTrigger=true;
            go.GetComponent<Renderer>().sharedMaterial=buttonFace;
            var button=go.AddComponent<SpatialButton>(); button.action=action;button.SetAvailable(available);
            Label(label,-575,y*1000+48,1150,96,36).alignment=TextAnchor.MiddleLeft;
            return button;
        }

        void Build()
        {
            foreach (Transform child in content)
            {
                child.gameObject.SetActive(false);
                Destroy(child.gameObject);
            }
            prioritize=null;
            patientIdentity=null;
            var bg=GameObject.CreatePrimitive(PrimitiveType.Cube);
            bg.name="Panel backing";bg.transform.SetParent(content,false);
            bg.transform.localScale=new Vector3(Width,1.33f,.018f);
            bg.GetComponent<Renderer>().sharedMaterial=backing;
            bg.GetComponent<Collider>().isTrigger=true;

            var canvasObject=new GameObject("Panel typography",typeof(RectTransform),typeof(Canvas));
            typeArea=(RectTransform)canvasObject.transform;
            typeArea.SetParent(content,false);typeArea.sizeDelta=new Vector2(1360,1330);
            typeArea.localScale=Vector3.one*.001f;
            typeArea.localPosition=Vector3.back*.034f;
            canvasObject.GetComponent<Canvas>().renderMode=RenderMode.WorldSpace;
            string title=stage switch {
                "hub"=>"Training hub","paused"=>"Paused","normal"=>room.Definition.location,
                "triage"=>"Patient assessment","diagnose"=>"Identify the condition","care"=>session.certification?"Certification":"Guided practice",
                "dashboard"=>page==0?"Certification results":ScenarioCatalog.All[session.results[page-1].scenario].title,
                _=>session.certification?"Case recorded":"Training debrief"
            };
            var logo = new GameObject("PULSE",typeof(RectTransform),typeof(RawImage)).GetComponent<RawImage>();
            logo.rectTransform.SetParent(typeArea,false);
            logo.rectTransform.anchoredPosition = new Vector2(495,566);
            logo.rectTransform.sizeDelta = new Vector2(216,60);
            logo.texture = pulseWordmark;
            logo.raycastTarget = false;
            Label(title,-600,608,950,82,49,true);
            bool tall=stage=="care"||stage=="result"||stage=="dashboard"||stage=="normal";
            bool hasPatient=stage=="triage" || stage=="diagnose" || stage=="care";
            if (hasPatient)
            {
                patientIdentity=Label(room.PatientPanelIdentity,-595,506,1184,50,32,true);
                patientIdentity.color=new Color(.42f,.94f,.83f);
            }
            float bodyTop=hasPatient?438:497;
            body=Label("",-595,bodyTop,1184,tall?(hasPatient?527:590):(hasPatient?298:363),35);
            if (stage=="hub")
            {
                for(int i=0;i<5;i++){int id=i;Button(ScenarioCatalog.All[i].title+"   "+session.best[i]+"%",i,()=>session.StartTraining(id));}
                Button(session.CanCertify?"Start certification":"Certification locked",5,()=>session.StartCertification(),session.CanCertify);
            }
            else if(stage=="triage")
            {
                for(int slot=0;slot<3;slot++)
                {
                    int id=room.PatientInSlot(slot);
                    Button("Inspect patient "+room.patientLabels[id],slot,()=>{room.Inspect(id);RefreshPanel(true);});
                }
                prioritize=Button("Prioritize inspected patient",3,()=>room.ChoosePatient(room.inspected),room.inspected>=0);
            }
            else if(stage=="diagnose")
            {
                for(int i=0;i<5;i++){int id=i;Button(ScenarioCatalog.All[i].title,i,()=>room.Diagnose(id));}
            }
            else if(stage=="care")
            {
                Button("Reassess patient",2,()=>{room.Inspect(room.encounter.SelectedPatient);showingObservation=!showingObservation;});
                if(session.certification) Button("Finish case and hand over",3,()=>room.Complete());
                Button("Return to hub",4,()=>session.BackToHub());
            }
            else if(stage=="result")
            {
                if(session.certification) Button(session.round==4?"View final results":"Continue to next case",3,()=>session.Next());
                else {Button("Retry training",3,()=>session.StartTraining(room.scenarioId));Button("Return to hub",4,()=>session.BackToHub());}
            }
            else if(stage=="dashboard")
            {
                if (session.results.Count>0)
                {
                    Button(page==0?"Review first case":page==session.results.Count?"Back to summary":"Review next case",2,()=>{
                        page=(page+1)%(session.results.Count+1);Build();body.text=Body();
                    });
                }
                Button("Export analytics",3,()=>session.Export());
                Button("Return to hub",4,()=>session.BackToHub());
            }
            else if(stage=="paused")
            {
                Button("Resume",0,()=>room.TogglePause());
                Button("Toggle sound",1,()=>session.muted=!session.muted);
                Button("Return to hub",2,()=>session.BackToHub());
            }
        }

        string Body()
        {
            switch(stage)
            {
                case "hub": return "Earn 100% in every scenario to unlock certification.\n\nWalk up to a scenario or point and press trigger. Left stick moves, right stick turns. Left X teleports. Right B recalls this panel.";
                case "normal": return room.Definition.normal;
                case "paused": return "Sound "+(session.muted?"off":"on")+"\nUse either trigger to select. Right B recalls this panel.";
                case "triage": case "diagnose": return room.inspected>=0?"Findings\n"+room.observation:"Select a patient to record your observations.";
                case "care":
                    string detail=showingObservation?"Reassessment\n"+room.observation:session.certification
                        ?"Elapsed "+room.elapsed.ToString("0")+" s  /  Limit "+room.Definition.timeLimit+" s"
                        :room.TrainingStepLabel+"\n"+room.CurrentTrainingInstruction;
                    string status=treatment.held?treatment.held.name+"\n"+treatment.status:"Grip to pick up equipment. Hold the trigger at the body target. Release grip to return it.";
                    return detail+"\n\n"+status;
                case "result": return session.certification?"Case "+(session.round+1)+" of 5 recorded.":"Score  "+room.result.score+"%\nErrors  "+room.result.mistakes+"\nActions  "+room.result.stepsCompleted+" / "+room.result.stepsRequired+"\n\n"+(room.result.score==100?"Scenario mastered.":"Earn 100% to master this scenario.");
                case "dashboard":
                    string result;
                    if(page==0) result=AnalyticsManager.Summary(session.results);
                    else
                    {
                        var r=session.results[page-1];
                        result="Case "+page+" of "+session.results.Count+"\nPrioritization  "+r.priority+"%      Diagnosis  "+r.diagnosis+"%\nTreatment quality  "+r.quality+"%      Efficiency  "+r.efficiency+"%\nCorrect actions  "+r.correctActions+"      Incorrect  "+r.incorrectActions+"\nCritical mistakes  "+r.criticalMistakes+"\nFirst response  "+(r.events.Count==0?"No response":r.firstActionSeconds.ToString("0.0")+" s")+"      Total  "+r.seconds.ToString("0.0")+" s\nPatient  "+(r.patientStabilized?"Stabilized":r.patientWorsened?"Worsened":"Monitored");
                    }
                    if(session.exportStatus.Length>0) result+="\n"+(session.exportStatus.StartsWith("Export failed",StringComparison.Ordinal)?"Export failed.":"Analytics saved on this device.");
                    return result;
                default: return "";
            }
        }

        internal bool OwnsSurface(Collider hit) => hit && hit.transform.parent==content;

        void OnDestroy()
        {
            if (backing) Destroy(backing);
            if (buttonFace) Destroy(buttonFace);
        }
    }
}
