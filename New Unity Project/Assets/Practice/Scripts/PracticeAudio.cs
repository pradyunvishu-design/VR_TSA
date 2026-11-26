using UnityEngine;

namespace First180.Practice
{
    public class PracticeAudio : MonoBehaviour
    {
        AudioSource heart, ambience, effects, breathing;
        AudioClip pulse, warning, ping, breath;
        AudioClip incidentSound;
        float nextBeat;
        AudioClip generatedRoomAir;
        static readonly string[] LocationLoops = { "Cafe", "Commons", "Road", "Workshop" };
        public Transform robot;
        void Awake()
        {
            heart=gameObject.AddComponent<AudioSource>();
            ambience=gameObject.AddComponent<AudioSource>();
            effects=gameObject.AddComponent<AudioSource>();
            breathing=gameObject.AddComponent<AudioSource>();breath=MakeBreath();breathing.clip=breath;breathing.loop=true;breathing.volume=0;breathing.Play();
            pulse=MakeTone("Heartbeat",0.36f,60,true);
            warning=MakeTone("Incident",0.8f,190,false);
            ping=MakeTone("Robot acknowledgment",0.15f,640,false);
            var room = GetComponent<PracticeRoom>();
            if (room && room.scenarioId >= 0 && room.scenarioId < IncidentNames.Length)
                incidentSound = MakeIncidentSound(room.scenarioId);
            if (room && room.scenarioId >= 0 && room.scenarioId < LocationLoops.Length)
                ambience.clip = Resources.Load<AudioClip>("LocationSound/" + LocationLoops[room.scenarioId]);
            if (!ambience.clip) ambience.clip = generatedRoomAir = MakeAmbience();
            ambience.loop=true;ambience.volume=0.08f;ambience.Play();
            if (robot) robot.gameObject.SetActive(false);
        }
        void Update()
        {
            var room=GetComponent<PracticeRoom>();
            var session=PracticeSession.Get();
            AudioListener.volume=session.muted?0:0.65f;
            if(room && room.EmergencyActive && !room.Paused && room.result==null)
            {
                var state=room.patients[0].GetComponent<PatientManager>();
                float bpm=state?state.heartRate:90;
                if(Time.time>=nextBeat) { heart.PlayOneShot(pulse,state && state.stability<35?.65f:.35f);nextBeat=Time.time+60f/Mathf.Max(35,bpm); }
                breathing.volume=room.scenarioId==1 && state && !state.treated?.015f:.12f;
                breathing.pitch=state && state.treated?.8f:1.25f;
            }
            else breathing.volume=0;
            ambience.volume=room && room.Paused?0:room && room.scenarioId==4?.28f:.07f;
        }
        public void SignalIncident(Transform patient)
        {
            // The first sound belongs to the scene. Both ears receive a cue from
            // the person's actual location, including when the player faces away.
            var source = new GameObject("Incident sound").AddComponent<AudioSource>();
            source.transform.SetParent(patient, false);
            source.transform.localPosition = Vector3.up * .85f;
            source.playOnAwake = false;
            source.spatialBlend = 1f;
            source.dopplerLevel = 0f;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.minDistance = 3f;
            source.maxDistance = 42f;
            source.clip = incidentSound;
            source.volume = .85f;
            source.Play();
        }

        static readonly string[] IncidentNames = {
            "Cup striking tile", "Chair scraping the dining floor", "Cart latch rattling",
            "Spindle winding down", "Hiker's double whistle"
        };

