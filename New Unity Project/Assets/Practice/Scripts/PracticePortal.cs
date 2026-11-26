using UnityEngine;

namespace First180.Practice
{
    public class PracticePortal : MonoBehaviour
    {
        public const float SelectionRange = 12f;
        public int scenarioId;
        public string label;
        public bool certification, returnToHub;
        public void Use()
        {
            var session=PracticeSession.Get();
            var room=FindFirstObjectByType<PracticeRoom>();
            if(session.loading || session.dashboard || (room && (room.Paused || room.result!=null))) return;
            if(returnToHub) session.BackToHub();
            else if(certification) session.StartCertification();
            else session.StartTraining(scenarioId);
        }
        internal bool CanSelect(float distance) => distance <= SelectionRange;

        void OnTriggerEnter(Collider other)
        {
            if(other.GetComponentInParent<PracticePlayer>()!=null) Use();
        }
    }
}
