using UnityEngine;

namespace First180.Practice
{
    // The learner can check a member of the public without selecting a clinical patient.
    [DisallowMultipleComponent]
    public sealed class CrowdBystander : MonoBehaviour
    {
        public string Observation { get; private set; }

        internal void Configure(string observation, float height)
        {
            Observation = observation;
            var target = gameObject.AddComponent<CapsuleCollider>();
            target.isTrigger = true;
            target.height = height;
            target.radius = Mathf.Clamp(height * .16f, .19f, .29f);
            target.center = Vector3.up * (.5f * height);
        }
    }
}
