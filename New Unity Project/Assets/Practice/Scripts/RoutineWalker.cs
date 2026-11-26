using System.Collections.Generic;
using UnityEngine;

namespace First180.Practice
{
    // Layers distance-driven steps over the team's own skeleton. Clinical poses regain all
    // joints before the first emergency frame. There is no animation on the medical targets.
    internal sealed class RoutineWalker
    {
        readonly Transform actor;
        readonly Transform[] joints;
        readonly Quaternion[] before;
        readonly Quaternion initialRotation;
        readonly Vector3 facingInActor;
        readonly float stride;
        bool applied;
        float phase, intensity;

        enum Joint { LeftThigh, RightThigh, LeftShin, RightShin, LeftFoot, RightFoot,
            LeftArm, RightArm, LeftForearm, RightForearm, LeftHand, RightHand, Chest }
        static readonly string[] jointNames = { "Thigh_L", "Thigh_R", "Shin_L", "Shin_R", "Foot_L", "Foot_R",
            "UpperArm_L", "UpperArm_R", "Forearm_L", "Forearm_R", "Hand_L", "Hand_R", "Chest" };

        public Transform Hand => joints[(int)Joint.RightHand];
        public Vector3 Position => actor.position;
        public Vector3 Facing => actor.TransformDirection(facingInActor);
        public float Distance { get; private set; }
        public Transform Actor => actor;
        public float Height { get; }

        public RoutineWalker(Transform actor, Animator animator)
        {
            this.actor = actor;
            initialRotation = actor.rotation;
            var bones = new Dictionary<string, Transform>();
            foreach (var bone in animator.GetComponentsInChildren<Transform>(true)) bones[bone.name] = bone;
            joints = new Transform[jointNames.Length];
            before = new Quaternion[joints.Length];
            for (int i = 0; i < joints.Length; i++) bones.TryGetValue(jointNames[i], out joints[i]);
            Vector3 front = actor.forward * -1f;
            if (bones.TryGetValue("AssessmentTarget", out var assessment) && bones.TryGetValue("Head", out var head))
            {
                Vector3 offset = Vector3.ProjectOnPlane(assessment.position - head.position, Vector3.up);
                if (offset.sqrMagnitude > .0001f) front = offset.normalized;
            }
            facingInActor = actor.InverseTransformDirection(front);
            Height = bones.TryGetValue("Head", out var h) ? Mathf.Clamp(h.position.y - actor.position.y + .16f, 1.12f, 2f) : 1.65f;
            stride = Mathf.Clamp((Height - .16f) * .82f, .72f, 1.42f);
        }

        public bool Walk(Vector3 destination, float speed, float dt)
        {
            Vector3 delta = destination - actor.position;
            float remaining = delta.magnitude;
            if (remaining < .012f) { actor.position = destination; Rest(dt); return true; }
            Face(delta, dt);
            float moved = Mathf.Min(speed * dt, remaining);
            actor.position += delta * (moved / remaining);
            Distance += moved;
            phase += moved * Mathf.PI * 2f / stride;
            intensity = Mathf.MoveTowards(intensity, 1f, dt * 6f);
            return remaining - moved < .012f;
        }

