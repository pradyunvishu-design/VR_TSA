using System;
using UnityEngine;
namespace First180.Practice
{
    // Spatial decision UI. Clinical treatments are exclusively equipment interactions.
    public class SpatialPanel : MonoBehaviour
    {
        PracticeRoom room;PracticeSession session;Transform content;TextMesh heading,body;string stage="";float refresh;
        Material background,buttonMaterial;
        void Start()
        {
            room=GetComponent<PracticeRoom>();session=PracticeSession.Get();
            background=new Material(Shader.Find("Unlit/Color")){color=new Color(.025f,.05f,.075f)};
            buttonMaterial=new Material(Shader.Find("Unlit/Color")){color=new Color(.055f,.24f,.27f)};
            var root=new GameObject("Spatial decision panel");content=root.transform;Recenter();
            UpdatePanel();
        }
        public void Recenter()
        {
            if(!content || !Camera.main)return;
            var head=Camera.main.transform;var forward=Vector3.ProjectOnPlane(head.forward,Vector3.up).normalized;
            content.position=head.position+forward*2.1f;content.rotation=Quaternion.LookRotation(forward);
        }
        TextMesh Text(string text,Vector3 position,float size,Transform parent)
        {
            var o=new GameObject("Panel text");o.transform.SetParent(parent,false);o.transform.localPosition=position;
            var t=o.AddComponent<TextMesh>();t.text=text;t.anchor=TextAnchor.UpperLeft;t.fontSize=64;t.characterSize=size;t.color=Color.white;return t;
        }
        void Button(string label,int row,Action action)
        {
            var o=GameObject.CreatePrimitive(PrimitiveType.Cube);o.name=label;o.transform.SetParent(content,false);o.transform.localPosition=new Vector3(0,.15f-row*.15f,-.02f);o.transform.localScale=new Vector3(1.1f,.125f,.025f);o.GetComponent<Renderer>().sharedMaterial=buttonMaterial;
            o.AddComponent<SpatialButton>().action=action;Text(label,new Vector3(-.5f,.197f-row*.15f,-.038f),.026f,content);
        }
        void Update(){if(Time.unscaledTime<refresh)return;refresh=Time.unscaledTime+.2f;UpdatePanel();}
        string Wrap(string text,int width=60)
        {
            string output="";int count=0;foreach(var word in text.Split(' ')){if(count+word.Length>width){output+="\n";count=0;}output+=word+" ";count+=word.Length+1;}return output;
        }
        void UpdatePanel()
        {
            if(!content)return;
            content.gameObject.SetActive(VRInteractionManager.Active);
            string next=room.Paused?"paused":session.dashboard?"dashboard":room.result!=null?"result":room.scenarioId<0?"hub":!room.EmergencyActive?"normal":room.encounter.SelectedPatient<0?"triage":room.encounter.DiagnosedAs<0?"diagnose":"care";
            if(stage!=next)
            {
                stage=next;foreach(Transform child in content)Destroy(child.gameObject);
                var bg=GameObject.CreatePrimitive(PrimitiveType.Cube);bg.name="Readable panel backing";bg.transform.SetParent(content,false);bg.transform.localScale=new Vector3(1.25f,1.65f,.02f);bg.GetComponent<Renderer>().sharedMaterial=background;Destroy(bg.GetComponent<Collider>());
                heading=Text("FIRST180 / "+stage.ToUpper(),new Vector3(-.55f,.74f,-.03f),.034f,content);
                body=Text("",new Vector3(-.55f,.6f,-.035f),.023f,content);
                if(stage=="hub")
                {
                    for(int i=0;i<5;i++){int id=i;Button(ScenarioCatalog.All[i].title+" / "+session.best[i]+"%",i,()=>session.StartTraining(id));}
                    Button(session.CanCertify?"Start certification":"Certification locked / need 5 x 100%",5,()=>session.StartCertification());
                }
                if(stage=="triage")for(int i=0;i<3;i++){int id=i;Button("Inspect patient "+room.patientLabels[i],i*2,()=>room.Inspect(id));Button("Prioritize patient "+room.patientLabels[i],i*2+1,()=>room.ChoosePatient(id));}
                if(stage=="diagnose")for(int i=0;i<5;i++){int id=i;Button(ScenarioCatalog.All[i].title,i,()=>room.Diagnose(id));}
                if(stage=="care"){Button("Reassess selected patient",3,()=>room.Inspect(room.encounter.SelectedPatient));if(session.certification)Button("Finish case / hand over",4,()=>room.Complete());Button("Abandon case / lobby",5,()=>session.BackToHub());}
                if(stage=="result")
                {
                    if(session.certification)Button(session.round==4?"Final analytics":"Next case",3,()=>session.Next());
                    else{Button("Retry training",3,()=>session.StartTraining(room.scenarioId));Button("Return to lobby",4,()=>session.BackToHub());}
                }
                if(stage=="dashboard"){Button("Return to lobby",4,()=>session.BackToHub());Button("Export local analytics JSON",5,()=>session.Export());}
                if(stage=="paused"){Button("Resume",0,()=>room.TogglePause());Button("Toggle sound",1,()=>session.muted=!session.muted);Button("Abandon / return to lobby",2,()=>session.BackToHub());}
            }
            if(!body)return;
            if(stage=="hub")body.text="Personal practice / not a clinical credential\nGrip: equipment. Trigger: select / use.\nLeft stick: move. Right stick: snap turn.\nLeft X: floor teleport. Right B: recall panel.";
            else if(stage=="normal")body.text=Wrap(room.Definition.normal+" Stay observant.");
            else if(stage=="triage" || stage=="diagnose")body.text=Wrap(room.observation);
            else if(stage=="care")body.text=Wrap(session.certification?room.observation:room.feedback)+"\n\n"+Wrap(room.GetComponent<TreatmentManager>().status);
            else if(stage=="result")body.text=session.certification?"Case recorded. Feedback follows all five cases.":"TRAINING SCORE: "+room.result.score+"%\nErrors: "+room.result.mistakes+"\n100% is required for mastery.";
            else if(stage=="dashboard")body.text=AnalyticsManager.Summary(session.results);
            else if(stage=="paused")body.text="Simulation paused. Left menu: resume.\nRight B: recall this panel.";
        }
        void OnDestroy(){if(content)Destroy(content.gameObject);Destroy(background);Destroy(buttonMaterial);}
    }
}
