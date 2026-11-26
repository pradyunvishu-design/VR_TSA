using UnityEngine;

namespace First180.Practice
{
    // The station materials and prop models are references to assets already in the project.
    internal sealed class RoutineStations
    {
        readonly Transform room;
        readonly RoutineProps art;
        public RoutineStations(Transform room, RoutineProps art) { this.room = room; this.art = art; }

        public Transform Create(int venue, Vector3 center)
        {
            switch (venue)
            {
                case 0: return art.Create(0, room, "Coffee for pickup", center);
                case 1:
                    var returnCart = Cart("Tray return", new Vector3(-6.87f, 0, 5.42f), "TRAY RETURN");
                    for (int i = 0; i < 4; i++)
                        art.Create(1, returnCart, "Returned tray", returnCart.position + new Vector3(-.41f, .965f + i * .026f, .01f));
                    var tray = art.Create(1, room, "Lunch tray", center);
                    art.Create(2, tray, "Sandwich", center + Vector3.up * .061f);
                    return tray;
                case 2:
                    var dock = Cart("Parcel dispatch", new Vector3(-6.52f, 0, 1.36f), "DISPATCH");
                    art.Create(3, dock, "Outbound parcel", dock.position + new Vector3(-.37f, 1.13f, .04f));
                    return art.Create(3, room, "Collected parcel", center);
                case 3:
                    var rack = Group("Fitting bench | tool issue", new Vector3(-4.24f,0,4.98f));
                    Part(rack, "Tool rail", new Vector3(0,1.26f,.12f), new Vector3(1.75f,.5f,.045f), art.enamel);
                    for (int i = 0; i < 3; i++)
                    {
                        float x = -.55f + i * .52f;
                        Part(rack, "Marked hook", new Vector3(x,1.25f,.073f), new Vector3(.055f,.07f,.08f), art.metal);
                        var wrench = art.Create(4, rack, "Stored spanner", rack.position + new Vector3(x,1.21f,.03f));
                        wrench.rotation = Quaternion.Euler(0,0,87);
                    }
                    return art.Create(4, room, "Bench fitting spanner", center);
                default:
                    var marker = Group("Ridge lookout | trail map station", new Vector3(4.00f,0,3.40f));
                    foreach (float side in new[] { -.62f, .62f })
                        Part(marker, "Trail post", new Vector3(side,.84f,.1f), new Vector3(.10f,1.68f,.14f), art.enamel);
                    Part(marker, "Map case", new Vector3(0,1.41f,.02f), new Vector3(1.56f,.83f,.085f), art.enamel);
                    Part(marker, "Printed panel", new Vector3(0,1.42f,-.032f), new Vector3(1.45f,.71f,.014f), art.linen);
                    Label(marker, "RIDGE LOOP", new Vector3(-.38f,1.65f,-.051f), .085f, art.enamel.color);
                    var knots = new[] { new Vector2(-.58f,1.18f), new Vector2(-.32f,1.43f), new Vector2(-.08f,1.36f),
                        new Vector2(.11f,1.53f), new Vector2(.42f,1.46f), new Vector2(.57f,1.59f) };
                    for (int i = 1; i < knots.Length; i++)
                    {
                        Vector2 vector = knots[i] - knots[i - 1];
                        Vector2 mid = (knots[i] + knots[i - 1]) * .5f;
                        var segment = Part(marker, "Marked hiking route", new Vector3(mid.x,mid.y,-.047f),
                            new Vector3(vector.magnitude,.033f,.015f), art.enamel);
                        segment.localRotation = Quaternion.Euler(0,0,Mathf.Atan2(vector.y, vector.x)*Mathf.Rad2Deg);
                    }
                    Part(marker, "Map shelf", new Vector3(0,.99f,-.17f), new Vector3(1.36f,.055f,.32f), art.metal);
                    return TrailMap(center);
            }
        }

        public Transform CreateAlternate(int venue, Vector3 center)
        {
            switch (venue)
            {
                case 0: return art.Create(0, room, "Window-table coffee", center);
                case 1:
                    var tray = art.Create(1, room, "Servery lunch tray", center);
                    art.Create(2, tray, "Sandwich", center + Vector3.up * .061f);
                    return tray;
                case 2:
                    Cart("Parcel sorting trolley", new Vector3(6.9f,0,3.48f), "OUTBOUND");
                    return art.Create(3, room, "Sorted parcel", center);
                case 3: return art.Create(4, room, "Machine setup spanner", center);
                default:
                    var rack = Group("Refuge trail register", new Vector3(-6.89f,0,-.20f));
                    Part(rack, "Register post", new Vector3(0,.50f,.03f), new Vector3(.12f,1.0f,.12f), art.enamel);
                    Part(rack, "Reading shelf", new Vector3(0,.94f,-.04f), new Vector3(.79f,.055f,.46f), art.linen);
                    Part(rack, "Register back", new Vector3(0,1.10f,.16f), new Vector3(.82f,.35f,.065f), art.enamel);
                    Label(rack, "TRAIL REGISTER", new Vector3(0,1.12f,.113f), .07f, Color.white);
                    return TrailMap(center);
            }
        }

        Transform TrailMap(Vector3 center)
        {
            var map = Group("Folded ridge route map", center);
            for (int i = -1; i <= 1; i++)
            {
                var fold = Part(map, "Map fold", new Vector3(i*.07f,0,0), new Vector3(.071f,.005f,.17f), art.linen);
                fold.localRotation = Quaternion.Euler(0,0,i*5f);
                var stripe = Part(map, "Trail print", new Vector3(i*.07f,.005f,.009f*i), new Vector3(.070f,.004f,.010f), art.enamel);
                stripe.localRotation = Quaternion.Euler(0,-16f,0);
            }
            return map;
        }

