using System.Collections.Generic;
using UnityEngine;

namespace First180.Practice
{
    [DefaultExecutionOrder(300)]
    public sealed class ScenarioRoutine : MonoBehaviour
    {
        PracticeRoom room;
        Errand lead, peer;
        Witness[] witnesses;
        bool forCare;
        Transform viewer;
        public bool ReadyForIncident => lead != null && peer != null && lead.Ready && peer.Ready;
        public bool HasCollectedProp => lead != null && lead.Collected;
        public bool PeerHasCollectedProp => peer != null && peer.Collected;
        public int WitnessCount => witnesses == null ? 0 : witnesses.Length;
        public float LeadDistanceTravelled => lead == null ? 0f : lead.walker.Distance;
        public float PeerDistanceTravelled => peer == null ? 0f : peer.walker.Distance;
        public Transform Lead => room.patients[0].transform;
        public Transform ActiveProp => lead?.prop;
        public Vector3 TreatmentMark => lead == null ? Vector3.zero : lead.mark;

        // All coordinates are on the authored floor, with the counter pickup just inside arm's reach.
        public static ScenarioRoutine StartFor(PracticeRoom room)
        {
            if (room.scenarioId < 0 || room.scenarioId > 4) return null;
            Animator rig = room.patients[0].GetComponentInChildren<Animator>();
            if (!rig) return null;
            var routine = room.gameObject.AddComponent<ScenarioRoutine>();
            routine.enabled = false;
            routine.room = room;
            routine.ConfigureVenue(rig);
            routine.enabled = true;
            return routine;
        }

        static Vector3 Floor(float x, float z) => new Vector3(x, 0f, z);

        void ConfigureVenue(Animator rig)
        {
            viewer = Camera.main ? Camera.main.transform : null;
            var art = Resources.Load<RoutineProps>("RoutineProps");
            Animator adult = null, child = null;
            foreach (var patient in room.patients)
            {
                if (patient.patientId == 0) continue;
                var figure = patient.GetComponentInChildren<Animator>();
                if (figure.name.Contains("Boy") || figure.name.Contains("Girl")) child = figure;
                else adult = figure;
            }
            var alternateChild = child.name.Contains("Boy") ? art.girl : art.boy;
            Witness Service(string name, Vector3 start, Vector3 end) =>
                AddWitness(adult.gameObject, adult.transform.localRotation, null, name, start, end, .91f, 1.65f);
            Witness Visitor(string name, Vector3 start, Vector3 end) =>
                AddWitness(alternateChild, child.transform.localRotation, art.cast, name, start, end, .79f, 1.25f);

            Vector3 start, aisle, pickup, counterProp;
            switch (room.scenarioId)
            {
                case 0:
                    start = Floor(-6.45f, 4.28f); pickup = Floor(-4.35f, 4.73f);
                    counterProp = new Vector3(-4.12f, 1.22f, 5.10f); aisle = Floor(-4.35f, 2.48f);
                    witnesses = new[] { Service("Barista | espresso pass", Floor(-7.35f,6.71f), Floor(-3.85f,6.71f)),
                        Visitor("Guest | window walk", Floor(5.88f,5.60f), Floor(5.88f,-1.75f)) };
                    break;
                case 1:
                    start = Floor(-8.06f, 4.52f); pickup = Floor(-6.56f, 4.66f);
                    counterProp = new Vector3(-6.33f, .994f, 5.06f); aisle = Floor(-6.28f, 2.38f);
                    witnesses = new[] { Service("Server | salad pass", Floor(.85f,6.78f), Floor(.85f,4.08f)),
                        Visitor("Student | commons aisle", Floor(-.85f,5.6f), Floor(-.85f,2.75f)) };
                    break;
                case 2:
                    start = Floor(-8.32f, -.15f); pickup = Floor(-6.30f, .62f);
                    counterProp = new Vector3(-6.12f, 1.105f, 1.02f); aisle = Floor(-6.02f,-.45f);
                    witnesses = new[] { Service("Dispatcher | loading shoulder", Floor(.88f,2.40f), Floor(3.65f,2.40f)),
                        Visitor("Pedestrian | shoulder walkway", Floor(7.46f,-2.3f), Floor(7.46f,2.03f)) };
                    break;
                case 3:
                    start = Floor(-1.89f, 2.94f); pickup = Floor(-3.6f, 3.75f);
                    counterProp = new Vector3(-3.39f, .996f, 4.15f); aisle = Floor(-2.48f,2.66f);
                    witnesses = new[] { Service("Technician | bench inspection", Floor(-.31f,4.7f), Floor(-.31f,7.01f)),
                        Visitor("Visitor | observation aisle", Floor(7.34f,-1.54f), Floor(7.34f,2.42f)) };
                    break;
                default:
                    start = Floor(1.55f, 4.13f); pickup = Floor(3.69f, 2.8f);
                    counterProp = new Vector3(3.90f, 1.025f, 3.18f); aisle = Floor(3.27f,2.12f);
                    witnesses = new[] { Service("Hiker | refuge approach", Floor(-.83f,2.50f), Floor(-.83f,5.81f)),
                        Visitor("Walker | eastern trail", Floor(7.18f,.66f), Floor(7.18f,-3.05f)) };
                    break;
            }
            Vector3 otherStart, otherPickup, otherItem, otherAisle;
            switch (room.scenarioId)
            {
                case 0:
                    otherStart = Floor(8.45f,2.12f); otherPickup = Floor(7.93f,3.57f);
                    otherItem = new Vector3(7.85f,.83f,3.99f); otherAisle = Floor(5.71f,1.72f); break;
                case 1:
                    otherStart = Floor(8.48f,6.23f); otherPickup = Floor(6.76f,6.89f);
                    otherItem = new Vector3(6.72f,1.228f,7.34f); otherAisle = Floor(7.52f,6.03f); break;
                case 2:
                    otherStart = Floor(8.59f,1.56f); otherPickup = Floor(6.78f,2.73f);
                    otherItem = new Vector3(6.94f,1.105f,3.12f); otherAisle = Floor(5.70f,2.08f); break;
                case 3:
                    otherStart = Floor(5.63f,2.94f); otherPickup = Floor(3.16f,3.45f);
                    otherItem = new Vector3(3.32f,.993f,3.86f); otherAisle = Floor(4.62f,2.38f); break;
                default:
                    otherStart = Floor(-8.30f,-1.46f); otherPickup = Floor(-7.04f,-.83f);
                    otherItem = new Vector3(-6.85f,.976f,-.42f); otherAisle = Floor(-6.78f,-1.49f); break;
            }
            var stations = new RoutineStations(transform, art);
            if (room.scenarioId == 2)
                witnesses[0].trolley = stations.DispatchTrolley(witnesses[0].first);
            var firstProp = stations.Create(room.scenarioId, counterProp);
            var secondProp = stations.CreateAlternate(room.scenarioId, otherItem);
            var adultPatient = adult.GetComponentInParent<PracticePatient>().transform;
            bool clinicalAtFirstStation = Random.value < .5f;
            var first = clinicalAtFirstStation ? Lead : adultPatient;
            var second = clinicalAtFirstStation ? adultPatient : Lead;
            var firstErrand = Prepare(first, clinicalAtFirstStation ? rig : adult, pickup, counterProp, firstProp, aisle, -1.62f);
            var secondErrand = Prepare(second, clinicalAtFirstStation ? adult : rig, otherPickup, otherItem, secondProp, otherAisle, room.scenarioId == 1 ? .72f : .30f);
            firstErrand.StartAt(start);
            secondErrand.StartAt(otherStart);
            SpaceCrossingPaths(firstErrand, secondErrand);
            lead = clinicalAtFirstStation ? firstErrand : secondErrand;
            peer = clinicalAtFirstStation ? secondErrand : firstErrand;
        }

