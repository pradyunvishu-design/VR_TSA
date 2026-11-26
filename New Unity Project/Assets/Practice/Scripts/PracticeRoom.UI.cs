using System;
using System.Collections.Generic;
using UnityEngine;

namespace First180.Practice
{
    public partial class PracticeRoom
    {
        GUIStyle titleStyle, textStyle, smallStyle, buttonStyle, statStyle;
        readonly List<Rect> drawnPanels = new List<Rect>(8);
        readonly Texture2D[] buttonFills = new Texture2D[3];
        GUIStyle hintStyle, panelCaption;
        Texture2D pulseWordmark;
        static float HudScale => Mathf.Max(.01f, Mathf.Min(Screen.width / 1600f, Screen.height / 900f));
        static float HudLeft => (Screen.width - 1600f * HudScale) * .5f;
        static float HudHeight => Screen.height / HudScale;
        readonly Color ink=new Color(0.035f,0.065f,0.10f,0.97f);
        readonly Color accent=new Color(0.24f,0.88f,0.77f);
        bool resetConfirm, showingReassessment;
        PracticePlayer desktopPlayer;
        void Styles()
        {
            if(titleStyle!=null) return;
            pulseWordmark=Resources.Load<Texture2D>("PulseWordmark");
            titleStyle=new GUIStyle(GUI.skin.label){fontSize=28,fontStyle=FontStyle.Bold,wordWrap=true};titleStyle.normal.textColor=Color.white;
            textStyle=new GUIStyle(GUI.skin.label){fontSize=19,wordWrap=true};textStyle.normal.textColor=new Color(0.86f,0.91f,0.95f);
            smallStyle=new GUIStyle(textStyle){fontSize=17};
            panelCaption=new GUIStyle(smallStyle){fontStyle=FontStyle.Bold,wordWrap=false};
            panelCaption.normal.textColor=accent;
            hintStyle=new GUIStyle(smallStyle){alignment=TextAnchor.MiddleCenter, wordWrap=false};
            statStyle=new GUIStyle(titleStyle){fontSize=42};statStyle.normal.textColor=accent;
            buttonStyle=new GUIStyle(GUI.skin.button){fontSize=19,alignment=TextAnchor.MiddleLeft,wordWrap=true,border=new RectOffset(),padding=new RectOffset(15,12,3,3)};
            buttonFills[0]=ButtonFill(new Color(.105f,.195f,.23f));
            buttonFills[1]=ButtonFill(new Color(.16f,.33f,.37f));
            buttonFills[2]=ButtonFill(new Color(.12f,.41f,.4f));
            SetButtonState(buttonStyle.normal,buttonFills[0]);
            SetButtonState(buttonStyle.hover,buttonFills[1]);
            SetButtonState(buttonStyle.focused,buttonFills[1]);
            SetButtonState(buttonStyle.active,buttonFills[2]);
        }
        static Texture2D ButtonFill(Color color)
        {
            var texture=new Texture2D(1,1){hideFlags=HideFlags.HideAndDontSave};
            texture.SetPixel(0,0,color);
            texture.Apply(false,true);
            return texture;
        }
        static void SetButtonState(GUIStyleState state,Texture2D background)
        {
            state.background=background;
#if UNITY_EDITOR
            state.scaledBackgrounds=Array.Empty<Texture2D>();
#endif
            state.textColor=Color.white;
        }
        void OnDestroy()
        {
            foreach(var fill in buttonFills) if(fill) Destroy(fill);
        }
        static void Fill(Rect rect,Color color)
        {
            var old=GUI.color;
            GUI.color=color;
            GUI.DrawTexture(rect,Texture2D.whiteTexture);
            GUI.color=old;
        }
        void Panel(Rect rect)
        {
            if (Event.current.type==EventType.Repaint) drawnPanels.Add(rect);
            Fill(rect,ink);
        }
        void Label(float x,float y,float w,float h,string text,GUIStyle style=null) { GUI.Label(new Rect(x,y,w,h),text,style??textStyle); }
        bool Button(float x,float y,float w,float h,string text) => GUI.Button(new Rect(x,y,w,h),text,buttonStyle);
        void OnGUI()
        {
            if(session==null || VRInteractionManager.Active) return;
            Styles();
            if(Event.current.type==EventType.Repaint) drawnPanels.Clear();
            var previous=GUI.matrix;
            try
            {
                GUI.matrix=Matrix4x4.TRS(new Vector3(HudLeft,0,0),Quaternion.identity,new Vector3(HudScale,HudScale,1));
                DrawDesktop();
            }
            finally { GUI.matrix=previous; }
        }
        void DrawDesktop()
        {
            float width=Screen.width/HudScale;
            float left=(1600-width)*.5f;
            Panel(new Rect(left,0,width,74));
            GUI.DrawTexture(new Rect(28,12,180,50),pulseWordmark,ScaleMode.ScaleToFit);
            string mode=session.dashboard?"CERTIFICATION RESULTS":scenarioId<0?"TRAINING HUB":
                session.certification?"CERTIFICATION   /   CASE "+(session.round+1)+" OF 5":"TRAINING   /   "+Definition.title.ToUpperInvariant();
            Label(254,21,980,35,mode);
            if(Button(1370,15,200,44,session.muted?"Sound off":"Sound on")) session.muted=!session.muted;

            bool modal=session.loading || Paused || session.dashboard || result!=null;
            float footer=HudHeight-60;
            Panel(new Rect(left,footer,width,60));
            string look=desktopPlayer && desktopPlayer.MouseLookActive?"Mouse   Look    Tab   Menu cursor":"Click floor or Tab   Mouse look";
            string controls=modal?"Personal practice simulation. Times and scores are for this exercise.":
                look+"     Middle-drag   Sidestep     WASD   Move     Left / Right   Turn     "+
                (scenarioId<0?"Walk up or E / click   Enter":"E   Inspect / pick up     F   Use     Q   Return tool")+"     Esc   Pause";
            Label(28,footer+15,1540,32,controls,smallStyle);
            if(session.loading)
            {
                Panel(new Rect(left,74,width,footer-74));
                Label(580,HudHeight*.48f,520,70,scenarioId<0?"Opening the gallery…":"Loading location…",titleStyle);
                return;
            }
            if(Paused) { DrawPause();return; }
            resetConfirm=false;
            if(session.dashboard) { DrawDashboard();return; }
            if(scenarioId<0) DrawHub(); else DrawCase();
            if(!modal) DrawInteractionHint();
        }
        void DrawInteractionHint()
        {
            float y=HudHeight*.5f;
            Fill(new Rect(796,y-4,8,8),new Color(.012f,.035f,.045f,.85f));
            Fill(new Rect(798,y-2,4,4),Color.white);
            if (!desktopPlayer) desktopPlayer=FindAnyObjectByType<PracticePlayer>();
            if(!desktopPlayer || string.IsNullOrEmpty(desktopPlayer.targetLabel)) return;
            string hint=desktopPlayer.targetLabel;
            float w=Mathf.Min(560,hintStyle.CalcSize(new GUIContent(hint)).x+38);
            var rect=new Rect(800-w*.5f,y+24,w,38);
            Panel(rect);
            GUI.Label(rect,hint,hintStyle);
        }
        void DrawHub()
        {
            Panel(new Rect(1150,103,430,690));
            Label(1173,125,375,45,"Training scenarios",titleStyle);
            Label(1173,174,375,65,"Choose a case here or walk up to its exhibit in the gallery.");
            for(int i=0;i<5;i++)
            {
                float y=250+i*65;
                if(Button(1173,y,383,54,ScenarioCatalog.All[i].title+"   ·   "+session.best[i]+"%")) session.StartTraining(i);
                if (session.best[i]>0) Fill(new Rect(1173,y+51,383*Mathf.Clamp01(session.best[i]/100f),3),accent);
            }
            GUI.enabled=session.CanCertify;
            if(Button(1173,597,383,56,session.CanCertify?"Start certification":"Certification locked")) session.StartCertification();
            GUI.enabled=true;
            Label(1173,671,378,100,session.CanCertify?"Unlocked. Five cases, random order. No robot or correction hints.":"Earn 100% in all five training cases to unlock certification.");
            Panel(new Rect(28,103,315,76));
            int mastered = Array.FindAll(session.best, mark => mark == 100).Length;
            Label(48,111,280,31,"Training record",textStyle);
            Label(48,145,275,26,mastered+" of 5 scenarios mastered",smallStyle);
        }
        void DrawCase()
        {
            Panel(new Rect(28,103,430,82));
            Label(48,114,389,34,Definition.location,titleStyle);
            Label(48,152,389,27,EmergencyActive?"INCIDENT ACTIVE" : "OBSERVATION",smallStyle);
            if(result!=null) { DrawResult();return; }
            Panel(new Rect(1150,103,430,EmergencyActive?690:218));
            Fill(new Rect(1150,103,430,3),accent);
            if(!EmergencyActive)
            {
                Label(1173,120,375,31,"LOCATION BRIEF",panelCaption);
                Label(1173,168,375,130,LocationBrief);
                return;
            }

            Label(1173,118,380,30,PatientPanelIdentity,panelCaption);
            string phase=encounter.SelectedPatient<0?"Patient assessment":
                encounter.DiagnosedAs<0?"Identify the condition":"Patient care";
            Label(1173,151,385,47,phase,titleStyle);
            string readout=session.certification
                ?"Elapsed "+elapsed.ToString("0")+" s   /   Limit "+Definition.timeLimit+" s"
                :"Training   /   "+encounter.Mistakes+(encounter.Mistakes==1?" error":" errors");
            Label(1173,199,375,34,readout,smallStyle);
            Fill(new Rect(1173,242,382,1),new Color(.2f,.35f,.39f));

            if(encounter.SelectedPatient<0)
            {
                for(int slot=0;slot<3;slot++)
                {
                    int patient=PatientInSlot(slot);
                    float y=259+slot*49;
                    GUI.enabled=CanObserve(patients[patient].transform);
                    if(Button(1173,y,382,42,"Inspect patient "+patientLabels[patient])) Inspect(patient);
                    GUI.enabled=true;
                    if(inspected==patient) Fill(new Rect(1173,y,4,42),accent);
                }
                Label(1173,419,375,30,"FINDINGS",panelCaption);
                Label(1173,457,380,126,SearchFindings);
                GUI.enabled=inspected>=0;
                if(Button(1173,604,382,54,inspected<0?"Choose a patient first":"Prioritize patient "+patientLabels[inspected])) ChoosePatient(inspected);
                GUI.enabled=true;
            }
            else if(encounter.DiagnosedAs<0)
            {
                Label(1173,257,375,30,"FINDINGS",panelCaption);
                Label(1173,296,380,115,observation);
                for(int i=0;i<5;i++) if(Button(1173,424+i*51,382,45,ScenarioCatalog.All[i].title)) Diagnose(i);
            }
            else
            {
                string heading=showingReassessment?"REASSESSMENT":session.certification?"CASE RECORD":TrainingStepLabel;
                Label(1173,257,375,30,heading,panelCaption);
                string detail=showingReassessment?observation:session.certification?"Your actions are being recorded.":CurrentTrainingInstruction;
                Label(1173,301,380,130,detail);
                Fill(new Rect(1173,448,382,1),new Color(.2f,.35f,.39f));
                Label(1173,463,375,30,"EQUIPMENT",panelCaption);
                var care=GetComponent<TreatmentManager>();
                Label(1173,501,380,95,care?care.status:"Loading equipment...");
                if(Button(1173,610,382,40,showingReassessment?"Back to task":"Reassess patient"))
                {
                    showingReassessment=!showingReassessment;
                    if(showingReassessment) Inspect(encounter.SelectedPatient);
                }
                if(session.certification && Button(1173,662,382,46,"Finish this case / hand over")) Complete();
            }
            if(session.certification) Label(1173,724,375,48,feedback,smallStyle);
        }

