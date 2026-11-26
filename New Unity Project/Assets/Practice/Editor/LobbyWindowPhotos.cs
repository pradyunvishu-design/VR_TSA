using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace First180.Practice.Editor
{
    public static class LobbyWindowPhotos
    {
        const string Folder = "Assets/Practice/Resources/LocationViews";
        static readonly Vector3[] Eyes = {
            new Vector3(1.4f,1.85f,-3),new Vector3(.8f,2.1f,-2.8f),new Vector3(-3.6f,2,.3f),
            new Vector3(1.7f,1.65f,1.8f),new Vector3(1.6f,1.9f,-6.7f)};
        static readonly Vector3[] Subjects = {
            new Vector3(0,1.6f,6),new Vector3(.8f,1.5f,6),new Vector3(.4f,1.7f,7),
            new Vector3(3.3f,1.35f,4.5f),new Vector3(-4.4f,1.9f,4.1f)};
        [MenuItem("FIRST180 Practice/Environment/Photograph destinations for lobby windows")]
        public static void Bake()
        {
            var open = SceneManager.GetActiveScene();
            if(EditorApplication.isPlaying || open.isDirty || SceneManager.sceneCount != 1 || string.IsNullOrEmpty(open.path))
                throw new InvalidOperationException("Start with one saved scene in Edit Mode.");
            string originalPath = open.path;
            if(!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/Practice/Resources","LocationViews");
            try
            {
                foreach(var scenario in ScenarioCatalog.All)
                {
                    EditorSceneManager.OpenScene("Assets/Practice/Scenes/" + scenario.scene + ".unity");
                    Photograph(scenario.id,scenario.scene);
                }
            }
            finally { EditorSceneManager.OpenScene(originalPath); }
            AssetDatabase.SaveAssets();
        }
        static void Photograph(int id, string destination)
        {
            var camera = new GameObject("Window photograph").AddComponent<Camera>();
            camera.gameObject.hideFlags = HideFlags.HideAndDontSave;
            camera.transform.SetPositionAndRotation(Eyes[id],Quaternion.LookRotation(Subjects[id]-Eyes[id]));
            camera.fieldOfView=56;
            var rt=RenderTexture.GetTemporary(512,256,24,RenderTextureFormat.ARGB32);
            var pixels = new Texture2D(512,256,TextureFormat.RGB24,false);
            RenderTexture previous=RenderTexture.active;
            string path=Folder+"/"+destination;
            try
            {
                camera.targetTexture=rt;
                camera.Render();
                RenderTexture.active=rt;
                pixels.ReadPixels(new Rect(0,0,512,256),0,0);
                pixels.Apply();
                File.WriteAllBytes(path+"-photo.jpg",pixels.EncodeToJPG(88));
            }
            finally
            {
                camera.targetTexture=null;RenderTexture.active=previous;
                RenderTexture.ReleaseTemporary(rt);
                Object.DestroyImmediate(pixels);Object.DestroyImmediate(camera.gameObject);
            }
            AssetDatabase.ImportAsset(path+"-photo.jpg",ImportAssetOptions.ForceSynchronousImport);
            var mat=AssetDatabase.LoadAssetAtPath<Material>(path+".mat");
            if(!mat)
            {
                mat=new Material(Shader.Find("Unlit/Texture"));
                AssetDatabase.CreateAsset(mat,path+".mat");
            }
            mat.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(path+"-photo.jpg");
            EditorUtility.SetDirty(mat);
        }
    }
}