        public void Face(Vector3 direction, float dt)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude < .0001f) return;
            Quaternion yaw = Quaternion.LookRotation(direction) * Quaternion.Inverse(Quaternion.LookRotation(facingInActor));
            actor.rotation = Quaternion.RotateTowards(actor.rotation, yaw, dt * 220f);
        }

        public void Rest(float dt) => intensity = Mathf.MoveTowards(intensity, 0f, dt * 7f);
        public Vector3 GripCenter(bool twoHanded)
        {
            Transform other = joints[(int)Joint.LeftHand];
            return twoHanded && other ? (Hand.position + other.position) * .5f + Vector3.up*.045f : Hand.position;
        }
        public bool Settle(float dt)
        {
            Rest(dt);
            actor.rotation = Quaternion.RotateTowards(actor.rotation, initialRotation, 220f * dt);
            return Quaternion.Angle(actor.rotation, initialRotation) < .5f;
        }
        public void ReturnToMark(Vector3 position)
        {
            Restore();
            actor.SetPositionAndRotation(position, initialRotation);
            intensity = 0f;
        }

        public void Restore()
        {
            if (!applied) return;
            for (int i = 0; i < joints.Length; i++) if (joints[i]) joints[i].localRotation = before[i];
            applied = false;
        }

        public void Pose(bool carry, Vector3? reach = null, bool support = false, float gesture = 0f)
        {
            Restore();
            for (int i = 0; i < joints.Length; i++) if (joints[i]) before[i] = joints[i].localRotation;
            applied = true;
            Vector3 front = Vector3.ProjectOnPlane(actor.TransformDirection(facingInActor), Vector3.up).normalized;
            Vector3 right = Vector3.Cross(Vector3.up, front);
            float left = Mathf.Sin(phase) * intensity, rightSwing = -left;
            Rotate(Joint.LeftThigh, right, -23f * left);
            Rotate(Joint.RightThigh, right, -23f * rightSwing);
            Rotate(Joint.LeftShin, right, 31f * Mathf.Max(0f, -Mathf.Cos(phase)) * intensity);
            Rotate(Joint.RightShin, right, 31f * Mathf.Max(0f, Mathf.Cos(phase)) * intensity);
            Rotate(Joint.LeftFoot, right, left * 9f);
            Rotate(Joint.RightFoot, right, rightSwing * 9f);
            Rotate(Joint.Chest, Vector3.up, left * 3.5f);
            SetDirection(Joint.LeftArm, Joint.LeftForearm, Vector3.down - right * .18f + front * (gesture * .52f - .38f * left));
            SetDirection(Joint.LeftForearm, Joint.LeftHand, Vector3.down * (1f - gesture) + front * (.15f + .95f * gesture - .30f * left));
            if (reach.HasValue && Hand)
            {
                Reach(Joint.RightArm, Joint.RightForearm, Joint.RightHand, reach.Value, front, right);
            }
            else
            {
                SetDirection(Joint.RightArm, Joint.RightForearm, Vector3.down + right * .18f + front * (carry ? .22f : .38f * left));
                SetDirection(Joint.RightForearm, Joint.RightHand, carry ? front * .93f + Vector3.up * .23f :
                    Vector3.down + front * (.15f + .30f * left));
            }
            if (support && carry && Hand)
                Reach(Joint.LeftArm, Joint.LeftForearm, Joint.LeftHand, Hand.position - right * .20f, front, -right);
        }

        void Reach(Joint upperJoint, Joint forearmJoint, Joint handJoint, Vector3 target, Vector3 front, Vector3 outward)
        {
            Transform arm = joints[(int)upperJoint], elbow = joints[(int)forearmJoint], hand = joints[(int)handJoint];
            if (!arm || !elbow || !hand) return;
            Vector3 delta = target - arm.position;
            float upper = Vector3.Distance(arm.position, elbow.position);
            float lower = Vector3.Distance(elbow.position, hand.position);
            if (upper < .001f || lower < .001f || delta.sqrMagnitude < .0001f) return;
            Vector3 direction = delta.normalized;
            float distance = Mathf.Clamp(delta.magnitude, Mathf.Abs(upper - lower) + .001f, upper + lower - .001f);
            float projection = (upper * upper + distance * distance - lower * lower) / (2f * distance);
            Vector3 pole = Vector3.ProjectOnPlane(Vector3.down + outward * .35f - front * .15f, direction).normalized;
            Vector3 elbowDirection = direction * projection + pole * Mathf.Sqrt(Mathf.Max(0f, upper * upper - projection * projection));
            SetDirection(upperJoint, forearmJoint, elbowDirection);
            SetDirection(forearmJoint, handJoint, target - elbow.position);
        }

        void Rotate(Joint index, Vector3 axis, float degrees)
        {
            Transform bone = joints[(int)index];
            if (bone) bone.rotation = Quaternion.AngleAxis(degrees, axis) * bone.rotation;
        }

        void SetDirection(Joint index, Joint childIndex, Vector3 direction)
        {
            Transform bone = joints[(int)index], child = joints[(int)childIndex];
            if (!bone || !child || direction.sqrMagnitude < .0001f) return;
            Vector3 existing = child.position - bone.position;
            if (existing.sqrMagnitude > .00001f) bone.rotation = Quaternion.FromToRotation(existing, direction) * bone.rotation;
        }
    }
}