        // Both displays identify the same person after the clinical roles are shuffled.
        internal string PatientPanelIdentity
        {
            get
            {
                int id=encounter.SelectedPatient>=0?encounter.SelectedPatient:inspected;
                if(id<0) return "SCENE ASSESSMENT";
                return "PATIENT "+patientLabels[id]+(encounter.SelectedPatient>=0?"  /  SELECTED":"  /  INSPECTED");
            }
        }
        internal string TrainingStepLabel => "TRAINING  /  STEP "+(Mathf.Min(encounter.Step,Definition.instructions.Length-1)+1)+" OF "+Definition.instructions.Length;
        internal string CurrentTrainingInstruction => Definition.instructions[Mathf.Min(encounter.Step,Definition.instructions.Length-1)];

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
            bool passed=AnalyticsManager.Passed(session.results);
            Fill(new Rect(91,134,5,56),passed?accent:new Color(1f,.7f,.39f));
            Label(114,137,1340,60,(passed?"CERTIFIED":"NOT CERTIFIED")+" / PRACTICE ASSESSMENT",titleStyle);
            int n=session.results.Count;float p=0,d=0,q=0,e=0;
            foreach(var r in session.results) { p+=r.priority;d+=r.diagnosis;q+=r.quality;e+=r.efficiency; }
            string[] labels={"PATIENT PRIORITIZATION","DIAGNOSIS","TREATMENT QUALITY","TIME EFFICIENCY"};
            float[] values={p,d,q,e};
            for(int i=0;i<4;i++)
            {
                float x=114+i*340, score=n>0?values[i]/n:0;
                Label(x,217,325,32,labels[i],smallStyle);
                Label(x,249,325,62,score.ToString("0")+"%",statStyle);
                Fill(new Rect(x,316,286,4),new Color(.18f,.26f,.28f));
                Fill(new Rect(x,316,286*Mathf.Clamp01(score/100),4),accent);
            }
            float[] columnX = {114,555,740,915,1060,1250};
            string[] headings = {"SCENARIO","PRIORITY","DIAGNOSIS","QUALITY","TIME","ERRORS"};
            for(int i=0;i<headings.Length;i++) Label(columnX[i],350,175,36,headings[i],smallStyle);
            for(int i=0;i<n;i++)
            {
                var r=session.results[i];float y=400+i*42;
                if(i%2==0) Fill(new Rect(103,y-2,1388,39),new Color(.10f,.15f,.185f));
                Label(114,y,410,37,ScenarioCatalog.All[r.scenario].title);
                Label(555,y,145,37,r.priority+"%");Label(740,y,145,37,r.diagnosis+"%");Label(915,y,130,37,r.quality+"%");
                Label(1060,y,150,37,r.seconds.ToString("0.0")+"s");Label(1250,y,180,37,r.mistakes+(r.timedOut?" / timeout":""));
            }
            int correct=0,wrong=0,critical=0,worsened=0,responded=0;
            float response=0;
            foreach(var r in session.results)
            {
                correct+=r.correctActions;wrong+=r.incorrectActions;critical+=r.criticalMistakes;
                if(r.patientWorsened) worsened++;
                if(r.firstActionSeconds>=0) { response+=r.firstActionSeconds; responded++; }
            }
            string average=responded>0?(response/responded).ToString("0.0")+" s":"—";
            Label(114,615,1365,34,"Correct "+correct+"    /    Incorrect "+wrong+"    /    Critical mistakes "+critical+"    /    Worsened "+worsened+
                "    /    Mean first response "+average+" ("+responded+"/"+n+" cases)",smallStyle);
            Label(114,652,1365,33,passed?"You completed all five cases and met the practice certification criteria.":
                "Review the priority, diagnosis and quality columns. Each error or timeout identifies a case to practice again.",smallStyle);
            if(Button(114,694,330,50,"Return to hub")) session.BackToHub();
            if(Button(465,694,330,50,"Export analytics JSON")) session.Export();
            Label(820,687,650,67,session.exportStatus,smallStyle);
        }
        void DrawPause()
        {
            Panel(new Rect(450,210,700,500));Label(485,242,630,50,"PAUSED",titleStyle);
            if(Button(485,317,630,50,"Resume")) TogglePause();
            if(Button(485,386,630,50,"Return to hub (abandon current case)")) { TogglePause();session.BackToHub(); }
            if(Button(485,455,630,50,resetConfirm?"Confirm reset of practice training scores":"Reset practice training progress"))
            {
                if(resetConfirm) { session.ResetTraining();resetConfirm=false; } else resetConfirm=true;
            }
            Label(485,539,630,125,resetConfirm?"Confirm to erase all five saved training scores. Returning to the simulation keeps them.":"Esc resumes the simulation. Returning to the hub ends this attempt.");
        }
    }
}
