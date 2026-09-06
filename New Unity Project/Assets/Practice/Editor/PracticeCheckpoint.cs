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
        [Serializable] class Command { public string id, action, scene, selection; }
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
                if(c.action=="stop")EditorApplication.isPlaying=false;
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
                else if(c.action=="game")EditorApplication.ExecuteMenuItem("Window/General/Game");
                Debug.Log("CHECKPOINT_COMMAND_DONE="+c.id);
            }
            catch(Exception e){Debug.LogException(e);}
        }
    }
}
