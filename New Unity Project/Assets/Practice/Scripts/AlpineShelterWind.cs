using UnityEngine;

namespace First180.Practice
{
    // The existing mix supplies outdoors wind. Muffle only that loop after the listener crosses the threshold.
    public sealed class AlpineShelterWind : MonoBehaviour
    {
        public PracticeRoom room;
        AudioSource wind;
        Transform listener;
        float attenuation = 1f;

        void Start()
        {
            if (!room) return;
            foreach (var source in room.GetComponents<AudioSource>())
                if (source.loop && source.clip && source.clip.name == "Room air") wind = source;
            var camera = Camera.main;
            if (camera) listener = camera.transform;
        }

        void LateUpdate()
        {
            if (!wind || !listener) return;
            var p = transform.InverseTransformPoint(listener.position);
            float depth = Mathf.Min(Mathf.Min(p.x + 7.52f, -2.48f - p.x), Mathf.Min(p.z - 1.48f, 6.74f - p.z));
            float target = Mathf.Lerp(1f,.21f,Mathf.SmoothStep(0f,1f,Mathf.Clamp01((depth+.15f)/.9f)));
            attenuation = Mathf.MoveTowards(attenuation, target, Time.deltaTime*1.6f);
            wind.volume *= attenuation;
        }
    }
}
