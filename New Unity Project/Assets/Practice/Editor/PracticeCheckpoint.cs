using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace First180.Practice.Editor
{
    [InitializeOnLoad]
    public static class PracticeCheckpoint
    {
        [Serializable] class Command { public string id, action, scene, selection; public Vector3 eye, subject; public float fov = 65; }
        static string LastKey="First180.Practice.LastEditorCommand";
        static double next;
        static PracticeCheckpoint(){EditorApplication.update+=Tick;}
        static void Tick()
        {
            if(EditorApplication.timeSinceStartup<next || EditorApplication.isCompiling || EditorApplication.isUpdating)return;
            next=EditorApplication.timeSinceStartup+1;
            string path=Path.GetFullPath(Path.Combine(Application.dataPath,"../../tmp/practice-editor-command.json"));
            if(!File.Exists(path))return;
            try
            {
                var c=JsonUtility.FromJson<Command>(File.ReadAllText(path));
                if(c==null || c.id==SessionState.GetString(LastKey,""))return;
                SessionState.SetString(LastKey,c.id);
                if(c.action=="play" && EditorUtility.scriptCompilationFailed) throw new InvalidOperationException("Resolve compilation errors before starting Play Mode.");
                if((c.action=="smoke" || c.action=="smokecaptures") && !EditorApplication.isPlaying) throw new InvalidOperationException("Start Play Mode before running the test.");
                if(c.action!="inspect") DocumentationCamera.Clear();
                if(c.action=="applyart" && !EditorApplication.isPlaying)
                {
                    if(EditorSceneManager.GetActiveScene().isDirty) throw new InvalidOperationException("Save your scene before applying a documented revision.");
                    PracticeEnvironmentArt.Apply();
                    if(!EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene())) throw new IOException("Could not save the scenario.");
                }
                else if(c.action=="frame" && !EditorApplication.isPlaying) DocumentationCamera.Frame(c.eye,c.subject,c.fov);
                else if(c.action=="windows" && !EditorApplication.isPlaying) LobbyWindowPhotos.Bake();
                else if(c.action=="routes" && !EditorApplication.isPlaying) EnvironmentRouteChecks.Run();
                else if(c.action=="sharedart" && !EditorApplication.isPlaying)
                {
                    var scene = EditorSceneManager.GetActiveScene();
                    if (scene.isDirty) throw new InvalidOperationException("Save the scene before updating its equipment.");
                    var room = UnityEngine.Object.FindFirstObjectByType<PracticeRoom>();
                    var art = GameObject.Find("Location art");
                    if (!room || room.scenarioId < 0 || !art) throw new InvalidOperationException("Open an authored practice room.");
                    PracticeEnvironmentArt.ApplyShared(room,art,scene);
                    if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Scene save failed.");
                }
                else if(c.action=="smokecaptures" && EditorApplication.isPlaying) new GameObject("Editor-only documented integration tests").AddComponent<PracticeSmokeTests>().documentCorrection = true;
                else if(c.action=="release" && !EditorApplication.isPlaying) PracticeBenchmarkBuild.BuildRelease();
                else if(c.action=="benchmark" && !EditorApplication.isPlaying) PracticeBenchmarkBuild.Build();
                else if(c.action=="refresh") AssetDatabase.Refresh();
                else if(c.action=="stop")EditorApplication.isPlaying=false;
                else if(c.action=="upgrade" && !EditorApplication.isPlaying)PracticeSystemsBuilder.Upgrade();
                else if(c.action=="verify" && !EditorApplication.isPlaying)PracticeVerification.Run();
                else if(c.action=="smoke" && EditorApplication.isPlaying)new GameObject("Editor-only integration tests").AddComponent<PracticeSmokeTests>();
                else if(c.action=="play")EditorApplication.isPlaying=true;
                else if(c.action=="scene" && !EditorApplication.isPlaying)
                {
                    if(EditorSceneManager.GetActiveScene().isDirty){Debug.LogError("Checkpoint refused to discard unsaved scene changes.");return;}
                    EditorSceneManager.OpenScene("Assets/Practice/Scenes/"+c.scene+".unity");
                    var view=SceneView.GetWindow<SceneView>();view.Focus();view.sceneLighting=true;
                    view.LookAt(new Vector3(0,1,1),Quaternion.Euler(18,0,0),11,false,true);
                }
                else if(c.action=="inspect")Selection.activeGameObject=GameObject.Find(c.selection);
                else if(c.action=="game")DocumentationCamera.FocusGame();
                Debug.Log("CHECKPOINT_COMMAND_DONE="+c.id);
            }
            catch(Exception e){Debug.LogException(e);}
        }
    }
}
