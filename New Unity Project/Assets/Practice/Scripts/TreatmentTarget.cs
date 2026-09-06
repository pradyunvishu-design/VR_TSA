using UnityEngine;
namespace First180.Practice
{
    public class TreatmentTarget : MonoBehaviour
    {
        public string[] actions;
        public int patientId;
        public string anatomicalSite;
        public bool Accepts(string action)=>System.Array.IndexOf(actions,action)>=0;
    }
}
