using UnityEngine;

namespace First180.Practice
{
    public class SpatialButton : MonoBehaviour
    {
        public System.Action action;
        public bool Available { get; private set; } = true;
        int hoveringHands;
        MaterialPropertyBlock properties;
        Renderer face;
        static readonly int Tint = Shader.PropertyToID("_Color");

        public void SetAvailable(bool available)
        {
            if (Available == available) return;
            Available = available;
            Paint();
        }

        public void SetHover(int hand, bool inside)
        {
            int mask = 1 << hand;
            int next = inside ? hoveringHands | mask : hoveringHands & ~mask;
            if (next == hoveringHands) return;
            hoveringHands = next;
            Paint();
        }

        void Paint()
        {
            if (!face) face = GetComponent<Renderer>();
            properties ??= new MaterialPropertyBlock();
            face.GetPropertyBlock(properties);
            Color color = !Available ? new Color(.10f,.12f,.14f)
                : hoveringHands != 0 ? new Color(.09f,.43f,.40f) : face.sharedMaterial.color;
            properties.SetColor(Tint,color);
            face.SetPropertyBlock(properties);
        }

        public void Invoke()
        {
            if (Available && isActiveAndEnabled) action?.Invoke();
        }
    }
}