        public Vector3 PublicCounter(int venue)
        {
            var counter = Group(new[] { "Coffee pickup ledge", "Family lunch counter", "Collection desk",
                "Service parts desk", "Ridge map table" }[venue], new Vector3(-8f, 0f, -16.1f));
            Part(counter, "Worktop", new Vector3(0,.91f,0), new Vector3(.68f,.065f,1.32f), art.linen);
            foreach (float end in new[] { -.52f, .52f })
                Part(counter, "End support", new Vector3(0,.445f,end), new Vector3(.53f,.87f,.095f), art.metal);
            var body = counter.gameObject.AddComponent<BoxCollider>();
            body.center = new Vector3(0,.46f,0);
            body.size = new Vector3(.67f,.92f,1.31f);
            var spare = CrowdProp(venue, counter, counter.position + new Vector3(-.09f,1f,.44f));
            float floor = spare.GetComponentInChildren<MeshRenderer>().bounds.min.y;
            spare.position += Vector3.up * (.943f - floor);
            return counter.position + new Vector3(.20f,.943f,-.11f);
        }

        public Transform CrowdProp(int venue, Transform parent, Vector3 at)
        {
            switch (venue)
            {
                case 0: return art.Create(0, parent, "Coffee cup", at);
                case 1: return art.Create(1, parent, "Cafeteria tray", at);
                case 2: return art.Create(3, parent, "Parcel", at);
                case 3: return art.Create(4, parent, "Fitting tool", at);
                default:
                    var map = TrailMap(at);
                    map.SetParent(parent, true);
                    return map;
            }
        }

        public Transform DispatchTrolley(Vector3 at)
        {
            var trolley = Group("Loading lane parcel trolley", at);
            foreach (float x in new[] { -.21f, .21f })
            {
                foreach (float z in new[] { .08f, .43f })
                {
                    var wheel = Part(trolley, "Caster", new Vector3(x,.063f,z),
                        new Vector3(.12f,.027f,.12f), art.charcoal, PrimitiveType.Cylinder);
                    wheel.localRotation = Quaternion.Euler(0,0,90);
                }
                Part(trolley, "Handle upright", new Vector3(x,.50f,0f), new Vector3(.033f,.74f,.033f), art.metal);
            }
            Part(trolley, "Parcel platform", new Vector3(0f,.15f,.25f), new Vector3(.50f,.065f,.60f), art.enamel);
            Part(trolley, "Grip", new Vector3(0f,.86f,0f), new Vector3(.46f,.044f,.044f), art.charcoal);
            var parcel = art.Create(3, trolley, "Parcel for loading", at + new Vector3(0f,.30f,.26f));
            var bounds = parcel.GetComponentInChildren<MeshRenderer>().bounds;
            parcel.position += Vector3.up * (at.y + .1825f - bounds.min.y);
            return trolley;
        }

        Transform Cart(string name, Vector3 at, string sign)
        {
            var cart = Group(name, at);
            foreach (float x in new[] { -.77f, .77f })
            foreach (float z in new[] { -.31f, .31f })
            {
                Part(cart, "Tubular upright", new Vector3(x,.51f,z), new Vector3(.045f,.84f,.045f), art.metal);
                var wheel = Part(cart, "Trolley wheel", new Vector3(x,.09f,z), new Vector3(.16f,.032f,.16f), art.charcoal, PrimitiveType.Cylinder);
                wheel.localRotation = Quaternion.Euler(0,0,90);
            }
            Part(cart,"Utility deck",new Vector3(0,.24f,0),new Vector3(1.68f,.055f,.76f),art.metal);
            Part(cart,"Service top",new Vector3(0,.92f,0),new Vector3(1.72f,.065f,.81f),art.linen);
            Part(cart,"Enamel strip",new Vector3(0,.78f,-.37f),new Vector3(1.56f,.20f,.04f),art.enamel);
            Label(cart, sign, new Vector3(0,.78f,-.399f),.093f,Color.white);
            var blocker = cart.gameObject.AddComponent<BoxCollider>();
            blocker.center = new Vector3(0,.48f,0);
            blocker.size = new Vector3(1.70f,.94f,.79f);
            return cart;
        }

        static Transform Part(Transform root, string name, Vector3 position, Vector3 size, Material material,
            PrimitiveType type = PrimitiveType.Cube)
        {
            var part = GameObject.CreatePrimitive(type);
            part.name = name;
            part.transform.SetParent(root, false);
            part.transform.localPosition = position;
            part.transform.localScale = size;
            var collider = part.GetComponent<Collider>();
            collider.enabled = false;
            Object.Destroy(collider);
            part.GetComponent<MeshRenderer>().sharedMaterial = material;
            return part.transform;
        }

        void Label(Transform root, string caption, Vector3 position, float height, Color color)
        {
            var sign = new GameObject(caption);
            sign.transform.SetParent(root, false);
            sign.transform.localPosition = position;
            var text = sign.AddComponent<TextMesh>();
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.fontSize = 64;
            text.characterSize = height * .16f;
            text.color = color;
            text.text = caption;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            sign.GetComponent<MeshRenderer>().sharedMaterial = art.lettering;
            var atlas = sign.AddComponent<WorldSignAtlas>();
            atlas.signs = new[] { text };
            atlas.Refresh();
        }

        Transform Group(string name, Vector3 at)
        {
            var group = new GameObject(name).transform;
            group.SetParent(room, false);
            group.position = at;
            return group;
        }
    }
}
