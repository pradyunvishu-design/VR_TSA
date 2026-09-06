using System.Collections.Generic;
namespace First180.Practice
{
    public static class AnalyticsManager
    {
        public static bool Passed(List<CaseResult> results)=>results.Count==5 && results.TrueForAll(r=>r.completed && r.priority==100 && r.diagnosis==100 && r.quality>=80 && r.mistakes==0 && !r.timedOut);
        public static string Summary(List<CaseResult> results)
        {
            float p=0,q=0,e=0,time=0;int mistakes=0,stable=0;
            foreach(var r in results){p+=r.priority;q+=r.quality;e+=r.efficiency;time+=r.seconds;mistakes+=r.mistakes;if(r.completed)stable++;}
            int n=System.Math.Max(1,results.Count);
            return (Passed(results)?"CERTIFIED":"NOT CERTIFIED")+" / PRACTICE ONLY\nPrioritization: "+(p/n).ToString("0")+"%\nTreatment quality: "+(q/n).ToString("0")+"%\nEfficiency: "+(e/n).ToString("0")+"%\nStabilized: "+stable+" / 5   Errors: "+mistakes+"\nActive treatment time: "+time.ToString("0.0")+" s\n"+(Passed(results)?"All five cases completed correctly.":"Improve: "+(p<500?"patient priority; ":"")+(q<500?"treatment selection / placement; ":"")+(stable<5?"complete care before deterioration; ":"")+"review case errors.");
        }
    }
}
