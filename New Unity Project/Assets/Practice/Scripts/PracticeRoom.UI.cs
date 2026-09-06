using System;
using UnityEngine;

namespace First180.Practice
{
    public partial class PracticeRoom
    {
        GUIStyle titleStyle, textStyle, smallStyle, buttonStyle, statStyle;
        Vector2 actionScroll;
        readonly Color ink=new Color(0.035f,0.065f,0.10f,0.97f);
        readonly Color accent=new Color(0.24f,0.88f,0.77f);
        bool resetConfirm;
        void Styles()
        {
            if(titleStyle!=null) return;
            titleStyle=new GUIStyle(GUI.skin.label){fontSize=28,fontStyle=FontStyle.Bold,wordWrap=true};titleStyle.normal.textColor=Color.white;
            textStyle=new GUIStyle(GUI.skin.label){fontSize=19,wordWrap=true};textStyle.normal.textColor=new Color(0.86f,0.91f,0.95f);
            smallStyle=new GUIStyle(textStyle){fontSize=15};
            statStyle=new GUIStyle(titleStyle){fontSize=42};statStyle.normal.textColor=accent;
            buttonStyle=new GUIStyle(GUI.skin.button){fontSize=17,alignment=TextAnchor.MiddleLeft,wordWrap=true,padding=new RectOffset(14,12,5,5)};
        }
        void Panel(Rect rect) { var old=GUI.color;GUI.color=ink;GUI.DrawTexture(rect,Texture2D.whiteTexture);GUI.color=old; }
        void Label(float x,float y,float w,float h,string text,GUIStyle style=null) { GUI.Label(new Rect(x,y,w,h),text,style??textStyle); }
        bool Button(float x,float y,float w,float h,string text) => GUI.Button(new Rect(x,y,w,h),text,buttonStyle);
        void OnGUI()
        {
            if(session==null || VRInteractionManager.Active) return;
            Styles();GUI.matrix=Matrix4x4.TRS(Vector3.zero,Quaternion.identity,new Vector3(Screen.width/1600f,Screen.height/900f,1));
            Panel(new Rect(0,0,1600,83));
            Label(28,15,450,42,"FIRST / 180",titleStyle);
            Label(280,20,710,40,scenarioId<0?"RESPONSE CAMPUS  /  TRAINING HUB":(session.certification?"CERTIFICATION  /  "+(session.round+1)+" OF 5":"TRAINING  /  "+Definition.title.ToUpper()));
            if(Button(1370,18,200,44,session.muted?"Sound: off":"Sound: on")) session.muted=!session.muted;
            Panel(new Rect(0,816,1600,84));
            Label(26,827,1530,28,"WASD / arrows move  |  Right mouse look  |  E pick up / interact  |  Hold F use at body target  |  Q return tool  |  Esc pause",smallStyle);
            Label(26,858,1470,26,"UNITY PRACTICE PROJECT  •  Simulated adult cases and compressed time. Not medical training or a clinical credential.",smallStyle);
            if(session.loading) { Panel(new Rect(440,320,720,180));Label(480,370,640,70,"Transporting...",titleStyle);return; }
            if(Paused) { DrawPause();return; }
            if(session.dashboard) { DrawDashboard();return; }
            if(scenarioId<0) { DrawHub();return; }
            DrawCase();
        }
        void DrawHub()
        {
            Panel(new Rect(1150,103,430,690));
            Label(1173,125,375,45,"YOUR TRAINING",titleStyle);
            Label(1173,174,375,65,"Walk through a portal, click its window, or choose a scenario here.");
            for(int i=0;i<5;i++)
            {
                if(Button(1173,255+i*67,383,56,ScenarioCatalog.All[i].title+"  /  "+session.best[i]+"%")) session.StartTraining(i);
            }
            GUI.enabled=session.CanCertify;
            if(Button(1173,608,383,58,"Start certification  →")) session.StartCertification();
            GUI.enabled=true;
            Label(1173,678,378,72,session.CanCertify?"Unlocked. Five cases, random order. No robot or correction hints.":"Earn 100% in all five training cases to unlock certification.");
            Panel(new Rect(28,105,530,96));
            Label(48,117,490,35,"ONE CAMPUS. FIVE EMERGENCIES.",textStyle);
            Label(48,156,490,28,"Explore  →  Practice  →  Assess  →  Review",smallStyle);
        }
        void DrawCase()
        {
            Panel(new Rect(28,103,610,82));
            Label(48,114,570,33,Definition.location,titleStyle);
            Label(48,152,560,27,EmergencyActive?"INCIDENT ACTIVE" : "A MOMENT BEFORE...",smallStyle);
            if(result!=null) { DrawResult();return; }
            Panel(new Rect(1150,103,430,690));
            Label(1173,119,380,45,session.certification?"ASSESS & RESPOND":"ROOK / ROBOT GUIDE",titleStyle);
            if(!EmergencyActive)
            {
                Label(1173,181,376,170,Definition.normal);
                Label(1173,382,376,65,"Explore the setting. Stay observant.");
                return;
            }
            if(session.certification) Label(1173,169,375,32,"Elapsed "+elapsed.ToString("0.0")+"s  /  Limit "+Definition.timeLimit+"s",smallStyle);
            else Label(1173,169,375,32,"Attempt errors: "+encounter.Mistakes+"  •  Perfect run required",smallStyle);
            if(encounter.SelectedPatient<0)
            {
                Label(1173,210,375,55,session.certification?"Choose the patient to treat first.":"Inspect each person. Prioritize the most urgent condition.");
                for(int i=0;i<3;i++) if(Button(1173,272+i*44,375,38,"Inspect patient "+patientLabels[i])) Inspect(i);
                Label(1173,413,375,169,observation==""?"Click a patient in the room to see their condition.":observation);
                GUI.enabled=inspected>=0;
                if(Button(1173,603,375,54,"Treat inspected patient first")) ChoosePatient(inspected);
                GUI.enabled=true;
            }
            else if(encounter.DiagnosedAs<0)
            {
                Label(1173,210,375,125,observation);
                for(int i=0;i<5;i++) if(Button(1173,350+i*54,375,46,ScenarioCatalog.All[i].title)) Diagnose(i);
            }
            else
            {
                if(!session.certification)
                {
                    Label(1173,210,375,143,Definition.instructions[Mathf.Min(encounter.Step,Definition.instructions.Length-1)]);
                    Label(1173,355,375,28,"NEXT: "+ScenarioCatalog.Label(Definition.actions[Mathf.Min(encounter.Step,Definition.actions.Length-1)]),smallStyle);
                }
                else Label(1173,210,375,105,"Choose equipment in the room. Decisions are recorded. Evaluation follows all five cases.");
                var care=GetComponent<TreatmentManager>();
                Label(1173,405,375,142,care?care.status:"Equipment system loading...");
                Label(1173,550,375,70,"E: pick up tool. Aim at the correct body area, hold F. Q: return tool.");
                if(Button(1173,610,375,40,"Reassess patient"))Inspect(encounter.SelectedPatient);
                if(session.certification && Button(1173,655,375,46,"Finish this case / hand over")) Complete();
            }
            if(!session.certification)
            {
                Panel(new Rect(28,675,1100,122));
                Label(48,687,1050,100,feedback);
            }
            else Label(1173,720,375,50,feedback,smallStyle);
        }
        void DrawResult()
        {
            Panel(new Rect(415,220,770,490));
            Label(450,251,700,62,session.certification?"CASE RECORDED":"TRAINING DEBRIEF",titleStyle);
            if(!session.certification)
            {
                Label(450,325,230,75,result.score+"%",statStyle);
                Label(695,325,440,115,result.score==100?"Perfect training run saved. This scenario counts toward certification.":"Attempt saved. Retry with no errors and complete every step to earn 100%.");
                Label(450,450,660,70,"Errors: "+result.mistakes+"   |   Actions: "+result.stepsCompleted+"/"+result.stepsRequired+"   |   Time: "+result.seconds.ToString("0.0")+"s");
                if(Button(450,552,320,52,"Retry this training case")) session.StartTraining(scenarioId);
                if(Button(790,552,320,52,"Return to training hub")) session.BackToHub();
            }
            else
            {
                Label(450,345,680,115,result.timedOut?"Time limit reached. Your actions have been saved for the final debrief.":"Your actions have been saved. The next case takes place in a different setting.");
                if(Button(450,532,660,58,session.round==4?"View final analytics":"Continue to next case")) session.Next();
            }
        }
        void DrawDashboard()
        {
            Panel(new Rect(80,112,1440,672));
            Label(114,137,1340,60,(AnalyticsManager.Passed(session.results)?"CERTIFIED":"NOT CERTIFIED")+" / PRACTICE ASSESSMENT",titleStyle);
            int n=session.results.Count;float p=0,d=0,q=0,e=0,total=0;int passed=0;
            foreach(var r in session.results) { p+=r.priority;d+=r.diagnosis;q+=r.quality;e+=r.efficiency;total+=r.seconds;if(r.score==100)passed++; }
            string[] labels={"PATIENT PRIORITIZATION","DIAGNOSIS","TREATMENT QUALITY","TIME EFFICIENCY"};
            float[] values={p,d,q,e};
            for(int i=0;i<4;i++) { Label(114+i*340,221,325,33,labels[i],smallStyle);Label(114+i*340,263,325,62,(n>0?values[i]/n:0).ToString("0")+"%",statStyle); }
            Label(114,350,1310,36,"SCENARIO                                       PRIORITY        DIAGNOSIS       QUALITY       TIME         ERRORS",smallStyle);
            for(int i=0;i<n;i++)
            {
                var r=session.results[i];float y=400+i*42;
                Label(114,y,410,37,ScenarioCatalog.All[r.scenario].title);
                Label(555,y,145,37,r.priority+"%");Label(740,y,145,37,r.diagnosis+"%");Label(915,y,130,37,r.quality+"%");
                Label(1060,y,150,37,r.seconds.ToString("0.0")+"s");Label(1250,y,180,37,r.mistakes+(r.timedOut?" / timeout":""));
            }
            int correct=0,wrong=0,critical=0,worsened=0;float response=0;
            foreach(var r in session.results){correct+=r.correctActions;wrong+=r.incorrectActions;critical+=r.criticalMistakes;if(r.patientWorsened)worsened++;response+=Mathf.Max(0,r.firstActionSeconds);}
            Label(114,617,1270,33,"Correct: "+correct+" / Incorrect: "+wrong+" / Critical mistakes: "+critical+" / Worsened: "+worsened+" / Mean response: "+(response/Mathf.Max(1,n)).ToString("0.0")+"s",smallStyle);
            Label(114,652,1270,30,passed==5?"All five cases completed correctly. Personal practice result only.":"Review missed diagnosis / priority, incorrect tool placement, and incomplete or delayed care in the case rows.",smallStyle);
            if(Button(114,694,330,50,"Return to hub")) session.BackToHub();
            if(Button(465,694,330,50,"Export analytics JSON")) session.Export();
            Label(820,687,650,67,session.exportStatus,smallStyle);
        }
        void DrawPause()
        {
            Panel(new Rect(450,210,700,500));Label(485,242,630,50,"PAUSED",titleStyle);
            if(Button(485,317,630,50,"Resume")) Paused=false;
            if(Button(485,386,630,50,"Return to hub (abandon current case)")) { Paused=false;session.BackToHub(); }
            if(Button(485,455,630,50,resetConfirm?"Confirm reset of practice training scores":"Reset practice training progress"))
            {
                if(resetConfirm) { session.ResetTraining();resetConfirm=false; } else resetConfirm=true;
            }
            Label(485,539,630,125,"The simulation pauses when you press Esc. Sound can be muted in the top-right corner. Hold the right mouse button to look around.");
        }
    }
}
