using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace First180.Practice.Editor
{
    public static class EnvironmentRouteChecks
    {
        [MenuItem("FIRST180 Practice/Environment/Check approach routes in all rooms")]
        public static void Run()
        {
            var current = SceneManager.GetActiveScene();
            if (EditorApplication.isPlaying || current.isDirty || SceneManager.sceneCount != 1 || string.IsNullOrEmpty(current.path))
                throw new InvalidOperationException("Run route checks with one saved scene open in Edit Mode.");
            string original = current.path;
            try
            {
                foreach (var scenario in ScenarioCatalog.All)
                {
                    var scene = EditorSceneManager.OpenScene("Assets/Practice/Scenes/" + scenario.scene + ".unity");
                    var room = UnityEngine.Object.FindFirstObjectByType<PracticeRoom>();
                    var start = UnityEngine.Object.FindFirstObjectByType<PracticePlayer>().transform.position;
                    if (!scene.GetRootGameObjects().Any(o => o.name == "Location art"))
                        throw new InvalidOperationException("Missing room art in " + scene.name);
                    var gear = UnityEngine.Object.FindObjectsByType<TreatmentTool>(FindObjectsSortMode.None);
                    if (gear.Length == 0 || gear.Any(g => !g.transform.Find("Equipment model") || !g.GetComponentInChildren<Collider>()))
                        throw new InvalidOperationException("Missing tool art or pickup collider in " + scene.name);
                    Physics.SyncTransforms();
                    var reachable = Flood(new Vector2Int(Mathf.RoundToInt(start.x*2),Mathf.RoundToInt(start.z*2)));
                    bool Near(Vector3 point, float distance) => reachable.Any(cell =>
                        Vector2.Distance((Vector2)cell*.5f,new Vector2(point.x,point.z)) < distance);
                    foreach (var patient in room.patients) Check(Near(patient.transform.position,1.25f), patient.name);
                    foreach (var tool in gear)
                        Check(CanAimAt(tool,tool.GetComponentInChildren<Collider>().bounds.center,3f), "pickup ray for " + tool.name);
                    foreach (string action in room.Definition.actions.Where(a => a != "call"))
                    {
                        var target = room.patients[0].GetComponentsInChildren<TreatmentTarget>().First(t => t.Accepts(action));
                        Check(CanAimAt(target,target.GetComponent<Collider>().bounds.center,2.8f), "treatment ray for " + action);
                    }
                    bool CanAimAt<T>(T expected, Vector3 aim, float range) where T : Component
                    {
                        foreach (var cell in reachable)
                        {
                            var eye = new Vector3(cell.x*.5f,1.75f,cell.y*.5f);
                            var delta = aim-eye;
                            if(delta.sqrMagnitude > range*range || Vector3.Angle(delta,Vector3.down)<20) continue;
                            if(Physics.Raycast(eye,delta.normalized,out RaycastHit hit,range,~0,QueryTriggerInteraction.Collide)
                                && hit.collider.GetComponentInParent<T>()==expected) return true;
                        }
                        return false;
                    }
                    var portal = UnityEngine.Object.FindObjectsByType<PracticePortal>(FindObjectsSortMode.None).Single(p => p.returnToHub);
                    Check(Near(portal.transform.position,1.35f),"return portal");
                    if(room.scenarioId == 4) Check(Near(new Vector3(-5,.2f,3),.8f),"shelter handoff");
                    Debug.Log("LOCATION_ROUTES_PASS=" + scene.name + " / " + gear.Length + " tools, three patients, return portal");
                    void Check(bool passed, string target)
                    {
                        if (!passed) throw new InvalidOperationException("No reachable approach to " + target + " in " + scene.name);
                    }
                }
            }
            finally { EditorSceneManager.OpenScene(original); }
        }

        // Half-metre walking grid. Ignore low step surfaces and the actor's own controller.
        static HashSet<Vector2Int> Flood(Vector2Int start)
        {
            var visited = new HashSet<Vector2Int> { start };
            var reachable = new HashSet<Vector2Int>();
            var pending = new Queue<Vector2Int>();
            pending.Enqueue(start);
            var directions = new[] {Vector2Int.up,Vector2Int.down,Vector2Int.left,Vector2Int.right};
            while(pending.Count != 0)
            {
                var cell = pending.Dequeue();
                Vector3 foot = new Vector3(cell.x*.5f,0,cell.y*.5f);
                var hits = Physics.OverlapCapsule(foot + Vector3.up*.43f,foot+Vector3.up*1.50f,.25f,~0,QueryTriggerInteraction.Ignore);
                if (hits.Any(h => !h.GetComponentInParent<PracticePlayer>() && h.bounds.max.y>.26f)) continue;
                if (!Physics.Raycast(foot+Vector3.up*.36f,Vector3.down,.70f,~0,QueryTriggerInteraction.Ignore)) continue;
                reachable.Add(cell);
                foreach (var direction in directions)
                {
                    var next = cell + direction;
                    if (Mathf.Abs(next.x)>19 || Mathf.Abs(next.y)>17 || !visited.Add(next)) continue;
                    pending.Enqueue(next);
                }
            }
            return reachable;
        }
    }
}
