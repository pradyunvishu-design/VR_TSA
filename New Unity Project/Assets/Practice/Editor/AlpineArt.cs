using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace First180.Practice.Editor
{
    public static class AlpineArt
    {
        const string Directory = "Assets/Practice/Art/Alpine/";
        const string GroupName = "North Ridge winter refuge";
        static readonly HashSet<string> Replaced = new HashSet<string>
        {
            "Rear wall", "Left wall", "Right wall", "Rear glazing", "Window mullion",
            "Ceiling beam", "Linear pendant", "Practical light", "Shelter deck", "Shelter roof",
            "Shelter post", "Snowy pine crown", "Pine trunk", "Trail sign",
            "Oak tabletop", "Table pedestal", "Table base"
        };

        public static void Apply(PracticeRoom room, Transform art)
        {
            if (room.scenarioId != 4) return;
            var roots = room.gameObject.scene.GetRootGameObjects();
            var shell = roots.Single(g => g.name.StartsWith("Environment / ", StringComparison.Ordinal)).transform;
            foreach (Transform child in shell)
                if (Replaced.Contains(child.name)) child.gameObject.SetActive(false);
            shell.Find("Floor").GetComponent<Renderer>().enabled = false;
            var previous = art.Find(GroupName);
            if (previous) Object.DestroyImmediate(previous.gameObject);
            var group = Child(art, GroupName);
            var matte = Finish("RefugePalette", "Standard", "RefugePalette", true);
            var refuge = MeshAsset("NorthRidgeRefuge");
            // ModelImporter can convert left/right-handed OBJ axes differently by version.
            // The refuge occupies negative X and positive Z in this scene.
            var axes = new Vector3(refuge.bounds.center.x > 0 ? -1 : 1, 1, refuge.bounds.center.z < 0 ? -1 : 1);
            AddMesh(group, "Timber rescue shelter", refuge, matte, axes, true);
            AddMesh(group, "Snow-covered Kenney firs and glacial boulders", MeshAsset("SnowlineForest"), matte, axes, true);
            AddMesh(group, "Snowpack and forked foot trail", MeshAsset("TrailClearing"), matte, axes, false);
            AddMesh(group, "Sawtooth skyline", MeshAsset("SawtoothRidges"), matte, axes, false);
            Collisions(group);
            Nameplate(group);
            Lighting(group, roots);
            var flakes = Finish("Spindrift", "Sprites/Default", "SnowGrain", false);
            Snow(group, flakes, new Vector3(4.65f, 3.7f, 1.5f), new Vector3(12.6f, .3f, 16), 16);
            Snow(group, flakes, new Vector3(-5.3f, 3.6f, -4.5f), new Vector3(6.0f, .3f, 9.1f), 8);
            var shelterWind = group.gameObject.AddComponent<AlpineShelterWind>();
            shelterWind.room = room;
        }

        static Transform Child(Transform parent, string name)
        {
            var child = new GameObject(name).transform;
            child.SetParent(parent, false);
            return child;
        }

        static Mesh MeshAsset(string name)
        {
            return AssetDatabase.LoadAllAssetsAtPath(Directory + name + ".obj").OfType<Mesh>().Single();
        }

        static Material Finish(string name, string shader, string image, bool palette)
        {
            string img = Directory + image + ".png";
            var importer = (TextureImporter)AssetImporter.GetAtPath(img);
            if (palette && (importer.filterMode != FilterMode.Point || importer.mipmapEnabled || importer.textureCompression != TextureImporterCompression.Uncompressed))
            {
                importer.filterMode = FilterMode.Point;
                importer.mipmapEnabled = false;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
            var path = Directory + name + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!mat)
            {
                mat = new Material(Shader.Find(shader));
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(img);
            if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", .11f);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static void AddMesh(Transform parent, string name, Mesh mesh, Material mat, Vector3 axes, bool casts)
        {
            var child = Child(parent, name);
            child.localScale = axes;
            child.gameObject.isStatic = true;
            child.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = child.gameObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = mat;
            renderer.shadowCastingMode = casts ? ShadowCastingMode.On : ShadowCastingMode.Off;
        }

        static void Box(Transform parent, string name, Vector3 p, Vector3 size)
        {
            var obj = Child(parent, name);
            obj.localPosition = p;
            obj.gameObject.isStatic = true;
            obj.gameObject.AddComponent<BoxCollider>().size = size;
        }

        static void Collisions(Transform parent)
        {
            var geo = Child(parent, "Trail boundaries and accessible shelter");
            // The original 20 metre floor remains the walkable ground.
            foreach (int s in new[] {-1, 1})
            {
                Box(geo, "Trail boundary", new Vector3(s*9.9f, 1.5f, 0), new Vector3(.2f, 3, 20));
                Box(geo, "Trail boundary", new Vector3(0, 1.5f, s*9.9f), new Vector3(20, 3, .2f));
            }
            Box(geo, "Shelter deck", new Vector3(-5,.1f,4.02f), new Vector3(5.1f,.2f,5.86f));
            Box(geo, "Rear timber wall", new Vector3(-5,1.52f,6.74f), new Vector3(5.1f,2.65f,.14f));
            foreach (float x in new[] {-7.52f,-2.48f})
                Box(geo, "Side timber wall", new Vector3(x,1.5f,4.11f), new Vector3(.14f,2.65f,5.28f));
            foreach (float x in new[] {-6.92f,-3.08f})
                Box(geo, "Door jamb", new Vector3(x,1.5f,1.48f), new Vector3(1.2f,2.65f,.14f));
            Box(geo, "Refuge bench", new Vector3(-5,.49f,6.03f), new Vector3(4,.62f,.54f));
        }

        static void Nameplate(Transform parent)
        {
            var obj = Child(parent, "Refuge gable sign");
            obj.localPosition = new Vector3(-5,3.04f,1.279f);
            var lettering = obj.gameObject.AddComponent<TextMesh>();
            lettering.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            lettering.text = "NORTH RIDGE";
            lettering.fontSize = 64;
            lettering.anchor = TextAnchor.MiddleCenter;
            lettering.alignment = TextAlignment.Center;
            lettering.color = new Color(.98f,.89f,.72f);
            var renderer = obj.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = lettering.font.material;
            float width = renderer.localBounds.size.x;
            if (width > .001f) obj.localScale = Vector3.one * (2.12f / width);
        }

        static void Lighting(Transform parent, GameObject[] roots)
        {
            var sun = roots.FirstOrDefault(g => g.name == "Daylight")?.GetComponent<Light>();
            if (sun)
            {
                sun.transform.rotation = Quaternion.Euler(36,-31,0);
                sun.color = new Color(1,.94f,.84f);
                sun.intensity = .97f;
                sun.shadowStrength = .56f;
            }
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(.66f,.76f,.80f);
            RenderSettings.ambientEquatorColor = new Color(.54f,.62f,.67f);
            RenderSettings.ambientGroundColor = new Color(.42f,.49f,.54f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = .015f;
            RenderSettings.fogColor = new Color(.73f,.81f,.85f);
            var lamp = Child(parent, "Warmth from the refuge");
            lamp.localPosition = new Vector3(-5,2.02f,4.25f);
            var source = lamp.gameObject.AddComponent<Light>();
            source.type = LightType.Point;
            source.color = new Color(1,.71f,.42f);
            source.range = 6.8f;
            source.intensity = 2.3f;
        }

        static void Snow(Transform parent, Material flakes, Vector3 p, Vector3 size, float rate)
        {
            var obj = Child(parent, "Drifting snow outside shelter");
            obj.localPosition = p;
            var system = obj.gameObject.AddComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = system.main;
            main.loop = true;
            main.prewarm = true;
            main.duration = 5;
            main.maxParticles = Mathf.CeilToInt(rate * 5);
            main.startSize = new ParticleSystem.MinMaxCurve(.012f,.030f);
            main.startLifetime = new ParticleSystem.MinMaxCurve(3.1f,4.5f);
            main.startSpeed = 0;
            main.startColor = new Color(1,1,1,.66f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = system.emission;
            emission.rateOverTime = rate;
            var shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = size;
            var wind = system.velocityOverLifetime;
            wind.enabled = true;
            wind.space = ParticleSystemSimulationSpace.World;
            wind.x = new ParticleSystem.MinMaxCurve(.1f,.29f);
            wind.y = new ParticleSystem.MinMaxCurve(-1.05f,-.78f);
            wind.z = new ParticleSystem.MinMaxCurve(.03f,.11f);
            var renderer = obj.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = flakes;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }
    }
}