        // Give crossing pedestrians a natural head start when their randomized assignments
        // share an aisle. This also protects players running the simulation at low frame rates.
        static void SpaceCrossingPaths(Errand first, Errand second)
        {
            const float sampleSeconds = .12f;
            const int maxWait = 25;
            int frames = Mathf.CeilToInt(Mathf.Max(first.Duration, second.Duration) / sampleSeconds) + maxWait + 1;
            var a = new Vector3[frames];
            var b = new Vector3[frames];
            for (int i = 0; i < frames; i++)
            {
                a[i] = first.PositionAt(i * sampleSeconds);
                b[i] = second.PositionAt(i * sampleSeconds);
            }
            float bestClearance = -1f;
            int bestFirst = 0, bestSecond = 0;
            for (int delay = 0; delay <= maxWait; delay++)
            {
                for (int turn = 0; turn < (delay == 0 ? 1 : 2); turn++)
                {
                    int da = turn == 0 ? delay : 0, db = turn == 0 ? 0 : delay;
                    float clearance = float.MaxValue;
                    for (int i = 0; i < frames; i++)
                        clearance = Mathf.Min(clearance, (a[Mathf.Max(0,i-da)] - b[Mathf.Max(0,i-db)]).sqrMagnitude);
                    if (clearance > bestClearance) { bestClearance = clearance; bestFirst = da; bestSecond = db; }
                    if (clearance >= .98f) { first.startDelay = da*sampleSeconds; second.startDelay = db*sampleSeconds; return; }
                }
            }
            first.startDelay = bestFirst * sampleSeconds;
            second.startDelay = bestSecond * sampleSeconds;
        }

