using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace First180.Practice
{
    [RequireComponent(typeof(CharacterController))]
    public class VRInteractionManager : MonoBehaviour
    {
        public Camera head;
        public Transform leftHand,rightHand;
        public float moveSpeed=1.8f;
        public static bool Active=>XRSettings.isDeviceActive;
        bool snapReady=true,oldTriggerL,oldTriggerR,oldGripL,oldGripR,oldTeleport,oldPanel,oldMenu;
        int heldHand=-1;float gravity;
        TreatmentTarget[] targets;TreatmentManager care;PracticeRoom room;
        LineRenderer leftRay,rightRay;
        Material rayMaterial;
        SpatialPanel panelUI;
        readonly SpatialButton[] hovered = new SpatialButton[2];
        void Start()
        {
            room=FindFirstObjectByType<PracticeRoom>();care=room.GetComponent<TreatmentManager>();
            panelUI=room.GetComponent<SpatialPanel>();
            targets=FindObjectsByType<TreatmentTarget>(FindObjectsSortMode.None);
            rayMaterial = new Material(Resources.Load<Shader>("PracticePanelSurface")) {color = new Color(.2f,.85f,.8f)};
            leftRay=Ray(leftHand);rightRay=Ray(rightHand);
            var subsystems=new List<XRInputSubsystem>();SubsystemManager.GetSubsystems(subsystems);
            foreach(var sub in subsystems)sub.TrySetTrackingOriginMode(TrackingOriginModeFlags.Floor);
        }
        void OnDestroy() { if (rayMaterial) Destroy(rayMaterial); }
        LineRenderer Ray(Transform hand)
        {
            var line=hand.gameObject.AddComponent<LineRenderer>();line.positionCount=2;line.startWidth=.007f;line.endWidth=.003f;
            line.sharedMaterial=rayMaterial;line.startColor=line.endColor=new Color(.2f,.85f,.8f);return line;
        }
        static void Pose(XRNode node,Transform t)
        {
            var d=InputDevices.GetDeviceAtXRNode(node);
            if(d.TryGetFeatureValue(CommonUsages.devicePosition,out var p))t.localPosition=p;
            if(d.TryGetFeatureValue(CommonUsages.deviceRotation,out var q))t.localRotation=q;
        }
        void Update()
        {
            leftHand.gameObject.SetActive(Active);rightHand.gameObject.SetActive(Active);
            if(!Active || !room || PracticeSession.Get().loading || PracticeSession.Get().testMode)return;
            Pose(XRNode.Head,head.transform);Pose(XRNode.LeftHand,leftHand);Pose(XRNode.RightHand,rightHand);
            var left=InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);var right=InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            right.TryGetFeatureValue(CommonUsages.secondaryButton,out bool panel);
            if(panel && !oldPanel)panelUI?.Recenter();oldPanel=panel;
            left.TryGetFeatureValue(CommonUsages.menuButton,out bool menu);
            if(menu && !oldMenu){room.TogglePause();panelUI?.RefreshPanel(true);panelUI?.Recenter();}oldMenu=menu;
            left.TryGetFeatureValue(CommonUsages.primaryButton,out bool teleport);
            if (PanelOnly)
            {
                Hand(left,leftHand,leftRay,0,ref oldTriggerL,ref oldGripL,true);
                Hand(right,rightHand,rightRay,1,ref oldTriggerR,ref oldGripR,true);
                oldTeleport=teleport;
                return;
            }
            var cc=GetComponent<CharacterController>();cc.height=Mathf.Clamp(head.transform.localPosition.y,1,2.2f);cc.center=new Vector3(head.transform.localPosition.x,cc.height/2,head.transform.localPosition.z);
            left.TryGetFeatureValue(CommonUsages.primary2DAxis,out Vector2 move);right.TryGetFeatureValue(CommonUsages.primary2DAxis,out Vector2 turn);
            Vector3 forward=Vector3.ProjectOnPlane(head.transform.forward,Vector3.up).normalized;
            gravity=cc.isGrounded?-1:Mathf.Max(-20,gravity+Physics.gravity.y*Time.deltaTime);
            cc.Move((Vector3.ClampMagnitude(forward*move.y+Vector3.Cross(Vector3.up,forward)*move.x,1)*moveSpeed+Vector3.up*gravity)*Time.deltaTime);
            if(Mathf.Abs(turn.x)<.3f)snapReady=true;
            if(snapReady && Mathf.Abs(turn.x)>.7f){transform.RotateAround(head.transform.position,Vector3.up,Mathf.Sign(turn.x)*30);snapReady=false;}
            Hand(left,leftHand,leftRay,0,ref oldTriggerL,ref oldGripL,false);
            Hand(right,rightHand,rightRay,1,ref oldTriggerR,ref oldGripR,PanelOnly);
            if(!PanelOnly && teleport && !oldTeleport && Physics.Raycast(leftHand.position,leftHand.forward,out var hit,8,~0,QueryTriggerInteraction.Ignore) && hit.normal.y>.9f && hit.point.y<.5f)
            {
                var p=hit.point+Vector3.up*.1f;
                if(!Physics.CheckCapsule(p+Vector3.up*.35f,p+Vector3.up*1.6f,.23f,~0,QueryTriggerInteraction.Ignore))
                {cc.enabled=false;transform.position+=p-new Vector3(head.transform.position.x,transform.position.y,head.transform.position.z);cc.enabled=true;}
            }
            oldTeleport=teleport;
        }
        bool PanelOnly => room.Paused || room.result!=null || PracticeSession.Get().dashboard;

        void Hand(InputDevice device,Transform hand,LineRenderer line,int index,ref bool oldTrigger,ref bool oldGrip,bool panelOnly)
        {
            device.TryGetFeatureValue(CommonUsages.triggerButton,out bool trigger);
            device.TryGetFeatureValue(CommonUsages.gripButton,out bool grip);
            bool found=Physics.Raycast(hand.position,hand.forward,out var hit,12,~0,QueryTriggerInteraction.Collide);
            line.SetPosition(0,hand.position);
            line.SetPosition(1,found?hit.point:hand.position+hand.forward*6);
            ApplyHand(index, hand, found?hit.collider:null, hit.distance, trigger, trigger&&!oldTrigger, grip, grip&&!oldGrip, panelOnly);
            oldTrigger=trigger;
            oldGrip=grip;
        }

        // Both controllers use the same ownership, panel selection and world interaction path.
        internal void ApplyHand(int index,Transform hand,Collider hit,float hitDistance,bool trigger,bool triggerDown,bool grip,bool gripDown,bool panelOnly)
        {
            if (PracticeSession.Get().loading) return;
            bool holding=care && care.held && heldHand==index;
            if (holding && !grip)
            {
                care.Release();
                heldHand=-1;
                holding=false;
            }
            var button=hit ? hit.GetComponentInParent<SpatialButton>() : null;
            if (hovered[index]!=button)
            {
                if (hovered[index]) hovered[index].SetHover(index,false);
                hovered[index]=button;
                if (button) button.SetHover(index,true);
            }
            if (button)
            {
                if (holding && !panelOnly) care.Use(null,false,0);
                if (triggerDown) { button.Invoke(); panelUI?.RefreshPanel(true); }
                return;
            }
            if (panelUI && panelUI.OwnsSurface(hit))
            {
                if (holding && !panelOnly) care.Use(null,false,0);
                return;
            }
            if (panelOnly) return;
            if (holding)
            {
                TreatmentTarget nearest=null;
                float best=.30f*.30f;
                foreach (var target in targets)
                {
                    if (!target) continue;
                    float squared=(care.held.transform.position-target.transform.position).sqrMagnitude;
                    if (squared<best){best=squared;nearest=target;}
                }
                care.Use(nearest,trigger,Time.deltaTime);
                return;
            }
            if (!hit) return;
            if (gripDown && hitDistance<2.5f && care && !care.held)
            {
                var tool=hit.GetComponentInParent<TreatmentTool>();
                if (tool && !tool.Held)
                {
                    care.Grab(tool,hand);
                    if(care.held==tool) heldHand=index;
                    return;
                }
            }
            if (!triggerDown) return;
            var portal=hit.GetComponentInParent<PracticePortal>();
            if (portal) portal.Use();
            else if (hit.GetComponentInParent<PracticePatient>() is PracticePatient patient) room.Inspect(patient.patientId);
        }
    }
}
