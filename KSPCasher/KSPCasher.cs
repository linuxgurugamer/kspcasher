using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;

namespace KSPCasher
{
    public class KSPCasherData : ScenarioModule
    {
        public override void OnSave(ConfigNode node)
        {
            base.OnSave(node);

            ConfigNode n = new ConfigNode("KSPCasher");
            n.SetValue("LastBudget", KSPCasher.instance.LastBudget.ToString());
            node.AddNode(n);
        }

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);

            var n = node.GetNode("KSPCasher");
            string lb = n.GetValue("LastBudget");
            if (n != null && lb != null)
                KSPCasher.instance.LastBudget = double.Parse(lb);
            
        }
    }

    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class CasherFlight : KSPCasher
    {    
    }

    [KSPAddon(KSPAddon.Startup.SpaceCentre, false)]
    public class CasherSC : KSPCasher
    {
    }

    [KSPAddon(KSPAddon.Startup.TrackingStation, false)]
    public class CasherTS : KSPCasher
    {
    }

    [KSPAddon(KSPAddon.Startup.EditorAny, false)]
    public class CasherEditor : KSPCasher
    {
    }


    public class KSPCasher : MonoBehaviour
    {
        public static KSPCasher instance;
        internal KSPCasher()
        {
            instance = this;
        }

        public void TechDisableEvent()
        {
            Log("Giving back " +giveBack.ToString() + " sci");
            ResearchAndDevelopment.Instance.AddScience(giveBack,TransactionReasons.None);
            giveBack = 0;
            foreach (RDTech item in relock)
            {
                ProtoTechNode protoNode = ResearchAndDevelopment.Instance.GetTechState(item.techID);
                protoNode.state = RDTech.State.Unavailable;
                ResearchAndDevelopment.Instance.SetTechState(item.techID, protoNode);
            }
            relock.Clear();
            Save();
        }

        private void Save()
        {
            GamePersistence.SaveGame("persistent", HighLogic.SaveFolder, SaveMode.OVERWRITE);
        }

        static IEnumerable<ConfigNode> techConfigs;

        public void Start()
        {

            GameEvents.OnTechnologyResearched.Add(TechUnlockEvent);
            GameEvents.onGUIRnDComplexDespawn.Add(TechDisableEvent);

            var game = HighLogic.CurrentGame;
            ProtoScenarioModule psm = game.scenarios.Find(s => s.moduleName == typeof(KSPCasherData).Name);
            if (psm == null)
            {
                psm = game.AddProtoScenarioModule(typeof(KSPCasherData), GameScenes.SPACECENTER);
            }
        }

        public void OnDestroy()
        {
            GameEvents.OnTechnologyResearched.Remove(TechUnlockEvent);
            GameEvents.onGUIRnDComplexDespawn.Remove(TechDisableEvent);
        }

        public double LastBudget = 0;
        public List<string> BudgetsDone = new List<string>();

        public void Update()
        {
            double time = Planetarium.GetUniversalTime();
            double since = time - LastBudget;
            if(since > 21600) {
                Log("Doing budget " + time.ToString());
                LastBudget = time;

                double budget = (Reputation.Instance.reputation * 10);
                if (budget > 0)
                {
                    ScreenMessages.PostScreenMessage("[Casher] Budget Receieved: " + budget.ToString("C"));
                    Funding.Instance.AddFunds(budget, TransactionReasons.None);
                }else
                {
                    ScreenMessages.PostScreenMessage("[Casher] No Budget Receieved, you need to work on your reputation.");
                }

                double bonus = (double)ResearchAndDevelopment.Instance.Science * 10000;
                if(bonus > 0)
                {
                    ScreenMessages.PostScreenMessage("[Casher] Science Bonus: " + bonus.ToString("C"));
                    Funding.Instance.AddFunds(bonus, TransactionReasons.None);
                    //And take the science
                    ResearchAndDevelopment.Instance.AddScience(-ResearchAndDevelopment.Instance.Science, TransactionReasons.None);
                }                
            }
        }

        private void Log(string msg)
        {
            //Debug.Log("[Casher] " + msg);
        }
        int giveBack = 0;
        List<RDTech> relock = new List<RDTech>();
        bool skip = false;
        public void TechUnlockEvent(GameEvents.HostTargetAction<RDTech, RDTech.OperationResult> ev) {
            if (skip)
            {
                skip = false;
                return;
            }
            bool canAfford = false;

            int cost = ev.host.scienceCost * 10000;

            if (Funding.Instance.Funds >= cost) canAfford = true;
            
            if(ev.target == RDTech.OperationResult.Successful)
            {
                //Always give the science back
                giveBack += ev.host.scienceCost;
            }

            if (canAfford)
            {
                //Take the funds
                Funding.Instance.AddFunds((double)-cost, TransactionReasons.RnDTechResearch);
                Log("Taking " + cost.ToString() + " funds");
                if (ev.target != RDTech.OperationResult.Successful)
                {
                    skip = true;
                    ev.host.UnlockTech(true);
                    ProtoTechNode protoNode = ResearchAndDevelopment.Instance.GetTechState(ev.host.techID);
                    protoNode.state = RDTech.State.Available;
                    ResearchAndDevelopment.Instance.SetTechState(ev.host.techID, protoNode);                                        
                }                    
            }else
            {
                Log("Cannot afford " + cost.ToString() + " funds");
                ScreenMessages.PostScreenMessage("[Casher] You cannot afford this node, it costs " + cost.ToString("C"));
                if (ev.target == RDTech.OperationResult.Successful)
                {
                    relock.Add(ev.host);
                }                    
            }
        }
    }
}
