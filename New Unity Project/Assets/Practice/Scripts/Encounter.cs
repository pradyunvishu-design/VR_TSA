using System;
using System.Collections.Generic;

namespace First180.Practice
{
    [Serializable] public class ActionRecord
    {
        public string action;
        public float seconds;
        public bool correct;
        public ActionRecord(string a,float s,bool c) { action=a;seconds=s;correct=c; }
    }
    [Serializable] public class CaseResult
    {
        public int scenario, mistakes, stepsCompleted, stepsRequired, diagnosis, priority, quality, efficiency, score;
        public int correctActions,incorrectActions,criticalMistakes;
        public float seconds, firstActionSeconds;
        public bool completed, timedOut,patientStabilized,patientWorsened;
        public List<ActionRecord> events = new List<ActionRecord>();
    }

    // Pure C# scoring logic: can be tested without scenes, colliders or UI.
    public class Encounter
    {
        public readonly ScenarioDefinition definition;
        public readonly bool certification;
        public int Step { get; private set; }
        public int Mistakes { get; private set; }
        public int SelectedPatient { get; private set; } = -1;
        public int DiagnosedAs { get; private set; } = -1;
        public bool Finished { get; private set; }
        public readonly List<ActionRecord> events = new List<ActionRecord>();
        public Encounter(int scenario,bool cert,ScenarioDefinition custom=null) { definition=custom??ScenarioCatalog.All[scenario];certification=cert; }
        public void PlacementError(string action,float seconds)
        { if(Finished)return;Mistakes++;events.Add(new ActionRecord("placement:"+action,seconds,false)); }
        public bool ChoosePatient(int patient,float seconds)
        {
            if(Finished || patient<0 || patient>2 || SelectedPatient>=0) return false;
            bool correct=patient==0;
            events.Add(new ActionRecord("prioritize:"+patient,seconds,correct));
            if(!correct) Mistakes++;
            if(correct || certification) SelectedPatient=patient;
            return correct;
        }
        public bool Diagnose(int diagnosis,float seconds)
        {
            if(Finished || SelectedPatient<0 || DiagnosedAs>=0 || diagnosis<0 || diagnosis>=5) return false;
            bool correct=diagnosis==definition.id;
            events.Add(new ActionRecord("diagnose:"+diagnosis,seconds,correct));
            if(!correct) Mistakes++;
            if(correct || certification) DiagnosedAs=diagnosis;
            return correct;
        }
        public bool Perform(string action,float seconds)
        {
            if(Finished || SelectedPatient<0 || DiagnosedAs<0 || Array.IndexOf(ScenarioCatalog.ActionIds,action)<0) return false;
            bool correct=SelectedPatient==0 && Step<definition.actions.Length && definition.actions[Step]==action;
            events.Add(new ActionRecord(action,seconds,correct));
            if(correct) Step++; else Mistakes++;
            return correct;
        }
        public CaseResult Finish(float seconds,bool timeout=false)
        {
            Finished=true;
            int required=definition.actions.Length;
            bool completed=Step==required && SelectedPatient==0 && DiagnosedAs==definition.id && !timeout;
            int treatmentErrors=events.FindAll(e=>!e.correct && !e.action.StartsWith("prioritize:") && !e.action.StartsWith("diagnose:")).Count;
            int quality=(int)Math.Round(100.0*Step/(required+treatmentErrors));
            int efficiency=Step==0 ? 0 : (int)Math.Round(Math.Max(0,100.0*(1-seconds/(definition.timeLimit*1.4))));
            return new CaseResult {
                scenario=definition.id,mistakes=Mistakes,stepsCompleted=Step,stepsRequired=required,
                diagnosis=DiagnosedAs==definition.id?100:0,priority=SelectedPatient==0?100:0,
                quality=quality,efficiency=efficiency,seconds=seconds,firstActionSeconds=events.Count>0?events[0].seconds:-1,
                score=completed && Mistakes==0 ? 100 : Math.Min(99,(int)Math.Round(100.0*(Step+(SelectedPatient==0?1:0)+(DiagnosedAs==definition.id?1:0))/(required+2+Mistakes))),
                completed=completed,timedOut=timeout,events=new List<ActionRecord>(events)
                ,correctActions=events.FindAll(e=>e.correct).Count,incorrectActions=Mistakes,
                criticalMistakes=events.FindAll(e=>!e.correct && (e.action=="water"||e.action=="rub"||e.action=="loosen"||e.action.StartsWith("placement:"))).Count,
                patientStabilized=completed,patientWorsened=!completed
            };
        }
    }
    public static class PracticeRules
    {
        public static bool CanCertify(int[] scores) => scores!=null && scores.Length==5 && Array.TrueForAll(scores,x=>x==100);
        public static int[] ShuffledOrder(Random random)
        {
            int[] order={0,1,2,3,4};
            for(int i=4;i>0;i--) { int j=random.Next(i+1); int value=order[i];order[i]=order[j];order[j]=value; }
            return order;
        }
    }
}
