using System;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace First180.Practice
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PracticePatient))]
    public sealed class CasePatientAnimation : MonoBehaviour
    {
        public enum Beat { Idle, Onset, Distress, CareReaction, Relief, Monitor, FollowUp }

        [Serializable]
        struct TargetBinding
        {
            public TreatmentTarget target;
            public Transform socket;
        }

        [SerializeField] PracticeRoom room;
        [SerializeField] Animator animator;
        [SerializeField] AnimationClip idle, onset, distress, careReaction, relief, monitor, followUp;
        [SerializeField] string reactionAction, followUpAction;
        [SerializeField] TargetBinding[] bindings = Array.Empty<TargetBinding>();
        [SerializeField] SkinnedMeshRenderer intact, injured, coldFingers, handDressing;
        [SerializeField] Transform symptomSocket;

        const float BlendSeconds = .16f;
        readonly AnimationClipPlayable[] poses = new AnimationClipPlayable[7];
        AnimationClip[] clips;
        PlayableGraph graph;
        AnimationMixerPlayable mixer;
        PracticePatient patient;
        PatientManager state;
        TreatmentManager treatment;
        Beat previous;
        float poseTime, previousTime, blendTime, previousRate = 1f;
        int observedActions;
        bool sawEmergency, showedRelief;

        public Beat CurrentBeat { get; private set; }
        public float PoseTime => poseTime;
        public bool DrivesVisual => graph.IsValid();
        public bool HasHandPresentation => coldFingers && handDressing;

        void Start()
        {
            patient = GetComponent<PracticePatient>();
            state = GetComponent<PatientManager>();
            if (!room || !animator || !idle || !onset || !distress)
            {
                if (animator) animator.gameObject.SetActive(false);
                if (patient.characterVisual) patient.characterVisual.GetComponent<MeshRenderer>().enabled = true;
                Debug.LogError("Incomplete case animation setup", this);
                return;
            }
            treatment = room.GetComponent<TreatmentManager>();
            clips = new[] { idle, onset, distress, careReaction, relief, monitor, followUp };
            graph = PlayableGraph.Create(gameObject.name + " case animation");
            graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            mixer = AnimationMixerPlayable.Create(graph, poses.Length);
            for (int i = 0; i < poses.Length; i++)
            {
                if (!clips[i]) continue;
                poses[i] = AnimationClipPlayable.Create(graph, clips[i]);
                poses[i].SetApplyFootIK(false);
                graph.Connect(poses[i], 0, mixer, i);
            }
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            AnimationPlayableOutput.Create(graph, "Case patient", animator).SetSourcePlayable(mixer);
            graph.Play();
            SamplePose();
        }

        void LateUpdate()
        {
            if (!graph.IsValid() || room.Paused || (room.result != null && !room.result.completed)) return;
            if (room.EmergencyActive && !sawEmergency)
            {
                sawEmergency = true;
                if (intact && injured)
                {
                    intact.enabled = false;
                    injured.enabled = true;
                }
                Play(Beat.Onset);
            }

            bool lead = patient.patientId == 0;
            if (sawEmergency)
            {
                if (lead) AdvanceLead();
                else if (CurrentBeat == Beat.Onset && FinishedClip()) Play(Beat.Distress);
            }

            // Keep the treatment target steady while its tool is held.
            float dt = lead && treatment && treatment.progress > 0f ? 0f : Time.deltaTime;
            poseTime += dt * Rate(CurrentBeat);
            previousTime += dt * previousRate;
            blendTime += dt;
            SamplePose();
        }

        void AdvanceLead()
        {
            string reaction = null;
            bool handProtected = false;
            var actions = room.encounter.events;
            for (; observedActions < actions.Count; observedActions++)
            {
                var action = actions[observedActions];
                if (!action.correct) continue;
                handProtected |= action.action == "protect";
                if (action.action == reactionAction || action.action == followUpAction)
                    reaction = action.action;
            }

            if (reaction != null && reaction == followUpAction)
                Play(Beat.FollowUp);
            else if (!showedRelief && ReachedRelief(handProtected))
            {
                showedRelief = true;
                Play(Beat.Relief);
            }
            else if (reaction != null && reaction == reactionAction)
                Play(Beat.CareReaction);
            else if (room.result != null && room.result.completed && CurrentBeat != Beat.Monitor
                && (CurrentBeat == Beat.Distress || FinishedClip()))
                Play(Beat.Monitor);
            else if ((CurrentBeat == Beat.Onset || CurrentBeat == Beat.CareReaction) && FinishedClip())
                Play(Beat.Distress);
        }

        bool ReachedRelief(bool handProtected)
        {
            if (!relief || !state) return false;
            switch (room.scenarioId)
            {
                case 0:
                case 1: return state.treated;
                case 2:
                case 3: return state.bleedingControlled;
                // Shelter also sets treated. Protection is the visible relief cue.
                case 4: return handProtected;
                default: return false;
            }
        }

        float Rate(Beat beat) => patient.patientId == 0 && beat == Beat.Distress && state
            ? Mathf.Lerp(1.35f, 1f, state.stability / 100f) : 1f;

        bool FinishedClip() => poseTime >= clips[(int)CurrentBeat].length;

        void Play(Beat beat)
        {
            if (!clips[(int)beat]) return;
            previous = CurrentBeat;
            previousTime = poseTime;
            previousRate = Rate(previous);
            CurrentBeat = beat;
            poseTime = 0;
            blendTime = 0;
        }

        void SamplePose()
        {
            int current = (int)CurrentBeat;
            int outgoing = (int)previous;
            float weight = outgoing == current ? 1f : Mathf.Clamp01(blendTime / BlendSeconds);
            for (int i = 0; i < poses.Length; i++)
                if (poses[i].IsValid())
                    mixer.SetInputWeight(i, i == current ? weight : i == outgoing ? 1f - weight : 0f);
            SetClipTime(CurrentBeat, poseTime);
            if (weight < 1f) SetClipTime(previous, previousTime);
            graph.Evaluate(0);

            foreach (var binding in bindings)
                binding.target.transform.position = binding.socket.position;
            if (HasHandPresentation)
            {
                coldFingers.enabled = sawEmergency && !showedRelief;
                handDressing.enabled = showedRelief;
            }
            if (patient.injuryVisual && symptomSocket)
                patient.injuryVisual.transform.position = symptomSocket.position;
        }

        void SetClipTime(Beat beat, float time)
        {
            int index = (int)beat;
            float length = clips[index].length;
            bool loop = beat == Beat.Idle || beat == Beat.Distress || beat == Beat.Monitor;
            poses[index].SetTime(loop ? Mathf.Repeat(time, length) : Mathf.Min(time, length));
        }

        void OnDestroy()
        {
            if (graph.IsValid()) graph.Destroy();
        }
    }
}



