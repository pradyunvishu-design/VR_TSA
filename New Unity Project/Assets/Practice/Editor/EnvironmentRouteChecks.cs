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
        [MenuItem("PULSE/Environment/Check approach routes in all rooms")]
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
                    ScenarioBayArchitecture.Verify(room, GameObject.Find("Location art").transform);
                    var gear = UnityEngine.Object.FindObjectsByType<TreatmentTool>(FindObjectsSortMode.None);
                    var satellite = gear.Where(tool => tool.transform.position.z < -11).ToArray();
                    var originalKit = gear.Where(tool => tool.transform.position.z >= -11).ToArray();
                    if (satellite.Length == 0 || !satellite.Select(tool => tool.actionId).OrderBy(x=>x)
                        .SequenceEqual(originalKit.Select(tool=>tool.actionId).OrderBy(x=>x)))
                        throw new InvalidOperationException("The original and south carts must stock the same equipment");
                    if (gear.Length == 0 || gear.Any(g => !g.transform.Find("Equipment model") || !g.GetComponentInChildren<Collider>()))
                        throw new InvalidOperationException("Missing tool art or pickup collider in " + scene.name);
                    Physics.SyncTransforms();
                    var entry = new Vector2Int(Mathf.RoundToInt(start.x*2),Mathf.RoundToInt(start.z*2));
                    var reachable = Flood(entry);
                    bool Near(Vector3 point, float distance) => reachable.Any(cell =>
                        Vector2.Distance((Vector2)cell*.5f,new Vector2(point.x,point.z)) < distance);
                    foreach (var patient in room.patients) Check(Near(patient.transform.position,1.25f), patient.name);
                    foreach (var tool in gear)
                        Check(CanAimAt(tool,tool.GetComponentInChildren<Collider>().bounds.center,3f), "pickup ray for " + tool.name);
                    void CheckTreatmentRays(string layout)
                    {
                        foreach (string action in room.Definition.actions.Where(a => a != "call"))
                        {
                            var target = room.patients[0].GetComponentsInChildren<TreatmentTarget>().First(t => t.Accepts(action));
                            Check(CanAimAt(target,target.GetComponent<Collider>().bounds.center,2.8f), layout+" treatment ray for "+action);
                        }
                    }
                    CheckTreatmentRays("authored layout");
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
                    int extensionCells = reachable.Count(cell => cell.y < -22);
                    Check(extensionCells * .25f >= 140f, "140 square metres of reachable new space south of z=-11");
                    Debug.Log("EXPANDED_FLOOR="+scene.name+" / reachable extension "+(extensionCells*.25f)+" square metres");
                    // Exercise both variations of the three original stations and the new area.
                    var originalPositions = room.patients.Select(p => p.transform.position).ToArray();
                    try
                    {
                        for (int variation = 0; variation < 2; variation++)
                        for (int shift = 0; shift < 4; shift++)
                        {
                            foreach (var patient in room.patients)
                                patient.transform.position = room.PatientMark((patient.patientId+shift)%4, variation);
                            Physics.SyncTransforms();
                            reachable = Flood(entry);
                            string layout = "placement "+variation+"/"+shift;
                            foreach (var patient in room.patients)
                            {
                                Vector3 feet = patient.transform.position;
                                var overlaps = Physics.OverlapCapsule(feet+Vector3.up*.52f,feet+Vector3.up*1.5f,.40f,
                                    ~0,QueryTriggerInteraction.Ignore);
                                Check(!overlaps.Any(hit => !hit.transform.IsChildOf(patient.transform)), layout+" standing space for "+patient.name);
                                Check(Near(feet,1.25f) && CanAimAt(patient,feet+Vector3.up*1.15f,2.8f),
                                    layout+" inspection ray for "+patient.name);
                            }
                            CheckTreatmentRays(layout);
                        }
                    }
                    finally
                    {
                        for (int i=0; i<room.patients.Length; i++) room.patients[i].transform.position = originalPositions[i];
                        Physics.SyncTransforms();
                    }
                    Debug.Log("PATIENT_MARKS_PASS="+scene.name+" / eight verified marks per patient");
                    Debug.Log("LOCATION_ROUTES_PASS=" + scene.name + " / " + gear.Length + " tools, three patients, return portal");
                    void Check(bool passed, string target)
                    {
                        if (!passed) throw new InvalidOperationException("No reachable approach to " + target + " in " + scene.name);
                    }
                }
            }
            finally { EditorSceneManager.OpenScene(original); }
        }

        // In Play Mode, move the real capsule along a connected route through the new entrance.
        internal static void WalkAcrossVenue(PracticePlayer player)
        {
            var controller = player.GetComponent<CharacterController>();
            Vector3 spawn = player.transform.position;
            var start = new Vector2Int(Mathf.RoundToInt(spawn.x*2), Mathf.RoundToInt(spawn.z*2));
            var previous = new Dictionary<Vector2Int, Vector2Int>();
            var reachable = Flood(start, previous);
            var target = reachable.Where(cell => cell.y >= -4).OrderBy(cell => (cell - new Vector2Int(-4, -2)).sqrMagnitude).First();
            var path = new Stack<Vector2Int>();
            for (var cell=target; cell!=start; cell=previous[cell]) path.Push(cell);
            try
            {
                foreach (var cell in path)
                {
                    var waypoint = new Vector3(cell.x * .5f, player.transform.position.y, cell.y * .5f);
                    controller.Move(waypoint - player.transform.position + Vector3.down * .04f);
                    if (Vector2.Distance(new Vector2(waypoint.x,waypoint.z),
                        new Vector2(player.transform.position.x,player.transform.position.z)) > .36f)
                        throw new InvalidOperationException("Player capsule blocked at " + waypoint);
                }
                if (player.transform.position.z < -2.5f || spawn.z > -20f)
                    throw new InvalidOperationException("Walk did not connect the extension and original venue");
                Debug.Log("PLAYER_CROSSING_PASS=" + player.gameObject.scene.name + " / " + path.Count + " half-metre steps");
            }
            finally
            {
                controller.enabled = false;
                player.transform.position = spawn;
                controller.enabled = true;
                Physics.SyncTransforms();
            }
        }

        // Half-metre walking grid. Ignore low step surfaces and the actor's own controller.
        static HashSet<Vector2Int> Flood(Vector2Int start, Dictionary<Vector2Int, Vector2Int> previous = null)
        {
            var visited = new HashSet<Vector2Int> { start };
            var reachable = new HashSet<Vector2Int>();
            var pending = new Queue<Vector2Int>();
            visited.Add(start);
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
                    if (Mathf.Abs(next.x)>19 || next.y < -53 || next.y>17 || !visited.Add(next)) continue;
                    if (previous != null) previous[next] = cell;
                    pending.Enqueue(next);
                }
            }
            return reachable;
        }
    }
}
