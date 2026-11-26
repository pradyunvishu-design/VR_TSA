#if FIRST180_FRAME_BENCHMARK && UNITY_STANDALONE_WIN && !UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace First180.Practice
{
    public sealed class PracticeFrameBenchmark : MonoBehaviour
    {
        const float WarmupSeconds = 3f;
        const float SampleSeconds = 12f;
        const double BudgetMs = 1000d / 60;
        [Serializable] sealed class Measurement
        {
            public string scene;
            public int width, height, samples;
            public float elapsedSeconds, averageFps, p50ms, p95ms, p99ms, framesWithinBudgetPercent, focusedFramePercent;
            public bool pass60AtP95;
            public bool[] patientSighted = new bool[3];
        }
        [Serializable] sealed class Report
        {
            public string utc, unity, os, processor, gpu, api, quality, monitor, windowMode, exitReason;
            public int desktopWidth, desktopHeight, originalVSync, benchmarkVSync, originalFrameLimit, ramMB, vramMB, errors;
            public double desktopHz;
            public bool testMode, complete;
            public Measurement lobby;
            public List<Measurement> rooms = new List<Measurement>();
        }

        readonly Report report = new Report();
        readonly RaycastHit[] sightHits = new RaycastHit[32];
        const string ReportName = "frame-results.json";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-first180-frame-benchmark") < 0) return;
            var host = new GameObject("FIRST180 standalone diagnostic");
            DontDestroyOnLoad(host);
            host.AddComponent<PracticeFrameBenchmark>();
        }

        IEnumerator Start()
        {
            var session = PracticeSession.Get();
            session.testMode = true;
            Application.logMessageReceived += CountErrors;
            var desktop = Screen.currentResolution;
            report.utc = DateTime.UtcNow.ToString("o");
            report.unity = Application.unityVersion;
            report.os = SystemInfo.operatingSystem;
            report.processor = SystemInfo.processorType;
            report.gpu = SystemInfo.graphicsDeviceName;
            report.api = SystemInfo.graphicsDeviceType.ToString();
            report.quality = QualitySettings.names[QualitySettings.GetQualityLevel()];
            report.monitor = Screen.mainWindowDisplayInfo.name;
            report.desktopWidth = desktop.width;
            report.desktopHeight = desktop.height;
            report.desktopHz = desktop.refreshRateRatio.value;
            report.windowMode = Screen.fullScreenMode.ToString();
            report.ramMB = SystemInfo.systemMemorySize;
            report.vramMB = SystemInfo.graphicsMemorySize;
            report.originalVSync = QualitySettings.vSyncCount;
            report.originalFrameLimit = Application.targetFrameRate;
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = -1;
            Application.runInBackground = true;
            report.benchmarkVSync = QualitySettings.vSyncCount;
            report.testMode = session.testMode;

            double lobbyLimit = Time.realtimeSinceStartupAsDouble + 45;
            while (FindObjectsByType<PracticePortal>().Length < 5 && Time.realtimeSinceStartupAsDouble < lobbyLimit)
                yield return null;
            var lobby = SceneManager.GetSceneByName("FIRST180_Lobby");
            if (!lobby.isLoaded || !Camera.main)
            {
                Finish(false,"Original lobby failed to load additively."); yield break;
            }
            yield return Sample(null, Camera.main, lobby.name);
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-first180-lobby-only") >= 0)
            {
                Finish(true,"Completed original lobby sweep."); yield break;
            }
            foreach (var scenario in ScenarioCatalog.All)
            {
                session.StartTraining(scenario.id);
                double limit = Time.realtimeSinceStartupAsDouble + 60;
                while ((session.loading || SceneManager.GetActiveScene().name != scenario.scene) && Time.realtimeSinceStartupAsDouble < limit)
                    yield return null;
                var room = FindAnyObjectByType<PracticeRoom>();
                var camera = Camera.main;
                if (session.loading || !room || room.scenarioId != scenario.id || !camera || room.patients.Length != 3)
                {
                    Finish(false, "Scene initialization failed: " + scenario.scene); yield break;
                }
                yield return Sample(room,camera,scenario.scene);
            }
            Finish(true, "Completed the five-room traversal.");
        }

        IEnumerator Sample(PracticeRoom room, Camera camera, string scene)
        {
            double settle = Time.realtimeSinceStartupAsDouble + WarmupSeconds;
            while (Time.realtimeSinceStartupAsDouble < settle) yield return null;
            if (room && !room.EmergencyActive) room.BeginEmergency();
            var m = new Measurement {scene=scene, width=Screen.width, height=Screen.height};
            var times = new List<double>(1200);
            int inFocus = 0, onTime = 0;
            var clock = Stopwatch.StartNew();
            double previous = 0;
            while (previous < SampleSeconds)
            {
                // A fixed, unobstructed eye-height route from entry to the treatment clearing.
                float progress = Mathf.Clamp01((float)previous / SampleSeconds);
                float sway = Mathf.Sin(progress * Mathf.PI * 2);
                if (room)
                {
                    camera.transform.position = new Vector3(sway*.72f,1.67f,Mathf.Lerp(-6f,-2.5f,progress));
                    var look = new Vector3(sway*2.9f,1.15f,1.15f);
                    camera.transform.rotation = Quaternion.LookRotation(look - camera.transform.position);
                    if ((times.Count & 15) == 0)
                        foreach (var patient in room.patients)
                            if (!m.patientSighted[patient.patientId]) m.patientSighted[patient.patientId] = HasSight(camera,patient);
                }
                else
                {
                    // Sweep all five portals and adjoining gallery from the authored entry.
                    camera.transform.rotation = Quaternion.Euler(0,360f*progress,0);
                }
                yield return null;
                double now = clock.Elapsed.TotalSeconds;
                double ms = (now - previous) * 1000;
                times.Add(ms);
                if (ms <= BudgetMs) onTime++;
                if (Application.isFocused) inFocus++;
                previous = now;
            }
            times.Sort();
            m.samples = times.Count;
            m.elapsedSeconds = (float)previous;
            m.averageFps = (float)(m.samples / previous);
            m.p50ms = Percentile(times,.50);
            m.p95ms = Percentile(times,.95);
            m.p99ms = Percentile(times,.99);
            m.focusedFramePercent = 100f * inFocus / m.samples;
            m.framesWithinBudgetPercent = 100f * onTime / m.samples;
            m.pass60AtP95 = m.p95ms <= BudgetMs;
            if (room) report.rooms.Add(m);
            else report.lobby = m;
            Save();
        }

        bool HasSight(Camera camera, PracticePatient patient)
        {
            // Center of the standing patient, with room fixtures and other patients as occluders.
            Vector3 target = patient.transform.position + Vector3.up * 1.2f;
            Vector3 viewport = camera.WorldToViewportPoint(target);
            if (viewport.z <= 0 || viewport.x < .02f || viewport.x > .98f || viewport.y < .02f || viewport.y > .98f) return false;
            Vector3 delta = target - camera.transform.position;
            int hits = Physics.RaycastNonAlloc(camera.transform.position, delta.normalized, sightHits, delta.magnitude, ~0, QueryTriggerInteraction.Ignore);
            if (hits == sightHits.Length) return false;
            for (int i=0; i<hits; i++)
            {
                var col = sightHits[i].collider;
                if (col.GetComponentInParent<PracticePlayer>() || col.GetComponentInParent<PracticePatient>() == patient) continue;
                return false;
            }
            return true;
        }

        static float Percentile(List<double> sorted, double fraction) =>
            (float)sorted[Math.Max(0,(int)Math.Ceiling(fraction * sorted.Count) - 1)];

        static string ResultPath => Path.Combine(Path.GetTempPath(),"FIRST180-PracticeBenchmark",ReportName);
        void Save()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ResultPath));
            File.WriteAllText(ResultPath,JsonUtility.ToJson(report,true));
        }

        void CountErrors(string message,string stack,LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) report.errors++;
        }
        void Finish(bool complete, string reason)
        {
            report.complete = complete;
            report.exitReason = reason;
            Save();
            Application.logMessageReceived -= CountErrors;
            UnityEngine.Debug.Log("FIRST180_FRAME_BENCHMARK=" + ResultPath);
            Application.Quit(complete ? 0 : 2);
        }
    }
}
#endif
