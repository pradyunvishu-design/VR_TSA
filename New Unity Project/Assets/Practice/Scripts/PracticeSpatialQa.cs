#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace First180.Practice
{
    // Uses the live board, room, tools and the same button/grip dispatch as the XR controllers.
    public static class PracticeSpatialQa
    {
        public static void CheckCase(PracticeRoom room,Action<bool,string> check)
        {
            var rig=Object.FindFirstObjectByType<VRInteractionManager>();
            var panel=room.GetComponent<SpatialPanel>();
            var care=room.GetComponent<TreatmentManager>();
            var tools=Object.FindObjectsByType<TreatmentTool>(FindObjectsSortMode.None);
            var first=tools[0];
            var firstCollider=first.GetComponentInChildren<Collider>();
            var secondCollider=tools[1].GetComponentInChildren<Collider>();

            rig.ApplyHand(0,rig.leftHand,firstCollider,1,false,false,true,true,false);
            rig.ApplyHand(1,rig.rightHand,secondCollider,1,false,false,true,true,false);
            check(care.held==first && first.transform.parent==rig.leftHand,"Opposite grip keeps the original hand and equipment");
            rig.ApplyHand(0,rig.leftHand,null,0,false,false,false,false,false);
            check(!care.held && !first.Held,"Original hand can return its tool after the other hand tries to grab");
            for (int index=0;index<2;index++)
            {
                var hand=index==0?rig.leftHand:rig.rightHand;
                rig.ApplyHand(index,hand,firstCollider,1,false,false,true,true,false);
                room.TogglePause();
                panel.RefreshPanel(true);
                var surface=panel.GetComponentsInChildren<Collider>().FirstOrDefault(c=>c.name=="Panel backing");
                check(surface && surface.enabled && surface.isTrigger && panel.OwnsSurface(surface),"Opaque pause board intercepts hand rays and permits walking");
                var resume=Button(panel,"Resume");
                check(resume && resume.Available,"Pause has a selectable spatial Resume button");
                if(resume) rig.ApplyHand(index,hand,resume.GetComponent<Collider>(),1,true,true,true,false,true);
                check(!room.Paused && care.held==first,"Paused hand "+index+" selects Resume while still gripping equipment");
                if(room.Paused) room.TogglePause();
                rig.ApplyHand(index,hand,null,0,false,false,false,false,false);
                rig.ApplyHand(index,hand,firstCollider,1,false,false,true,true,true);
                check(!care.held,"Modal spatial input blocks world pickup for hand "+index);
            }
            room.BeginEmergency();
            var audio=room.GetComponents<AudioSource>();
            check(audio.Where(source=>source.clip).All(source=>
                    !UnityEditor.AssetDatabase.GetAssetPath(source.clip).Contains("/PracticeVoice/")),
                  "ROOK instruction dispatch does not assign a voice clip");
            check(audio.Any(source=>source.clip && source.loop && source.clip.name=="Procedural breathing") &&
                  audio.Any(source=>source.clip && source.loop &&
                    UnityEditor.AssetDatabase.GetAssetPath(source.clip).Contains("/LocationSound/")),
                  "Breathing and room ambience remain configured during caption-only guidance");
            panel.RefreshPanel(true);
            var prioritize=Button(panel,"Prioritize inspected patient");
            check(prioritize && !prioritize.Available,"Spatial triage requires inspection before prioritization");
            for (int slot=0;slot<3;slot++)
            {
                var inspect=Button(panel,"Inspect patient "+(char)('A'+slot));
                if(inspect) rig.ApplyHand(1,rig.rightHand,inspect.GetComponent<Collider>(),1,true,true,false,false,false);
                check(inspect && room.inspected==room.PatientInSlot(slot) && prioritize && prioritize.Available,
                      "Spatial inspection selects the displayed A/B/C slot "+slot);
                string expected="PATIENT "+(char)('A'+slot)+"  /  INSPECTED";
                var labels=panel.GetComponentsInChildren<Text>();
                check(room.PatientPanelIdentity==expected && labels.Any(label=>label.text==expected)
                      && room.observation.Length>0 && labels.Any(label=>label.text.Contains(room.observation)),
                      "Desktop and spatial patient records agree with inspected person and findings: "+(char)('A'+slot));
            }
            room.inspected=-1;
            room.observation="";
            panel.RefreshPanel(VRInteractionManager.Active);
        }

        public static void CheckDashboard(PracticeRoom room,Action<bool,string> check)
        {
            var session=PracticeSession.Get();
            var panel=room.GetComponent<SpatialPanel>();
            var rig=Object.FindFirstObjectByType<VRInteractionManager>();
            panel.RefreshPanel(true);
            for(int page=0;page<session.results.Count;page++)
            {
                var next=Button(panel,page==0?"Review first case":"Review next case");
                check(next,"Spatial analytics page navigation "+(page+1));
                if(!next) break;
                rig.ApplyHand(0,rig.leftHand,next.GetComponent<Collider>(),1,true,true,false,false,true);
                var logo=panel.GetComponentsInChildren<RawImage>().FirstOrDefault(image=>image.name=="PULSE");
                check(logo && logo.texture && logo.texture.name=="PulseWordmark" && !logo.raycastTarget,
                      "PULSE wordmark is visible on the spatial results board and passes through pointer input");
                var labels=panel.GetComponentsInChildren<Text>();
                var score=session.results[page];
                check(labels.Any(t=>t.text==ScenarioCatalog.All[score.scenario].title)
                      && labels.Any(t=>t.text.Contains("Diagnosis") && t.text.Contains(score.quality+"%") && t.text.Contains("First response")),
                      "Spatial analytics page reports the recorded case and metrics for "+score.scenario);
                check(labels.All(t=>t.rectTransform.rect.width>0 && t.resizeTextForBestFit),
                      "Spatial page has bounded, fit-to-width text "+(page+1));
            }
            var summary=Button(panel,"Back to summary");
            if(summary) rig.ApplyHand(1,rig.rightHand,summary.GetComponent<Collider>(),1,true,true,false,false,true);
            check(summary && Button(panel,"Review first case"),"Right-hand trigger returns from the last case to the spatial summary");
            panel.RefreshPanel(VRInteractionManager.Active);
        }

        static SpatialButton Button(SpatialPanel panel,string label) =>
            panel.GetComponentsInChildren<SpatialButton>().FirstOrDefault(button=>button.name==label);
    }
}
#endif
