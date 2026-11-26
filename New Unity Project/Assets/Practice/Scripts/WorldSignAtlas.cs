using UnityEngine;

namespace First180.Practice
{
    [ExecuteAlways]
    public class WorldSignAtlas : MonoBehaviour
    {
        public TextMesh[] signs;
        static readonly int MainTexture = Shader.PropertyToID("_MainTex");
        void OnEnable() { Font.textureRebuilt += UpdateFont; Refresh(); }
        void OnDisable() { Font.textureRebuilt -= UpdateFont; }
        public void Refresh() { UpdateFont(null); }
        void UpdateFont(Font changed)
        {
            if (signs == null) return;
            var properties = new MaterialPropertyBlock();
            foreach (var sign in signs)
            {
                if (!sign || !sign.font || (changed && changed != sign.font)) continue;
                var renderer = sign.GetComponent<MeshRenderer>();
                renderer.GetPropertyBlock(properties);
                properties.SetTexture(MainTexture, sign.font.material.mainTexture);
                renderer.SetPropertyBlock(properties);
            }
        }
    }
}
