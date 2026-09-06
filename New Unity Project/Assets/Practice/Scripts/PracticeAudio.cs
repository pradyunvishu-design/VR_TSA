using UnityEngine;

namespace First180.Practice
{
    public class PracticeAudio : MonoBehaviour
    {
        AudioSource heart, speaker, ambience, effects, breathing;
        AudioClip pulse, warning, ping, breath;
        float nextBeat;
        public Transform robot;
        Vector3 robotBase;
        void Awake()
        {
            heart=gameObject.AddComponent<AudioSource>();
            speaker=gameObject.AddComponent<AudioSource>();
            ambience=gameObject.AddComponent<AudioSource>();
            effects=gameObject.AddComponent<AudioSource>();
            breathing=gameObject.AddComponent<AudioSource>();breath=MakeBreath();breathing.clip=breath;breathing.loop=true;breathing.volume=0;breathing.Play();
            pulse=MakeTone("Heartbeat",0.36f,60,true);
            warning=MakeTone("Incident",0.8f,190,false);
            ping=MakeTone("Robot acknowledgment",0.15f,640,false);
            ambience.clip=MakeAmbience();ambience.loop=true;ambience.volume=0.08f;ambience.Play();
            if(robot) robotBase=robot.position;
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
            if(robot)
            {
                if(Camera.main)
                {
                    var view=Camera.main.transform;var f=Vector3.ProjectOnPlane(view.forward,Vector3.up).normalized;
                    var goal=view.position+f*1.9f+Vector3.Cross(Vector3.up,f)*.8f;
                    goal.y=view.position.y-.15f+Mathf.Sin(Time.time*2)*.06f;
                    robot.position=Vector3.Lerp(robot.position,goal,Time.deltaTime*1.3f);
                    robot.rotation=Quaternion.LookRotation(Vector3.ProjectOnPlane(robot.position-view.position,Vector3.up));
                }
                robot.gameObject.SetActive(!session.certification);
            }
        }
        public void Speak(string key)
        {
            if(PracticeSession.Get().certification) return;
            AudioClip clip=Resources.Load<AudioClip>("PracticeVoice/"+key);
            speaker.Stop();
            if(clip) { speaker.clip=clip;speaker.Play(); } else speaker.PlayOneShot(ping,0.2f);
        }
        public void Alarm() { effects.PlayOneShot(warning,0.24f); }
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
            if(pulse) Destroy(pulse);if(warning) Destroy(warning);if(ping) Destroy(ping);if(breath)Destroy(breath);
            if(ambience && ambience.clip) Destroy(ambience.clip);
        }
    }
}
