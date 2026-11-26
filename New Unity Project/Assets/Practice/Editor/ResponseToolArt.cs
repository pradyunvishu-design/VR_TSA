using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace First180.Practice.Editor
{
    public static class ResponseToolArt
    {
        const string Directory = "Assets/Practice/Art/Shared/Equipment";
        const string VisualName = "Equipment model";
        static readonly string[] Swatches = {"263C44","437B80","DDDED2","CF9752","B5C4C1","6C8591","96CBC2","824D43"};
        enum Coat { Graphite, Teal, Linen, Amber, Steel, Blue, Mint, Rust }

        public static void Apply(PracticeRoom room)
        {
            if (!room || room.scenarioId < 0) return;
            if (!AssetDatabase.IsValidFolder(Directory)) AssetDatabase.CreateFolder("Assets/Practice/Art/Shared","Equipment");
            var tools = room.gameObject.scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<TreatmentTool>(true));
            foreach (var tool in tools)
            {
                if (Array.IndexOf(ScenarioCatalog.ActionIds, tool.actionId) < 0) continue;
                var existing = tool.transform.Find(VisualName);
                if (existing) Object.DestroyImmediate(existing.gameObject);
                var shape = new Shape();
                shape.Build(tool.actionId);
                var go = new GameObject(VisualName, typeof(MeshFilter), typeof(MeshRenderer));
                go.transform.SetParent(tool.transform, false);
                // The tray surface sits 8.5 cm below the established grab pivot in all rooms.
                go.transform.localPosition = new Vector3(0,-.083f,0);
                shape.Assign(go,tool.actionId);
                var body=tool.transform.Find("Tool body");
                if (body && body.TryGetComponent<Renderer>(out var render)) render.enabled=false;
            }
        }

        sealed class Shape
        {
            readonly SortedDictionary<Coat,List<CombineInstance>> layers = new SortedDictionary<Coat,List<CombineInstance>>();
            static readonly Dictionary<PrimitiveType,Mesh> primitives = new Dictionary<PrimitiveType,Mesh>();

            static Mesh Primitive(PrimitiveType type)
            {
                if (primitives.TryGetValue(type,out var mesh) && mesh) return mesh;
                var temporary = GameObject.CreatePrimitive(type);
                mesh = temporary.GetComponent<MeshFilter>().sharedMesh;
                Object.DestroyImmediate(temporary);
                primitives[type] = mesh;
                return mesh;
            }

            // Designers' dimensions are centimetres; diameter-height-depth describe the full part.
            void Part(PrimitiveType type, Coat coat, float x,float y,float z,float w,float h,float d,float yaw=0,float roll=0,float pitch=0)
            {
                if (!layers.TryGetValue(coat,out var parts)) layers.Add(coat,parts=new List<CombineInstance>());
                var position=new Vector3(x,y,z)*.01f;
                var scale=new Vector3(w,h,d)*.01f;
                if(type==PrimitiveType.Cylinder || type==PrimitiveType.Capsule) scale.y*=.5f;
                parts.Add(new CombineInstance{mesh=Primitive(type),transform=Matrix4x4.TRS(position,Quaternion.Euler(pitch,yaw,roll),scale)});
            }
            void Box(Coat c,float x,float y,float z,float w,float h,float d,float yaw=0) =>
                Part(PrimitiveType.Cube,c,x,y,z,w,h,d,yaw);
            void Round(Coat c,float x,float y,float z,float w,float h,float d,float yaw=0) =>
                Part(PrimitiveType.Sphere,c,x,y,z,w,h,d,yaw);
            void Disc(Coat c,float x,float y,float z,float w,float h,float d) =>
                Part(PrimitiveType.Cylinder,c,x,y,z,w,h,d);
            void Sleeve(Coat c,float x,float y,float z,float w,float h,float d,float yaw=0) =>
                Part(PrimitiveType.Capsule,c,x,y,z,w,d,h,yaw,0,90);
            void FoldedPack(Coat body,Coat band,float w,float h,float d)
            {
                Round(body,0,h*.46f,0,w,h*.92f,d);
                Box(body,0,h*.5f,0,w*.89f,h*.86f,d*.79f);
                Box(band,0,h*.95f,0,w*.24f,.32f,d*.82f);
                Box(band,0,h*.48f,-d*.405f,w*.24f,h*.91f,.32f);
                Box(band,0,h*.48f,d*.405f,w*.24f,h*.91f,.32f);
            }
            void Glove(Coat glove,Coat cuff,float offset,float bottom,bool folded)
            {
                float step=folded ? .72f : 1;
                Round(glove,offset,bottom+1.8f,-.6f,9,3.6f,10);
                Box(cuff,offset,bottom+1.4f,-5.95f,7.8f,2.7f,3.6f);
                float[] lengths={4.65f,5.95f,5.4f,4.4f};
                for(int i=0;i<4;i++)
                    Sleeve(glove,offset-3.1f+i*1.96f,bottom+1.5f,3.5f+lengths[i]*step*.38f,1.83f,2.1f,lengths[i]*step,5-i*3);
                Sleeve(glove,offset-4.2f,bottom+1.5f,-.35f,2.6f,2.25f,6.15f,-36);
            }
            void Dressing(bool compression)
            {
                FoldedPack(Coat.Linen,compression?Coat.Rust:Coat.Blue,17,5.4f,11.2f);
                for(int i=0;i<3;i++) Box(Coat.Steel,5.7f,1.4f+i*1.25f,-4.49f,3.5f,.18f,.25f);
                if(compression)
                {
                    // A second compressed roll sits in its paper sleeve along the long edge.
                    Part(PrimitiveType.Cylinder,Coat.Linen,0,2.8f,6.1f,5.6f,16,5.6f,0,90);
                    Part(PrimitiveType.Cylinder,Coat.Rust,0,2.8f,6.1f,5.75f,3.8f,5.75f,0,90);
                }
            }

            public void Build(string action)
            {
                switch(action)
                {
                    case "safety":
                        Glove(Coat.Mint,Coat.Teal,1.6f,0,true);
                        Glove(Coat.Teal,Coat.Mint,-1.8f,2.6f,true); break;
                    case "backblows":
                        Glove(Coat.Blue,Coat.Amber,0,0,false);
                        Round(Coat.Mint,0,3.55f,-.3f,7.8f,1.1f,6.9f); break;
                    case "thrusts":
                        Glove(Coat.Rust,Coat.Graphite,0,0,true);
                        for(int i=0;i<3;i++) Round(Coat.Amber,-2.4f+i*2.4f,3.32f,2.3f,2.1f,1.4f,3.4f);
                        break;
                    case "rub": Glove(Coat.Linen,Coat.Rust,0,0,false); break;
                    case "call":
                        Box(Coat.Graphite,0,1.05f,0,11.5f,2.1f,20);
                        Box(Coat.Steel,0,2.15f,0,10.3f,.25f,18.4f);
                        Box(Coat.Graphite,0,2.35f,0,9.8f,.25f,17.9f);
                        Box(Coat.Teal,0,2.55f,.8f,8.35f,.16f,12.6f);
                        for(int i=0;i<3;i++) Box(Coat.Mint,-2.7f+i*1.15f,2.7f,5.1f+i*.27f,.78f,.12f,1.2f+i*.54f);
                        Box(Coat.Steel,0,2.65f,8.15f,2.75f,.18f,.39f);
                        Disc(Coat.Mint,0,2.65f,-6.95f,1.7f,.18f,1.7f); break;
                    case "epinephrine":
                        Disc(Coat.Amber,0,2.1f,0,4.1f,4.2f,4.1f);
                        Disc(Coat.Linen,0,12.8f,0,3.76f,17.3f,3.76f);
                        Disc(Coat.Teal,0,22.4f,0,4.0f,2.1f,4.0f);
                        Disc(Coat.Graphite,0,24.0f,0,3.7f,1.25f,3.7f);
                        Box(Coat.Blue,0,14.3f,-1.84f,1.15f,5.7f,.22f);
                        Disc(Coat.Amber,0,8.1f,0,3.93f,.92f,3.93f); break;
                    case "pressure": Dressing(true); break;
                    case "protect": Dressing(false); break;
                    case "tourniquet":
                        FoldedPack(Coat.Graphite,Coat.Amber,17,3.8f,9.4f);
                        Box(Coat.Steel,0,4.35f,.1f,6.4f,1.25f,4.6f);
                        Box(Coat.Teal,0,4.85f,.1f,4.9f,.55f,2.7f);
                        Part(PrimitiveType.Cylinder,Coat.Graphite,0,5.85f,.35f,1.55f,13.5f,1.55f,12,90);
                        break;
                    case "loosen":
                        Box(Coat.Graphite,0,.75f,0,14.5f,1.5f,4f);
                        Box(Coat.Steel,1.5f,2.05f,0,6.5f,1.5f,5.5f);
                        Box(Coat.Rust,1.5f,3.02f,0,4.3f,.85f,3.2f);
                        Box(Coat.Amber,-6.4f,1.85f,0,1.65f,.68f,4f); break;
                    case "preserve":
                        Box(Coat.Teal,0,4.6f,0,18.6f,9.2f,13.2f);
                        Box(Coat.Linen,0,9.3f,0,19.2f,1.4f,13.8f);
                        foreach(float side in new[]{-1f,1f})
                        {
                            Box(Coat.Amber,side*5.55f,8.3f,-6.94f,2.6f,3.25f,.8f);
                            Box(Coat.Graphite,side*3.7f,10.9f,0,1.4f,2.15f,1.7f);
                        }
                        Box(Coat.Graphite,0,12.0f,0,8.8f,1.2f,1.72f); break;
                    case "shelter":
                        FoldedPack(Coat.Blue,Coat.Graphite,20.9f,8.3f,14.1f);
                        Round(Coat.Teal,0,8.4f,.75f,18.1f,3.8f,10.9f);
                        Box(Coat.Amber,0,10.17f,.65f,2.7f,.5f,10.1f); break;
                    case "monitor":
                        Box(Coat.Graphite,0,2.5f,-2.8f,8.8f,5f,15.5f);
                        Box(Coat.Steel,0,2.9f,4.1f,11.8f,5.8f,10.6f);
                        Box(Coat.Teal,0,5.96f,4.1f,8.9f,.37f,6.7f);
                        Box(Coat.Mint,0,6.19f,4.1f,6.4f,.3f,.5f);
                        Disc(Coat.Amber,0,5.13f,-3.4f,2.4f,.45f,2.4f); break;
                    case "water":
                        Disc(Coat.Blue,0,6.95f,0,7.1f,13.9f,7.1f);
                        Round(Coat.Blue,0,13.83f,0,7.1f,5.4f,7.1f);
                        Disc(Coat.Linen,0,6.8f,0,7.3f,5.1f,7.3f);
                        Disc(Coat.Teal,0,16.8f,0,4.2f,2.4f,4.2f); break;
                    default: throw new ArgumentOutOfRangeException(nameof(action),action,"No equipment model");
                }
            }

            static Material Finish(Coat coat)
            {
                string path=Directory+"/"+coat+".mat";
                var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
                if(!mat)
                {
                    mat=new Material(Shader.Find("Standard"));
                    ColorUtility.TryParseHtmlString("#"+Swatches[(int)coat],out var tint);
                    mat.color=tint;
                    mat.SetFloat("_Glossiness",coat==Coat.Steel?.42f:.21f);
                    AssetDatabase.CreateAsset(mat,path);
                }
                return mat;
            }

            public void Assign(GameObject target,string action)
            {
                var meshes=new List<Mesh>(); var materials=new List<Material>();
                foreach(var layer in layers)
                {
                    var mesh=new Mesh();
                    mesh.CombineMeshes(layer.Value.ToArray());
                    meshes.Add(mesh);
                    materials.Add(Finish(layer.Key));
                }
                var combined=new Mesh{name=action+" training prop"};
                combined.CombineMeshes(meshes.Select(m=>new CombineInstance{mesh=m,transform=Matrix4x4.identity}).ToArray(),false,false);
                foreach(var mesh in meshes) Object.DestroyImmediate(mesh);
                string path=Directory+"/"+action+".asset";
                var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if(existing)
                {
                    EditorUtility.CopySerialized(combined,existing);
                    EditorUtility.SetDirty(existing);
                    Object.DestroyImmediate(combined);
                }
                else { AssetDatabase.CreateAsset(combined,path);existing=combined; }
                target.GetComponent<MeshFilter>().sharedMesh=existing;
                target.GetComponent<MeshRenderer>().sharedMaterials=materials.ToArray();
            }
        }
    }
}
