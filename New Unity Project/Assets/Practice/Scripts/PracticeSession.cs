using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace First180.Practice
{
    public class PracticeSession : MonoBehaviour
    {
        public static PracticeSession Instance { get; private set; }
        public bool certification, loading, dashboard;
        public bool muted;
        [NonSerialized] public bool testMode;
        public int pendingId=-1, round;
        public int[] order;
        public int[] best = new int[5];
        public readonly List<CaseResult> results = new List<CaseResult>();
        public string exportStatus="";
        public const string PrefPrefix="First180Practice.v1.training.";
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Instance=null; }
        public static PracticeSession Get()
        {
            if(Instance==null) new GameObject("Practice Session (persistent)").AddComponent<PracticeSession>();
            return Instance;
        }
        void Awake()
        {
            if(Instance!=null && Instance!=this) { Destroy(gameObject);return; }
            Instance=this;
            DontDestroyOnLoad(gameObject);
            for(int i=0;i<5;i++) best[i]=Mathf.Clamp(PlayerPrefs.GetInt(PrefPrefix+i,0),0,100);
        }
        public bool CanCertify => PracticeRules.CanCertify(best);
        public void StartTraining(int id)
        {
            if(loading || id<0 || id>=5) return;
            certification=false;dashboard=false;results.Clear();pendingId=id;
            Load(ScenarioCatalog.All[id].scene);
        }
        public bool StartCertification()
        {
            if(!CanCertify || loading) return false;
            certification=true;dashboard=false;results.Clear();round=0;
            order=PracticeRules.ShuffledOrder(new System.Random());pendingId=order[round];
            Load(ScenarioCatalog.All[pendingId].scene);
            return true;
        }
        public void Record(CaseResult result)
        {
            results.Add(result);
            if(!certification)
            {
                best[result.scenario]=Math.Max(best[result.scenario],result.score);
                if(!testMode){PlayerPrefs.SetInt(PrefPrefix+result.scenario,best[result.scenario]);PlayerPrefs.Save();}
            }
            Export();
        }
        public void Next()
        {
            if(loading) return;
            if(certification && ++round<5)
            {
                pendingId=order[round];Load(ScenarioCatalog.All[pendingId].scene);
            }
            else { dashboard=true;Load(ScenarioCatalog.Hub); }
        }
        public void BackToHub()
        {
            certification=false;dashboard=false;pendingId=-1;Load(ScenarioCatalog.Hub);
        }
        public void Load(string scene)
        {
            if(loading) return;
            StartCoroutine(LoadAsync(scene));
        }
        System.Collections.IEnumerator LoadAsync(string scene)
        {
            loading=true;
            yield return null;
            var operation=SceneManager.LoadSceneAsync(scene);
            if(operation!=null) while(!operation.isDone) yield return null;
            loading=false;
        }
        [Serializable] class Report
        {
            public string generatedUtc,mode,scope="Unity learning simulation; not a medical qualification";
            public int[] trainingBest;
            public List<CaseResult> cases;
        }
        public void Export()
        {
            if(testMode)return;
            try
            {
                string path=Path.Combine(Application.persistentDataPath,"First180Practice");
                Directory.CreateDirectory(path);
                var report=new Report {generatedUtc=DateTime.UtcNow.ToString("o"),mode=certification?"Certification":"Training",trainingBest=best,cases=results};
                string file=Path.Combine(path,"latest-"+(certification?"certification":"training")+".json");
                File.WriteAllText(file,JsonUtility.ToJson(report,true));
                exportStatus=file;
            }
            catch(Exception error) { exportStatus="Export failed: "+error.Message;Debug.LogWarning(exportStatus); }
        }
        public void ResetTraining()
        {
            for(int i=0;i<5;i++) { best[i]=0;PlayerPrefs.DeleteKey(PrefPrefix+i); }
            PlayerPrefs.Save();
        }
    }
}
