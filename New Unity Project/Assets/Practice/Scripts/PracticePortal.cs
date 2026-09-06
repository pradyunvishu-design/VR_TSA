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
            if(Time.time<readyAt || session.loading) return;
            if(returnToHub) session.BackToHub();
            else if(certification) session.StartCertification();
            else session.StartTraining(scenarioId);
        }
        void OnTriggerEnter(Collider other)
        {
            if(other.GetComponent<PracticePlayer>()!=null) Use();
        }
    }
}