        // Original mono foley. Spatialization happens on the patient in the scene.
        static AudioClip MakeIncidentSound(int scenario)
        {
            const int sampleRate = 22050;
            float[] samples = new float[(int)(sampleRate * 1.35f)];
            var noise = new System.Random(173 + scenario);
            float filtered = 0f, phase = 0f;
            for (int i = 0; i < samples.Length; i++)
            {
                float t = i / (float)sampleRate;
                float grit = (float)(noise.NextDouble() * 2 - 1);
                filtered = .86f * filtered + .14f * grit;
                switch (scenario)
                {
                    case 0:
                        float ring = .45f * Mathf.Sin(2f * Mathf.PI * 845f * t)
                            + .22f * Mathf.Sin(2f * Mathf.PI * 2256f * t);
                        samples[i] = (Impact(t, 15f) + .48f * Impact(t - .21f, 19f)) * (ring + .19f * grit);
                        break;
                    case 1:
                        float scrape = Burst(t, .04f, .53f) + .65f * Burst(t, .63f, .89f);
                        phase += 2f * Mathf.PI * (305f + 44f * Mathf.Sin(31f * t)) / sampleRate;
                        samples[i] = scrape * (.26f * Mathf.Sin(phase) + .08f * Mathf.Sin(3f * phase) + .64f * filtered);
                        break;
                    case 2:
                        float latch = Impact(t, 14f) + .7f * Impact(t - .17f, 17f) + .45f * Impact(t - .29f, 23f);
                        samples[i] = latch * (.43f * Mathf.Sin(2f * Mathf.PI * 185f * t) + .21f * Mathf.Sin(2f * Mathf.PI * 634f * t) + .27f * grit);
                        break;
                    case 3:
                        phase += 2f * Mathf.PI * Mathf.Lerp(410f, 64f, t) / sampleRate;
                        samples[i] = Burst(t, 0f, 1.18f) * (.38f * Mathf.Sin(phase) + .13f * Mathf.Sin(phase * 2f) + .38f * filtered);
                        break;
                    default:
                        float whistle = Burst(t, .02f, .38f) + .86f * Burst(t, .56f, 1.16f);
                        phase += 2f * Mathf.PI * (1160f + 160f * whistle + 15f * Mathf.Sin(t * 30f)) / sampleRate;
                        samples[i] = whistle * (.46f * Mathf.Sin(phase) + .16f * filtered);
                        break;
                }
            }
            var clip = AudioClip.Create(IncidentNames[scenario], samples.Length, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
        static float Impact(float t, float decay) => t < 0f ? 0f : Mathf.Exp(-t * decay);
        static float Burst(float t, float start, float end) =>
            t <= start || t >= end ? 0f : Mathf.Sin(Mathf.PI * (t - start) / (end - start));
        public void Feedback(bool correct){effects.PlayOneShot(correct?ping:warning,correct?.2f:.12f);}
        static AudioClip MakeBreath()
        {
            const int rate=22050;float[] samples=new float[rate*3];var random=new System.Random(22);float last=0;
            for(int i=0;i<samples.Length;i++){float t=(float)i/rate;last=last*.75f+(float)(random.NextDouble()*2-1)*.25f;samples[i]=last*Mathf.Max(0,Mathf.Sin(t/3*Mathf.PI*2));}
            var clip=AudioClip.Create("Procedural breathing",samples.Length,1,rate,false);clip.SetData(samples,0);return clip;
        }
        static AudioClip MakeTone(string name,float duration,float frequency,bool heartbeat)
        {
            int rate=22050;float[] samples=new float[(int)(duration*rate)];
            for(int i=0;i<samples.Length;i++)
            {
                float t=(float)i/rate;
                float env=heartbeat ? Mathf.Exp(-t*40)+0.65f*Mathf.Exp(-Mathf.Abs(t-0.15f)*50) : Mathf.Sin(Mathf.PI*t/duration)*0.3f;
                samples[i]=Mathf.Sin(2*Mathf.PI*frequency*t)*env*0.65f;
            }
            var clip=AudioClip.Create(name,samples.Length,1,rate,false);clip.SetData(samples,0);return clip;
        }
        static AudioClip MakeAmbience()
        {
            int rate=22050;float[] samples=new float[rate*4];var random=new System.Random(91);
            float last=0;
            for(int i=0;i<samples.Length;i++) { last=last*0.97f+(float)(random.NextDouble()*2-1)*0.03f;samples[i]=last; }
            var clip=AudioClip.Create("Room air",samples.Length,1,rate,false);clip.SetData(samples,0);return clip;
        }
        void OnDestroy()
        {
            if(incidentSound) Destroy(incidentSound);
            if(pulse) Destroy(pulse);if(warning) Destroy(warning);if(ping) Destroy(ping);if(breath)Destroy(breath);
            if(generatedRoomAir) Destroy(generatedRoomAir);
        }
    }
}
