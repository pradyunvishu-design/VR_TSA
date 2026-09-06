using UnityEngine;
namespace First180.Practice
{
    public class SpatialButton : MonoBehaviour
    {
        public System.Action action;
        public void Invoke(){action?.Invoke();}
    }
}
