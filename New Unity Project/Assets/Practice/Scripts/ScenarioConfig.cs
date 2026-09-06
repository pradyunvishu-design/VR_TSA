using UnityEngine;
namespace First180.Practice
{
    [CreateAssetMenu(menuName="FIRST180/Scenario configuration")]
    public class ScenarioConfig : ScriptableObject
    {
        [Range(0,4)] public int scenarioId;
        [Tooltip("Game seconds, not a clinical survival prediction.")] public float certificationSeconds=180;
        public Vector2 onsetSeconds=new Vector2(8,18);
        public Vector2 deteriorationPerSecond=new Vector2(.22f,.38f);
        public bool allowEarlyResponseVariant=true;
        public string clinicalReview="Prototype only. Adult responsive patients. Instructor review required.";
    }
}
