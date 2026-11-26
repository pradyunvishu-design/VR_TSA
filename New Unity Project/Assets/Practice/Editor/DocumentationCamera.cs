using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
namespace First180.Practice.Editor
{
    // An Edit Mode camera allows close-up documentation without moving the player rig.
    [InitializeOnLoad]
    static class DocumentationCamera
    {
        static Camera preview;
        static EditorWindow maximizedForCapture;
        static DocumentationCamera()
        {
            AssemblyReloadEvents.beforeAssemblyReload += Clear;
            EditorApplication.playModeStateChanged += _ => Clear();
            EditorApplication.quitting += Clear;
        }
        internal static void Clear()
        {
            if (preview) Object.DestroyImmediate(preview.gameObject);
            if (maximizedForCapture) maximizedForCapture.maximized = false;
            maximizedForCapture = null;
        }
        internal static void Frame(Vector3 eye, Vector3 subject, float fov, bool maximize = false)
        {
            Clear();
            var go = new GameObject("Temporary documentation camera", typeof(Camera));
            go.hideFlags = HideFlags.HideAndDontSave;
            preview = go.GetComponent<Camera>();
            if (Camera.main) preview.CopyFrom(Camera.main);
            preview.enabled = true;
            preview.depth = 100;
            preview.stereoTargetEye = StereoTargetEyeMask.None;
            preview.targetTexture = null;
            preview.fieldOfView = Mathf.Clamp(fov,35,90);
            go.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(subject-eye));
            FocusGame(maximize);
        }
        internal static void FocusGame(bool maximize = false)
        {
            foreach (var scene in Resources.FindObjectsOfTypeAll<SceneView>())
                if (!scene.docked) scene.Close();
            var game = EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView"));
            if (maximize && !game.maximized)
            {
                game.maximized = true;
                maximizedForCapture = game;
            }
            game.Focus(); game.Repaint();
            EditorApplication.QueuePlayerLoopUpdate();
        }
    }
}