        Errand Prepare(Transform actor, Animator rig, Vector3 pickup, Vector3 item, Transform prop, Vector3 aisle, float lane)
        {
            return new Errand {
                actor = actor, walker = new RoutineWalker(actor, rig), mark = actor.position, pickup = pickup, item = item, prop = prop,
                path = Approach(actor, pickup, aisle, lane), halfHeight = item.y - prop.GetComponentInChildren<MeshRenderer>().bounds.min.y,
                support = room.scenarioId == 1 || room.scenarioId == 2 || room.scenarioId == 4
            };
        }

        // Stay in the open cross aisle until aligned with the reserved triage spot.
        // Walk around the two other assessed patients when their randomized marks lie in the path.
        Vector3[] Approach(Transform actor, Vector3 pickup, Vector3 aisleEntry, float crossAisleZ)
        {
            var points = new List<Vector3>();
            Vector3 careMark = actor.position;
            Vector3 previous = pickup;
            foreach (var next in new[] { aisleEntry, new Vector3(aisleEntry.x, 0, crossAisleZ),
                new Vector3(careMark.x, 0, crossAisleZ), careMark })
            {
                // A final turn may be only a few centimetres from the care mark.
                // Skip identical points only, so every errand ends on its assigned mark.
                if ((previous - next).sqrMagnitude < .000001f) continue;
                for (int i = 0; i < room.patients.Length; i++)
                {
                    if (room.patients[i].transform == actor) continue;
                    Vector3 person = room.patients[i].transform.position;
                    Vector3 line = next - previous;
                    float t = Mathf.Clamp01(Vector3.Dot(person - previous, line) / line.sqrMagnitude);
                    Vector3 nearest = previous + line * t;
                    if ((person - nearest).sqrMagnitude >= .72f * .72f) continue;
                    Vector3 side = Vector3.ProjectOnPlane(nearest - person, line.normalized);
                    if (side.sqrMagnitude < .0001f) side = Vector3.Cross(Vector3.up, line);
                    Vector3 skirt = person + side.normalized * .95f;
                    points.Add(skirt);
                    previous = skirt;
                }
                points.Add(next);
                previous = next;
            }
            return points.ToArray();
        }

        sealed class Witness
        {
            public RoutineWalker walker;
            public Transform trolley;
            public bool fittedTrolley;
            public Vector3 first, second;
            public float speed, dwell, remaining;
            public bool turn;
        }

        Witness AddWitness(GameObject model, Quaternion facing, Material outfit, string name, Vector3 from, Vector3 to, float speed, float dwell)
        {
            var actor = new GameObject(name).transform;
            actor.SetParent(transform, true);
            actor.position = from;
            var copy = Instantiate(model, actor, false);
            copy.transform.localRotation = facing;
            if (outfit)
                foreach (var skin in copy.GetComponentsInChildren<SkinnedMeshRenderer>()) skin.sharedMaterial = outfit;
            var clone = copy.GetComponent<Animator>();
            // No controller, clinical script, target, or collider is attached to the scenic people.
            clone.enabled = false;
            var walker = new RoutineWalker(actor, clone);
            walker.Face(to - from, 1f);
            return new Witness { walker = walker, first = from, second = to, speed = speed, dwell = dwell };
        }

        sealed class Errand
        {
            public Transform actor, prop;
            public RoutineWalker walker;
            public Vector3 mark, pickup, item;
            public Vector3[] path;
            public bool support;
            public float halfHeight, startDelay;
            int leg;
            float pickupSeconds, dropSeconds;
            Vector3 start, startDrop, endDrop;
            public float Duration { get; private set; }
            public bool Ready { get; private set; }
            public bool Collected { get; private set; }

            public void StartAt(Vector3 start)
            {
                this.start = start;
                actor.position = start;
                walker.Face(pickup - start, 1f);
                Duration = Vector3.Distance(start, pickup) / 1.16f + 1.10f;
                Vector3 from = pickup;
                foreach (var to in path) { Duration += Vector3.Distance(from, to)/1.18f; from = to; }
            }

            public Vector3 PositionAt(float time)
            {
                float approach = Vector3.Distance(start, pickup) / 1.16f;
                if (time <= approach) return Vector3.MoveTowards(start, pickup, 1.16f*time);
                time -= approach + 1.10f;
                if (time <= 0f) return pickup;
                var from = pickup;
                foreach (var to in path)
                {
                    float travel = Vector3.Distance(from, to)/1.18f;
                    if (time <= travel) return Vector3.MoveTowards(from, to, time*1.18f);
                    time -= travel; from = to;
                }
                return mark;
            }

