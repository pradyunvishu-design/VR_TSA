using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace First180.Practice
{
    // The authored lobby stays untouched: connections exist only in the loaded runtime scene.
    public class LobbyConnections : MonoBehaviour
    {
        public IEnumerator Start()
        {
            yield return SceneManager.LoadSceneAsync("FIRST180_Lobby", LoadSceneMode.Additive);
            var lobby=SceneManager.GetSceneByName("FIRST180_Lobby");
            foreach(var root in lobby.GetRootGameObjects())
            {
                foreach(var camera in root.GetComponentsInChildren<Camera>(true)) camera.enabled=false;
                foreach(var listener in root.GetComponentsInChildren<AudioListener>(true)) listener.enabled=false;
                foreach(var t in root.GetComponentsInChildren<Transform>(true))
                {
                    for(int i=0;i<5;i++)
                    {
                        string prefix=(i+1).ToString("00")+" - ";
                        if(!t.name.StartsWith(prefix) || !t.name.Contains(ScenarioCatalog.All[i].title.ToUpper())) continue;
                        var p=t.gameObject.AddComponent<PracticePortal>();p.scenarioId=i;p.label=ScenarioCatalog.All[i].title;
                        foreach(var text in t.GetComponentsInChildren<TextMesh>())if(text.name=="Offline Label")text.text="APPROACH OR SELECT TO ENTER";
                        var entrance=new GameObject("Runtime entrance / "+p.label);
                        entrance.transform.SetParent(t,false);entrance.transform.localPosition=new Vector3(0,1.2f,-0.8f);
                        var trigger=entrance.AddComponent<BoxCollider>();trigger.isTrigger=true;trigger.size=new Vector3(3,2.4f,2);
                        var body=entrance.AddComponent<Rigidbody>();body.isKinematic=true;body.useGravity=false;
                        entrance.AddComponent<PortalEntryTrigger>().portal=p;
                        Debug.Log("Connected lobby entrance: "+t.name);
                    }
                }
            }
        }
    }
}
