using UnityEngine;
namespace First180.Practice
{
    public class PortalEntryTrigger : MonoBehaviour
    {
        public PracticePortal portal;
        void OnTriggerEnter(Collider other){if(other.GetComponentInParent<PracticePlayer>())portal.Use();}
    }
}
