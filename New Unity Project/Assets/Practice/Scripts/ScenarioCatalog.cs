using System;

namespace First180.Practice
{
    [Serializable]
    public class ScenarioDefinition
    {
        public int id;
        public string title, location, scene, normal, incident, symptoms;
        public string[] actions, instructions;
        public float timeLimit = 90f;
    }

    // Small, editable case data. These are game interactions, not clinical credentials.
    public static class ScenarioCatalog
    {
        public const string Hub = "TrainingHub";
        public static readonly ScenarioDefinition[] All =
        {
            new ScenarioDefinition {
                id=0, title="Anaphylaxis", location="Juniper Cafe", scene="Cafe_Anaphylaxis",
                normal="A customer is enjoying lunch. The cafe is quiet.",
                incident="A customer suddenly struggles to breathe after eating.",
                symptoms="Swollen lips, hives and noisy breathing after a known allergen. The patient has a prescribed auto-injector.",
                actions=new[]{"safety","call","epinephrine","monitor"},
                instructions=new[]{"Check the surroundings, introduce yourself and obtain consent.","Ask a bystander to call 911 and retrieve the patient's prescribed auto-injector.","Assist with the prescribed epinephrine device if trained, following its instructions. Use the outer mid-thigh for this simulated auto-injector.","Stay with the patient and reassess breathing and responsiveness until EMS takes over."}
            },
            new ScenarioDefinition {
                id=1, title="Obstructed Airways", location="Commons Dining Hall", scene="Dining_ObstructedAirways",
                normal="Guests are talking over a meal in the dining hall.",
                incident="A diner stands up, grabs their throat and cannot speak.",
                symptoms="A responsive adult cannot speak, breathe or cough effectively after taking a bite. No allergy signs.",
                actions=new[]{"safety","call","backblows","thrusts","monitor"},
                instructions=new[]{"Check safety and ask if the person is choking. This case is a responsive adult with severe obstruction.","Direct a bystander to call 911 while you begin care.","If trained, give up to five back blows. Stop immediately if the obstruction clears.","For this simulated adult the obstruction remains: give up to five abdominal thrusts. Alternate if needed; stop when clear. Chest thrusts are used when abdominal thrusts are unsuitable.","The object has cleared in this case. Reassess breathing and arrange medical assessment. If the person becomes unresponsive, follow dispatcher guidance and your CPR training."}
            },
            new ScenarioDefinition {
                id=2, title="Traumatic Hemorrhaging", location="Riverside Service Road", scene="Road_Hemorrhaging",
                normal="A delivery van has stopped beside road maintenance barriers.",
                incident="A crash is heard. A worker has a serious arm wound.",
                symptoms="Continuous heavy bleeding from the forearm, pale skin and dizziness. Bleeding continues despite pressure in this scripted case.",
                actions=new[]{"safety","call","pressure","tourniquet","monitor"},
                instructions=new[]{"Confirm traffic is stopped and use a protective barrier before contact.","Delegate a 911 call and ask for the bleeding-control kit. Do not delay urgent pressure.","Apply firm direct pressure to the bleeding wound using a dressing.","In this scenario bleeding persists: apply a commercial tourniquet if trained, above the limb wound and not over a joint. Follow device instructions and record the time.","Keep the patient warm, reassess breathing and bleeding, and hand over to EMS. Do not loosen the tourniquet."}
            },
            new ScenarioDefinition {
                id=3, title="Lost Limb", location="Fabrication Workshop", scene="Workshop_LostLimb",
                normal="A workshop demonstration is underway. Machines are behind guards.",
                incident="Equipment stops with a bang. A worker has an amputated forearm.",
                symptoms="Traumatic forearm amputation with severe bleeding. The machine is isolated. The detached part is nearby.",
                actions=new[]{"safety","call","pressure","tourniquet","preserve","monitor"},
                instructions=new[]{"Confirm the machine is isolated and use protective equipment.","Delegate a 911 call and request a bleeding-control kit.","Start direct pressure on the bleeding wound. Life-threatening bleeding takes priority over retrieving the part.","For persistent life-threatening limb bleeding, apply a commercial tourniquet if trained and note the time.","After bleeding control, wrap the part and seal it in a clean waterproof bag. Cool the bag externally; never put tissue directly on ice or in water. Send it with EMS.","Keep the patient warm and reassess breathing and bleeding until EMS arrives."}
            },
            new ScenarioDefinition {
                id=4, title="Frostbite", location="Alpine Trail Shelter", scene="Trail_Frostbite",
                normal="Hikers arrive at a trail shelter as snow starts to fall.",
                incident="A hiker removes a glove and notices numb, waxy fingers.",
                symptoms="An alert hiker has cold, numb, pale waxy fingers after freezing exposure. No confusion or severe shivering in this case.",
                actions=new[]{"safety","call","shelter","protect","monitor"},
                instructions=new[]{"Check for environmental hazards and more urgent hypothermia signs first.","Arrange medical help through the simulated emergency call.","Move into shelter and replace wet clothing. Avoid further cold exposure.","Protect the affected area with loose dry dressings. Do not rub or apply direct heat. Avoid thawing when refreezing is possible; follow medical guidance.","Continue checking overall warmth, responsiveness and breathing while awaiting help."}
            }
        };

        public static readonly string[] ActionIds = {
            "safety","call","epinephrine","backblows","thrusts","pressure","tourniquet","preserve","shelter","protect","monitor","water","rub","loosen"
        };
        public static readonly string[] ActionLabels = {
            "Check safety / consent / PPE","Delegate emergency call + equipment","Assist with prescribed epinephrine","Give up to five back blows","Give up to five abdominal thrusts","Apply direct pressure","Apply trained-use tourniquet","Bag and cool detached part","Move into shelter / dry clothing","Protect with loose dry dressings","Monitor and hand over to EMS","Offer a drink of water","Rub the cold tissue","Loosen the tourniquet"
        };
        public static string Label(string action)
        {
            int index=Array.IndexOf(ActionIds,action);
            return index>=0 ? ActionLabels[index] : action;
        }
        public static string WhyWrong(string action)
        {
            if(action=="water") return "A drink does not treat these emergencies and may be unsafe when swallowing is impaired.";
            if(action=="rub") return "Rubbing frostbitten tissue can cause further damage.";
            if(action=="loosen") return "Do not loosen an applied bleeding-control tourniquet while waiting for EMS.";
            return "That action is not the next step for this simulated case.";
        }
    }
}
