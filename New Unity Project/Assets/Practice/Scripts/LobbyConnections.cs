using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace First180.Practice
{
    // Connect the authored exhibits to the practice session after the additive load.
    public class LobbyConnections : MonoBehaviour
    {
        public bool Ready { get; private set; }
        void Awake() { PracticeSession.Get().loadingHub=this; }
        public IEnumerator Start()
        {
            const string lobbyName = "FIRST180_Lobby";
            if (!Application.CanStreamedLevelBeLoaded(lobbyName))
            {
                Debug.LogError("FIRST180_Lobby is unavailable. The training menu can still open scenarios.");
                Ready = true;
                yield break;
            }
            yield return SceneManager.LoadSceneAsync(lobbyName, LoadSceneMode.Additive);
            var lobby=SceneManager.GetSceneByName(lobbyName);
            var signs=new List<TextMesh>();
            foreach(var root in lobby.GetRootGameObjects())
            {
                signs.AddRange(root.GetComponentsInChildren<TextMesh>(true));
                foreach(var camera in root.GetComponentsInChildren<Camera>(true)) camera.enabled=false;
                foreach(var listener in root.GetComponentsInChildren<AudioListener>(true)) listener.enabled=false;
                foreach(var t in root.GetComponentsInChildren<Transform>(true))
                {
                    FitGallerySurface(t);
                    for(int i=0;i<ScenarioCatalog.All.Length;i++)
                    {
                        string prefix=(i+1).ToString("00")+" - ";
                        if(!t.name.StartsWith(prefix) || !t.name.Contains(ScenarioCatalog.All[i].title.ToUpperInvariant())) continue;
                        var p=t.gameObject.AddComponent<PracticePortal>();p.scenarioId=i;p.label=ScenarioCatalog.All[i].title;
                        foreach(var text in t.GetComponentsInChildren<TextMesh>())if(text.name=="Offline Label")text.text="WALK UP OR SELECT";
                        AddLocationView(t, ScenarioCatalog.All[i]);
                        var entrance=new GameObject("Runtime entrance / "+p.label);
                        entrance.transform.SetParent(t,false);entrance.transform.localPosition=new Vector3(0,1.2f,-0.8f);
                        var trigger=entrance.AddComponent<BoxCollider>();trigger.isTrigger=true;trigger.size=new Vector3(3,2.4f,2);
                        var body=entrance.AddComponent<Rigidbody>();body.isKinematic=true;body.useGravity=false;
                        entrance.AddComponent<PortalEntryTrigger>().portal=p;
                        Debug.Log("Connected lobby entrance: "+t.name);
                    }
                }
            }
            BindGalleryLettering(signs);
            Ready=true;
        }
        void BindGalleryLettering(List<TextMesh> signs)
        {
            // The built-in TextMesh shader draws signs through opaque world-space menus.
            var material=Resources.Load<Material>("PracticeGalleryLettering");
            var fallbackFont=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            foreach(var sign in signs)
            {
                if (!sign.font) sign.font=fallbackFont;
                sign.GetComponent<MeshRenderer>().sharedMaterial=material;
            }
            var atlas=gameObject.AddComponent<WorldSignAtlas>();
            atlas.signs=signs.ToArray();
            atlas.Refresh();
        }
        static void FitGallerySurface(Transform surface)
        {
            if (surface.name!="Gallery Floor" && surface.name!="Inset Floor" && surface.name!="Tree Well" && surface.name!="Soil Bed") return;
            if (!surface.TryGetComponent<CapsuleCollider>(out var capsule) || !surface.TryGetComponent<MeshFilter>(out var mesh)) return;
            // Wide, shallow cylinder primitives expand their capsule collider into a dome.
            capsule.enabled=false;
            var floor=surface.gameObject.AddComponent<MeshCollider>();
            floor.sharedMesh=mesh.sharedMesh;
        }
        static void AddLocationView(Transform exhibit, ScenarioDefinition destination)
        {
            var image = Resources.Load<Material>("LocationViews/" + destination.scene);
            if (!image) return;
            var originalSymbol = exhibit.Find("Scenario Symbol");
            if (originalSymbol) originalSymbol.gameObject.SetActive(false);
            var plate = GameObject.CreatePrimitive(PrimitiveType.Quad);
            plate.name = destination.location + " window";
            plate.transform.SetParent(exhibit,false);
            plate.transform.localPosition = new Vector3(0,3.55f,-.268f);
            plate.transform.localScale = new Vector3(2.12f,1.06f,1);
            Destroy(plate.GetComponent<Collider>());
            plate.GetComponent<Renderer>().sharedMaterial = image;
            var label = exhibit.Find("Training Label");
            if (label && label.TryGetComponent<TextMesh>(out var text)) text.text = destination.location.ToUpperInvariant();
        }
    }
}
