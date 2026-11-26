using UnityEngine;

namespace First180.Practice
{
    public class PracticePortal : MonoBehaviour
    {
        public int scenarioId;
        public string label;
        public bool certification, returnToHub;
        float readyAt;
        void Start() { readyAt=Time.time+1.5f; }
        public void Use()
        {
            var session=PracticeSession.Get();
            var room=FindFirstObjectByType<PracticeRoom>();
            if(session.loading || session.dashboard || (room && (room.Paused || room.result!=null))) return;
            if(returnToHub) session.BackToHub();
            else if(certification) session.StartCertification();
            else session.StartTraining(scenarioId);
        }
        public void Enter()
        {
            if(Time.time>=readyAt) Use();
        }
        void OnTriggerEnter(Collider other)
        {
            if(other.GetComponentInParent<PracticePlayer>()!=null) Enter();
        }
    }
}
