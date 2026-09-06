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
        void Start()
        {
            room=FindFirstObjectByType<PracticeRoom>();care=room.GetComponent<TreatmentManager>();
            targets=FindObjectsByType<TreatmentTarget>(FindObjectsSortMode.None);
            leftRay=Ray(leftHand);rightRay=Ray(rightHand);
            var subsystems=new List<XRInputSubsystem>();SubsystemManager.GetSubsystems(subsystems);
            foreach(var sub in subsystems)sub.TrySetTrackingOriginMode(TrackingOriginModeFlags.Floor);
        }
        LineRenderer Ray(Transform hand)
        {
            var line=hand.gameObject.AddComponent<LineRenderer>();line.positionCount=2;line.startWidth=.007f;line.endWidth=.003f;
            line.material=new Material(Shader.Find("Sprites/Default"));line.startColor=line.endColor=new Color(.2f,.85f,.8f);return line;
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
            if(panel && !oldPanel)FindFirstObjectByType<SpatialPanel>()?.Recenter();oldPanel=panel;
            left.TryGetFeatureValue(CommonUsages.menuButton,out bool menu);
            if(menu && !oldMenu){room.TogglePause();FindFirstObjectByType<SpatialPanel>()?.Recenter();}oldMenu=menu;
            if(room.Paused){Hand(right,rightHand,rightRay,1,ref oldTriggerR,ref oldGripR);return;}
            var cc=GetComponent<CharacterController>();cc.height=Mathf.Clamp(head.transform.localPosition.y,1,2.2f);cc.center=new Vector3(head.transform.localPosition.x,cc.height/2,head.transform.localPosition.z);
            left.TryGetFeatureValue(CommonUsages.primary2DAxis,out Vector2 move);right.TryGetFeatureValue(CommonUsages.primary2DAxis,out Vector2 turn);
            Vector3 forward=Vector3.ProjectOnPlane(head.transform.forward,Vector3.up).normalized;
            gravity=cc.isGrounded?-1:Mathf.Max(-20,gravity+Physics.gravity.y*Time.deltaTime);
            cc.Move((Vector3.ClampMagnitude(forward*move.y+Vector3.Cross(Vector3.up,forward)*move.x,1)*moveSpeed+Vector3.up*gravity)*Time.deltaTime);
            if(Mathf.Abs(turn.x)<.3f)snapReady=true;
            if(snapReady && Mathf.Abs(turn.x)>.7f){transform.RotateAround(head.transform.position,Vector3.up,Mathf.Sign(turn.x)*30);snapReady=false;}
            Hand(left,leftHand,leftRay,0,ref oldTriggerL,ref oldGripL);
            Hand(right,rightHand,rightRay,1,ref oldTriggerR,ref oldGripR);
            left.TryGetFeatureValue(CommonUsages.primaryButton,out bool teleport);
            if(teleport && !oldTeleport && Physics.Raycast(leftHand.position,leftHand.forward,out var hit,8) && hit.normal.y>.9f && hit.point.y<.5f)
            {
                var p=hit.point+Vector3.up*.1f;
                if(!Physics.CheckCapsule(p+Vector3.up*.35f,p+Vector3.up*1.6f,.23f,~0,QueryTriggerInteraction.Ignore))
                {cc.enabled=false;transform.position+=p-new Vector3(head.transform.position.x,transform.position.y,head.transform.position.z);cc.enabled=true;}
            }
            oldTeleport=teleport;
        }
        void Hand(InputDevice device,Transform hand,LineRenderer line,int index,ref bool oldTrigger,ref bool oldGrip)
        {
            device.TryGetFeatureValue(CommonUsages.triggerButton,out bool trigger);device.TryGetFeatureValue(CommonUsages.gripButton,out bool grip);
            bool hitSomething=Physics.Raycast(hand.position,hand.forward,out var hit,12,~0,QueryTriggerInteraction.Collide);
            line.SetPosition(0,hand.position);line.SetPosition(1,hitSomething?hit.point:hand.position+hand.forward*6);
            if(care && care.held && heldHand==index)
            {
                TreatmentTarget nearest=null;float distance=.3f;
                foreach(var target in targets)if(target && Vector3.Distance(care.held.transform.position,target.transform.position)<distance){distance=Vector3.Distance(care.held.transform.position,target.transform.position);nearest=target;}
                care.Use(nearest,trigger,Time.deltaTime);
                if(!grip && oldGrip){care.Release();heldHand=-1;}
            }
            else if(hitSomething)
            {
                if(grip && !oldGrip && hit.distance<2.5f && care){var tool=hit.collider.GetComponentInParent<TreatmentTool>();if(tool){care.Grab(tool,hand);heldHand=index;}}
                if(trigger && !oldTrigger)
                {
                    var button=hit.collider.GetComponentInParent<SpatialButton>();if(button)button.Invoke();
                    else if(hit.collider.GetComponentInParent<PracticePortal>() is PracticePortal portal)portal.Use();
                    else if(hit.collider.GetComponentInParent<PracticePatient>() is PracticePatient patient)room.Inspect(patient.patientId);
                }
            }
            oldTrigger=trigger;oldGrip=grip;
        }
        void OnDestroy(){if(leftRay)Destroy(leftRay.material);if(rightRay)Destroy(rightRay.material);}
    }
}
