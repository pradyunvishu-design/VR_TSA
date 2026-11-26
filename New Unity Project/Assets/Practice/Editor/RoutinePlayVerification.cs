using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace First180.Practice.Editor
{
    // Run in an isolated batch-mode Editor. Exercise the same scene loading, clocks,
    // visual discovery, patient selection and physical tool handlers used by the player.
    [InitializeOnLoad]
    public static class RoutinePlayVerification
    {
        const string State = "Pulse.RoutinePlayVerification";
        static IEnumerator checks;
        static double deadline;
        static int lastFrame = -1;
        static string runtimeError;

        static RoutinePlayVerification() { EditorApplication.update += Tick; }

        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/Practice/Scenes/TrainingHub.unity");
            SessionState.SetBool(State, true);
            EditorApplication.isPlaying = true;
        }

        static string Argument(string name, string fallback)
        {
            string[] args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, name);
            return index >= 0 && index + 1 < args.Length ? args[index + 1] : fallback;
        }

        static void Tick()
        {
            if (!SessionState.GetBool(State, false) || !EditorApplication.isPlaying || EditorApplication.isCompiling) return;
            try
            {
                if (checks == null)
                {
                    Application.runInBackground = true;
                    Application.logMessageReceived += RecordRuntimeError;
                    checks = Exercise();
                    deadline = EditorApplication.timeSinceStartup + 540;
                }
                if (runtimeError != null) throw new InvalidOperationException(runtimeError);
                if (EditorApplication.timeSinceStartup > deadline) throw new TimeoutException("Timed out in live routine checks");
                if (lastFrame == Time.frameCount) return;
                lastFrame = Time.frameCount;
                if (checks.MoveNext()) return;
                Debug.Log("PULSE_ROUTINES_PASS");
                SessionState.SetBool(State, false);
                EditorApplication.Exit(0);
            }
            catch (Exception error)
            {
                Debug.LogException(error);
                SessionState.SetBool(State, false);
                EditorApplication.Exit(2);
            }
        }

        static void RecordRuntimeError(string message, string trace, LogType type)
        {
            if ((type == LogType.Exception || type == LogType.Error || type == LogType.Assert) &&
                trace.Contains("First180.Practice") && runtimeError == null) runtimeError = message;
        }

        static void Check(bool ok, string label)
        {
            if (!ok) throw new InvalidOperationException(label);
            Debug.Log("ROUTINE PASS " + label);
        }

        static IEnumerator Exercise()
        {
            string choice = Argument("-pulseRoutineCase", "all");
            int[] cases = choice == "all" ? new[] { 0, 1, 2, 3, 4 } : new[] { int.Parse(choice) };
            string output = Argument("-pulseEvidenceDir", "");
            var session = PracticeSession.Get();
            session.testMode = true;
            Time.timeScale = 2f;
            var hub = Object.FindFirstObjectByType<LobbyConnections>();
            while (!hub || !hub.Ready || session.loading) yield return null;
            int seed = int.Parse(Argument("-pulseSeed", "1847"));
            UnityEngine.Random.InitState(seed);
            Debug.Log("PULSE_ROUTINE_SEED=" + seed);
            foreach (int id in cases)
            {
                string firstPerson = null;
                string firstMesh = null;
                for (int visit = 0; visit < (id == 0 ? 3 : 2); visit++)
                {
                    bool training = visit == 0;
                    session.muted = visit == 2;
                    if (training) session.StartTraining(id);
                    else
                    {
                        session.certification = true;
                        session.Load(ScenarioCatalog.All[id].scene);
                    }
                    yield return null;
                    while (session.loading) yield return null;
                    yield return null;
                    var room = Object.FindFirstObjectByType<PracticeRoom>();
                    var routine = room ? room.GetComponent<ScenarioRoutine>() : null;
                    Check(room && room.scenarioId == id && routine, "Loaded routine " + id + " visit " + visit);
                    var lead = room.patients[0];
                    var peer = room.patients[id == 1 || id == 2 ? 1 : 2];
                    var eye = Camera.main;
                    var cast = lead.GetComponent<CasePatientAnimation>();
                    string person = cast.ActiveAppearance;
                    Check(!string.IsNullOrEmpty(person) && (visit != 1 || firstPerson != person), "Eligible adult " + person);
                    var body = lead.GetComponentInChildren<Animator>().GetComponentsInChildren<SkinnedMeshRenderer>()
                        .First(s => s.enabled && s.sharedMesh);
                    string bodyPath = AssetDatabase.GetAssetPath(body.sharedMesh);
                    Check(!string.IsNullOrEmpty(bodyPath) && (visit != 1 || firstMesh != bodyPath), "Visible adult uses " + bodyPath);
                    if (training) { firstPerson = person; firstMesh = bodyPath; }
                    Vector3 eyeStart = eye.transform.position;
                    Vector3 away = -Vector3.forward;
                    eye.transform.rotation = Quaternion.LookRotation(away);
                    room.calmTime = 0;
                    bool photographed = false;
                    Transform trolley = id == 2 ? room.transform.Find("Loading lane parcel trolley") : null;
                    Vector3 startingTrolley = trolley ? trolley.position : Vector3.zero;
                    float trolleyTravel = 0;
                    bool picturedTrolley = false;
                    Vector3? collectedAt = null;
                    Vector3 previousFeet = lead.transform.position;
                    float start = Time.realtimeSinceStartup;
                    while (!routine.ReadyForIncident && !room.EmergencyActive)
                    {
                        CheckDeadline(start, "routine approach " + id);
                        CheckStandingSpace(id, lead.transform);
                        CheckStandingSpace(id, peer.transform);
                        if (trolley) trolleyTravel = Mathf.Max(trolleyTravel, Vector3.Distance(trolley.position, startingTrolley));
                        if (training && !picturedTrolley && trolleyTravel > 1.35f)
                        {
                            Capture(output, "2-trolley", eye, trolley, 3.8f, trolley.forward);
                            picturedTrolley = true;
                        }
                        if (routine.HasCollectedProp && !collectedAt.HasValue) collectedAt = lead.transform.position;
                        if (training && !photographed && collectedAt.HasValue &&
                            Vector3.Distance(collectedAt.Value, lead.transform.position) > 1.25f)
                        {
                            Capture(output, id + "-routine", eye, lead.transform, 3.8f, lead.transform.position - previousFeet);
                            photographed = true;
                        }
                        previousFeet = lead.transform.position;
                        yield return null;
                    }
                    Check(routine.WitnessCount == 2 && routine.LeadDistanceTravelled > .6f && routine.HasCollectedProp &&
                        routine.PeerHasCollectedProp && routine.PeerDistanceTravelled > .6f,
                        "Both eligible adults walked and collected props with two witnesses in " + id);
                    if (id == 2) Check(trolley && trolley.Find("Parcel for loading") && trolleyTravel > .6f,
                        "Dispatcher wheeled a loaded trolley along the service lane");
                    Check(Vector3.Distance(lead.transform.position, routine.TreatmentMark) < .04f,
                        "Reached assigned treatment mark " + routine.TreatmentMark + " from " + lead.transform.position);
                    if (training || session.muted)
                    {
                        for (int f = 0; f < 8; f++) yield return null;
                        Check(!room.EmergencyActive && room.elapsed == 0, "Unseen incident waits without charging clock");
                        room.TogglePause();
                        Vector3 still = lead.transform.position;
                        yield return null;
                        Check(room.Paused && lead.transform.position == still && !room.EmergencyActive, "Pause holds scene");
                        room.TogglePause();
                        Vector3 subject = lead.transform.position + Vector3.up * 1.28f;
                        eye.transform.position = subject + Vector3.back * 1.7f;
                        eye.transform.LookAt(subject);
                    }
                    start = Time.realtimeSinceStartup;
                    while (!room.EmergencyActive) { CheckDeadline(start, "natural onset " + id); yield return null; }
                    Check(room.elapsed < 1f, "Recognition clock starts at perceptible onset");
                    var cue = lead.transform.Find("Incident sound")?.GetComponent<AudioSource>();
                    Check(cue && cue.clip && cue.spatialBlend == 1f, "Local incident cue " + (cue ? cue.clip.name : ""));
                    float[] firstBurst = new float[cue.clip.frequency / 3];
                    Check(cue.clip.GetData(firstBurst, 0) && firstBurst.Any(v => Mathf.Abs(v) > .08f) &&
                        Vector3.Distance(eye.transform.position, cue.transform.position) < cue.maxDistance,
                        "Incident has a measurable signal within the listener's range");
                    yield return null;
                    var animation = new SerializedObject(cast);
                    SkinnedMeshRenderer Skin(string field) => animation.FindProperty(field).objectReferenceValue as SkinnedMeshRenderer;
                    if (id == 3)
                    {
                        var injury = Skin("injured");
                        Check(injury && injury.enabled && !Skin("intact").enabled && injury.sharedMesh &&
                            (person != "PracticeWoman" || AssetDatabase.GetAssetPath(injury.sharedMesh).Contains("WomanWorkshopInjury")),
                            "Workshop injury belongs to the active patient");
                    }
                    if (id == 4)
                    {
                        var cold = Skin("coldFingers");
                        Check(cold && cold.enabled && cold.sharedMesh && !Skin("handDressing").enabled &&
                            (person != "PracticeWorkshopMan" || AssetDatabase.GetAssetPath(cold.sharedMesh).Contains("ManTrailHand")),
                            "Visible hand symptoms match the active hiker");
                    }
                    if (visit == 1)
                    {
                        float poseTime = Time.time + 1.05f;
                        while (Time.time < poseTime) yield return null;
                        Capture(output, id + "-incident", eye, lead.transform, 3.2f);
                    }
                    eye.transform.position = eyeStart;
                    if (visit == 2) continue;
                    room.Inspect(0);
                    room.ChoosePatient(0);
                    room.Diagnose(id);
                    var care = room.GetComponent<TreatmentManager>();
                    foreach (var action in room.Definition.actions)
                    {
                        var tool = Object.FindObjectsByType<TreatmentTool>(FindObjectsSortMode.None).First(t => t.actionId == action);
                        var target = Object.FindObjectsByType<TreatmentTarget>(FindObjectsSortMode.None).FirstOrDefault(t => t.patientId == 0 && t.Accepts(action));
                        Check(target || action == "call", "Reachable clinical handler " + action);
                        care.Grab(tool, eye.transform);
                        care.Use(target, true, tool.holdSeconds + .1f);
                        care.Use(target, false, 0);
                        care.Release();
                        float next = Time.unscaledTime + .36f;
                        while (Time.unscaledTime < next) yield return null;
                        if (action == "protect" && id == 4)
                            Check(Skin("handDressing").enabled && !Skin("coldFingers").enabled, "Protection replaced cold fingers with the fitted dressing");
                    }
                    Check(room.result != null && room.result.score == 100 && lead.GetComponent<PatientManager>().stabilized, "Treated and stabilized " + id);
                }
            }
        }

        static void CheckDeadline(float start, string stage)
        {
            if (Time.realtimeSinceStartup - start > 35f) throw new TimeoutException(stage);
        }

        static void CheckStandingSpace(int id, Transform actor)
        {
            Vector3 feet = actor.position;
            var obstacle = Physics.OverlapCapsule(feet + Vector3.up * .51f, feet + Vector3.up * 1.45f,
                .22f, ~0, QueryTriggerInteraction.Ignore).FirstOrDefault(collider => !collider.transform.IsChildOf(actor));
            if (obstacle)
            {
                var other = obstacle.GetComponentInParent<PracticePatient>();
                throw new InvalidOperationException("Routine " + id + ", " + actor.name + " crossed " + obstacle.name +
                    " on " + (other ? other.name : obstacle.transform.root.name) + " at " + feet);
            }
        }

        static Vector3 FramePosition(Vector3 subject, Transform lead, float distance, Vector3 front)
        {
            var people = Object.FindObjectsByType<SkinnedMeshRenderer>(FindObjectsSortMode.None);
            foreach (float angle in new[] { 18f, -30f, 52f, -58f, 85f, -88f, 140f, 180f })
            {
                Vector3 offset = Quaternion.Euler(0, angle, 0) * front * distance + Vector3.up * .45f;
                Vector3 candidate = subject + offset;
                var ray = new Ray(candidate, -offset.normalized);
                bool blocked = people.Any(skin => skin.enabled && !skin.transform.IsChildOf(lead) &&
                    skin.bounds.IntersectRay(ray, out float at) && at < offset.magnitude);
                if (blocked || Physics.CheckSphere(candidate, .45f, ~0, QueryTriggerInteraction.Ignore)) continue;
                if (Physics.Raycast(candidate, ray.direction, out var hit, offset.magnitude, ~0, QueryTriggerInteraction.Ignore)
                    && !hit.transform.IsChildOf(lead)) continue;
                return candidate;
            }
            return subject + Vector3.back * distance + Vector3.up * .45f;
        }

        static void Capture(string directory, string name, Camera eye, Transform lead, float distance, Vector3 travel = default)
        {
            if (string.IsNullOrEmpty(directory)) return;
            Directory.CreateDirectory(directory);
            var subject = lead.position + Vector3.up * 1.05f;
            var go = new GameObject("Documentation camera");
            var camera = go.AddComponent<Camera>();
            var texture = RenderTexture.GetTemporary(960, 540, 24);
            var previous = RenderTexture.active;
            var frame = new Texture2D(960, 540, TextureFormat.RGB24, false);
            try
            {
                camera.CopyFrom(eye);
                camera.enabled = false;
                camera.stereoTargetEye = StereoTargetEyeMask.None;
                camera.fieldOfView = 54f;
                camera.aspect = 16f / 9;
                Vector3 front = travel.sqrMagnitude > .00001f ? travel.normalized : Vector3.back;
                Vector3 position = FramePosition(subject, lead, distance, front);
                camera.transform.SetPositionAndRotation(position, Quaternion.LookRotation(subject - position));
                camera.targetTexture = texture;
                camera.Render();
                RenderTexture.active = texture;
                frame.ReadPixels(new Rect(0, 0, 960, 540), 0, 0);
                frame.Apply();
                File.WriteAllBytes(Path.Combine(directory, name + ".jpg"), frame.EncodeToJPG(89));
            }
            finally
            {
                RenderTexture.active = previous;
                camera.targetTexture = null;
                RenderTexture.ReleaseTemporary(texture);
                Object.DestroyImmediate(frame);
                Object.DestroyImmediate(go);
            }
        }
    }
}