            public void Advance(float dt)
            {
                if (startDelay > 0f)
                {
                    float held = Mathf.Min(dt, startDelay);
                    startDelay -= held;
                    dt -= held;
                }
                while (!Ready && dt > .00001f)
                {
                    if (leg == 1)
                    {
                        float spent = Mathf.Min(dt, 1.10f - pickupSeconds);
                        walker.Rest(spent);
                        walker.Face(item - actor.position, spent);
                        pickupSeconds += spent;
                        Collected = pickupSeconds >= .68f;
                        if (pickupSeconds >= 1.09999f) leg++;
                        dt -= spent;
                    }
                    else if (leg == 0 || leg - 2 < path.Length)
                    {
                        Vector3 destination = leg == 0 ? pickup : path[leg - 2];
                        float speed = leg == 0 ? 1.16f : 1.18f;
                        float spent = Mathf.Min(dt, Vector3.Distance(walker.Position, destination)/speed);
                        if (walker.Walk(destination, speed, spent)) leg++;
                        dt -= spent;
                    }
                    else { Ready = walker.Settle(dt); break; }
                }
            }

            public void Pose()
            {
                walker.Pose(Collected, leg == 1 && !Collected ? item : (Vector3?)null, support);
                if (Collected && walker.Hand)
                {
                    var grip = walker.GripCenter(support);
                    prop.position = leg == 1 ? Vector3.Lerp(item, grip, Mathf.SmoothStep(0f, 1f, (pickupSeconds - .68f) / .4f)) : grip;
                }
            }

            public void Interrupt()
            {
                walker.ReturnToMark(mark);
                startDrop = prop.position;
                endDrop = mark + new Vector3(.46f, halfHeight, .32f);
            }

            public void Release(float dt)
            {
                if (!Collected || dropSeconds >= .38f) return;
                dropSeconds += dt;
                float t = Mathf.Clamp01(dropSeconds / .38f);
                prop.position = Vector3.Lerp(startDrop, endDrop, t*t);
            }
        }

        void Update()
        {
            if (lead == null || peer == null) return;
            if (!forCare) { lead.walker.Restore(); peer.walker.Restore(); }
            foreach (var witness in witnesses) witness?.walker.Restore();
            if (room.Paused || room.result != null) return;
            Advance(Time.deltaTime);
        }

        public void Advance(float dt)
        {
            if (dt <= 0f || room.Paused || room.result != null) return;
            if (!forCare)
            {
                lead.Advance(dt);
                peer.Advance(dt);
            }
            else
            {
                lead.Release(dt);
                peer.Release(dt);
                peer.walker.Face(Lead.position - peer.walker.Position, dt);
            }
            foreach (var witness in witnesses)
            {
                if (witness == null) continue;
                if (forCare)
                {
                    witness.walker.Rest(dt);
                    witness.walker.Face(Lead.position - witness.walker.Position, dt);
                }
                else if (witness.remaining > 0f)
                {
                    witness.remaining -= dt;
                    witness.walker.Rest(dt);
                }
                else
                {
                    Vector3 goal = witness.turn ? witness.first : witness.second;
                    if (NeedsSpace(witness.walker.Position, goal)) witness.walker.Rest(dt);
                    else if (witness.walker.Walk(goal, witness.speed, dt))
                    {
                        witness.turn = !witness.turn;
                        witness.remaining = witness.dwell;
                    }
                }
            }
        }

        bool NeedsSpace(Vector3 feet, Vector3 goal)
        {
            Vector3 direction = (goal - feet).normalized;
            bool InFront(Vector3 position)
            {
                var offset = position - feet;
                offset.y = 0;
                return offset.sqrMagnitude < 1.05f && Vector3.Dot(offset, direction) > .04f;
            }
            return (viewer && InFront(viewer.position)) || InFront(lead.walker.Position) || InFront(peer.walker.Position);
        }

        void LateUpdate()
        {
            if (lead == null || peer == null) return;
            if (!forCare) { lead.Pose(); peer.Pose(); }
            foreach (var witness in witnesses)
            {
                bool pushing = witness.trolley && !forCare;
                witness.walker.Pose(pushing, null, pushing);
                if (!pushing) continue;
                var grip = witness.walker.GripCenter(true);
                witness.trolley.SetPositionAndRotation(new Vector3(grip.x, witness.walker.Position.y, grip.z),
                    Quaternion.LookRotation(witness.walker.Facing, Vector3.up));
                // Keep all four casters on the pavement while matching this character's grip height.
                if (!witness.fittedTrolley)
                {
                    witness.trolley.localScale = new Vector3(1f, Mathf.Clamp((grip.y - witness.walker.Position.y) / .86f, .8f, 1.3f), 1f);
                    witness.fittedTrolley = true;
                }
            }
        }

        public void InterruptForCare()
        {
            if (forCare) return;
            forCare = true;
            lead.Interrupt();
            peer.Interrupt();
            Physics.SyncTransforms();
        }
    }
}
