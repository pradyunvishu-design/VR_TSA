using UnityEngine;

namespace First180.Practice
{
    // References the import assets themselves: built players can combine static scenery meshes.
    public sealed class RoutineProps : ScriptableObject
    {
        [System.Serializable]
        public struct Item { public Mesh mesh; public GameObject model; public Material finish; public float length; public Vector3 tilt; }
        [SerializeField] Item[] items;
        public GameObject girl, boy;
        public Material cast, enamel, linen, metal, charcoal, lettering;

        public Transform Create(int index, Transform parent, string label, Vector3 center)
        {
            var item = items[index];
            var root = new GameObject(label).transform;
            root.SetParent(parent, false);
            root.position = center;
            Transform surface;
            if (item.mesh)
            {
                surface = new GameObject(item.mesh.name).transform;
                surface.SetParent(root, false);
                surface.gameObject.AddComponent<MeshFilter>().sharedMesh = item.mesh;
                surface.gameObject.AddComponent<MeshRenderer>().sharedMaterial = item.finish;
            }
            else
            {
                surface = Instantiate(item.model, root, false).transform;
            }
            surface.localPosition = Vector3.zero;
            surface.localRotation = Quaternion.Euler(item.tilt);
            var renderers = surface.GetComponentsInChildren<MeshRenderer>();
            Bounds bounds = renderers[0].bounds;
            foreach (var renderer in renderers)
            {
                bounds.Encapsulate(renderer.bounds);
                var materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++) materials[i] = item.finish;
                renderer.sharedMaterials = materials;
            }
            float largest = Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));
            float scale = item.length / Mathf.Max(largest, .0001f);
            surface.localScale *= scale;
            surface.position += (root.position - bounds.center) * scale;
            return root;
        }
    }
}
