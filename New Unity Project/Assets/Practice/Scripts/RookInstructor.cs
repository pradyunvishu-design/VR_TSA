using UnityEngine;
using UnityEngine.UI;

namespace First180.Practice
{
    // The same caption and portrait are presented on the monitor and in stereo.
    public class RookInstructor : MonoBehaviour
    {
        const int FramesPerGesture = 8;
        const int AtlasRows = 4;
        const int FramePixels = 128;
        const float FramesPerSecond = 8;

        PracticeRoom room;
        PracticeSession session;
        Canvas canvas;
        CanvasScaler scaler;
        CanvasGroup group;
        RectTransform card;
        Text caption;
        RawImage portrait;
        bool animatedPortrait;
        int displayedFrame = -1;
        float duration, elapsed;
        bool spatial, reposition;

        void Awake()
        {
            room = GetComponent<PracticeRoom>();
            session = PracticeSession.Get();
            var root = new GameObject("ROOK instructor display", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(CanvasGroup));
            root.transform.SetParent(transform, false);
            canvas = root.GetComponent<Canvas>();
            scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600, 900);
            scaler.matchWidthOrHeight = .5f;
            group = root.GetComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;
            card = Element("Guidance", root.transform, 0, 0, 760, 134);
            card.gameObject.AddComponent<Image>().color = new Color(.025f, .075f, .095f, .97f);
            portrait = Element("Heart robot", card, 8, 5, 124, 124).gameObject.AddComponent<RawImage>();
            var atlas = Resources.Load<Texture2D>("RookAnimation");
            animatedPortrait = atlas && atlas.width == FramePixels * FramesPerGesture && atlas.height == FramePixels * AtlasRows;
            portrait.texture = animatedPortrait ? atlas : Resources.Load<Texture2D>("RookInstructor");
            UpdatePortrait();
            var title = Label("Instructor", 98, 19, 26);
            title.color = new Color(.45f, .96f, .83f);
            title.fontStyle = FontStyle.Bold;
            title.text = "ROOK  /  TRAINING INSTRUCTOR";
            caption = Label("Instruction", 9, 22, 84);
            canvas.sortingOrder = 20;
            Configure(VRInteractionManager.Active);
            canvas.enabled = false;
        }

        static RectTransform Element(string name, Transform parent, float x, float y, float width, float height)
        {
            var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.zero;
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = new Vector2(x, y);
            return rect;
        }

        Text Label(string name, float y, int size, float height)
        {
            var text = Element(name, card, 142, y, 603, height).gameObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.color = Color.white;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 16;
            text.resizeTextMaxSize = size;
            text.alignment = TextAnchor.MiddleLeft;
            text.raycastTarget = false;
            return text;
        }

        public void Present(string message)
        {
            if (session.certification || string.IsNullOrWhiteSpace(message)) return;
            caption.text = message.StartsWith("ROOK: ") ? message.Substring(6) : message;
            elapsed = 0;
            duration = Mathf.Clamp(3f + caption.text.Length / 13f, 6, 30);
            reposition = true;
        }

        void Configure(bool useSpatial)
        {
            spatial = useSpatial;
            canvas.renderMode = spatial ? RenderMode.WorldSpace : RenderMode.ScreenSpaceOverlay;
            scaler.enabled = !spatial;
            var root = (RectTransform)canvas.transform;
            if (spatial)
            {
                root.pivot = Vector2.zero;
                root.sizeDelta = card.sizeDelta;
                root.localScale = Vector3.one * .0013f;
            }
            card.anchoredPosition = spatial ? Vector2.zero : new Vector2(28, 100);
            reposition = true;
        }

        void LateUpdate()
        {
            if (spatial != VRInteractionManager.Active) Configure(!spatial);
            bool visible = !session.certification && !session.loading && !session.dashboard
                && !room.Paused && elapsed < duration;
            canvas.enabled = visible;
            if (!visible) return;
            if (spatial && reposition)
            {
                var head = Camera.main;
                if (!head) { canvas.enabled = false; return; }
                Vector3 forward = Vector3.ProjectOnPlane(head.transform.forward, Vector3.up);
                if (forward.sqrMagnitude < .0001f) forward = Vector3.forward;
                Quaternion facing = Quaternion.LookRotation(forward);
                canvas.transform.SetPositionAndRotation(head.transform.position
                    + facing * new Vector3(-.49f, .71f, 1.65f), facing);
                reposition = false;
            }
            elapsed += Time.unscaledDeltaTime;
            group.alpha = Mathf.Clamp01(Mathf.Min(elapsed / .12f, (duration - elapsed) / .24f));
            UpdatePortrait();
        }

        void UpdatePortrait()
        {
            if (!animatedPortrait) return;

            // Gesture once when a caption appears, then hold the looping idle expression.
            int row = elapsed < FramesPerGesture / FramesPerSecond ? 2 : 0;
            int column = Mathf.FloorToInt(elapsed * FramesPerSecond) % FramesPerGesture;
            int frame = row * FramesPerGesture + column;
            if (frame == displayedFrame) return;

            displayedFrame = frame;
            const float inset = .5f / FramePixels;
            portrait.uvRect = new Rect((column + inset) / FramesPerGesture, (AtlasRows - row - 1 + inset) / AtlasRows,
                (1 - inset * 2) / FramesPerGesture, (1 - inset * 2) / AtlasRows);
        }
    }
}
