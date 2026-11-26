using UnityEngine;

namespace First180.Practice
{
    [RequireComponent(typeof(CharacterController))]
    public class PracticePlayer : MonoBehaviour
    {
        public Camera view;
        public float speed=4.2f;
        public Vector3 spawn;
        float vertical, pitch;
        bool ownsCursor;
        public bool MouseLookActive => ownsCursor && Cursor.lockState==CursorLockMode.Locked;
        public string targetLabel="";
        void Start() { spawn=transform.position; }
        void SetMouseLook(bool active)
        {
            if (!active && !ownsCursor) return;
            ownsCursor=active;
            Cursor.lockState=active ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible=!active;
        }
        void OnDisable() { SetMouseLook(false); }
        void OnApplicationFocus(bool focused) { if (!focused) SetMouseLook(false); }
        void Update()
        {
            if(VRInteractionManager.Active || PracticeSession.Get().testMode) { SetMouseLook(false); return; }
            var room=FindFirstObjectByType<PracticeRoom>();
            if(room==null || room.Paused || room.result!=null || PracticeSession.Get().dashboard || PracticeSession.Get().loading)
            {
                targetLabel="";
                SetMouseLook(false);
                return;
            }
            if (!Application.isFocused) return;
            if (ownsCursor && !MouseLookActive) SetMouseLook(false);
            bool toggle=Input.GetKeyDown(KeyCode.Tab);
            if (toggle) SetMouseLook(!MouseLookActive);
            bool strafeDrag=Input.GetMouseButton(2) && (MouseLookActive || !room.PointerOverUI(Input.mousePosition));
            float mouseX=Input.GetAxisRaw("Mouse X");
            float turn=(Input.GetKey(KeyCode.RightArrow)?1:0)-(Input.GetKey(KeyCode.LeftArrow)?1:0);
            transform.Rotate(0,turn*90f*Time.deltaTime,0);
            if(!toggle && !strafeDrag && (MouseLookActive || Input.GetMouseButton(1)))
            {
                transform.Rotate(0,mouseX*2.1f,0);
                pitch=Mathf.Clamp(pitch-Input.GetAxisRaw("Mouse Y")*2.1f,-65,70);
                view.transform.localRotation=Quaternion.Euler(pitch,0,0);
            }
            Vector3 move=new Vector3((Input.GetKey(KeyCode.D)?1:0)-(Input.GetKey(KeyCode.A)?1:0),0,
                (Input.GetKey(KeyCode.W)||Input.GetKey(KeyCode.UpArrow)?1:0)-(Input.GetKey(KeyCode.S)||Input.GetKey(KeyCode.DownArrow)?1:0));
            var controller=GetComponent<CharacterController>();
            vertical=controller.isGrounded?-2f:Mathf.Max(-30,vertical-20*Time.deltaTime);
            Vector3 displacement=(transform.TransformDirection(Vector3.ClampMagnitude(move,1))*speed+Vector3.up*vertical)*Time.deltaTime;
            if (strafeDrag && !toggle) displacement+=transform.right*(mouseX*.08f);
            controller.Move(displacement);
            if(transform.position.y < -8) { controller.enabled=false;transform.position=spawn;controller.enabled=true; }
            targetLabel="";
            bool click=Input.GetMouseButtonDown(0) && (MouseLookActive || !room.PointerOverUI(Input.mousePosition));
            Ray ray=click && !MouseLookActive?view.ScreenPointToRay(Input.mousePosition):new Ray(view.transform.position,view.transform.forward);
            bool clickedScenery=click && !MouseLookActive;
            if(Physics.Raycast(ray,out RaycastHit hit,50f,~0,QueryTriggerInteraction.Collide))
            {
                var care=room.GetComponent<TreatmentManager>();
                if(care)
                {
                    if(Input.GetKeyDown(KeyCode.Q))care.Release();
                    var tool=hit.collider.GetComponentInParent<TreatmentTool>();
                    if(tool && hit.distance<3 && !care.held)
                    {
                        targetLabel = "E  Pick up " + tool.name;
                        if(Input.GetKeyDown(KeyCode.E)) care.Grab(tool,view.transform);
                    }
                    care.Use(hit.distance<2.8f?hit.collider.GetComponentInParent<TreatmentTarget>():null,Input.GetKey(KeyCode.F),Time.deltaTime);
                }
                var button=hit.collider.GetComponentInParent<SpatialButton>();
                if(button && (click || Input.GetKeyDown(KeyCode.E)))button.Invoke();
                var portal=hit.collider.GetComponentInParent<PracticePortal>();
                if(portal!=null)
                {
                    targetLabel="E / click: "+portal.label;
                    if(click || Input.GetKeyDown(KeyCode.E)) portal.Use();
                }
                var patient=hit.collider.GetComponentInParent<PracticePatient>();
                if (button || portal || patient || hit.collider.GetComponentInParent<TreatmentTool>() || hit.collider.GetComponentInParent<TreatmentTarget>())
                    clickedScenery=false;
                if(patient!=null)
                {
                    if (care && care.held && hit.distance < 2.8f)
                        targetLabel = "Hold F  Use " + care.held.name;
                    else targetLabel="E / click  Inspect patient";
                    if(click || Input.GetKeyDown(KeyCode.E)) room.Inspect(patient.patientId);
                }
            }
            else
            {
                var care=room.GetComponent<TreatmentManager>();if(care){care.Use(null,Input.GetKey(KeyCode.F),Time.deltaTime);if(Input.GetKeyDown(KeyCode.Q))care.Release();}
            }
            if (clickedScenery) SetMouseLook(true);
        }
    }
}
