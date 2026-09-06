using UnityEngine;
namespace First180.Practice
{
    public class TreatmentTool : MonoBehaviour
    {
        public string actionId;
        [Tooltip("Game interaction duration only; never a real device instruction.")]public float holdSeconds=2;
        public bool Held {get;private set;}
        Vector3 home;Quaternion rotation;Transform homeParent;
        Collider[] colliders;
        void Awake(){home=transform.position;rotation=transform.rotation;homeParent=transform.parent;colliders=GetComponentsInChildren<Collider>();}
        public void Grab(Transform hand)
        {
            Held=true;transform.SetParent(hand,true);transform.localPosition=hand.GetComponent<Camera>()?new Vector3(.35f,-.22f,.7f):Vector3.forward*.13f;transform.localRotation=Quaternion.identity;
            foreach(var c in colliders)c.enabled=false;
        }
        public void Release()
        {
            Held=false;transform.SetParent(homeParent,true);transform.SetPositionAndRotation(home,rotation);foreach(var c in colliders)c.enabled=true;
        }
    }
}
