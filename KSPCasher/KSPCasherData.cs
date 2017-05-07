using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KSPPluginFramework;

using UnityEngine;
using KSP.UI.Screens;


namespace KSPCasher
{
    [KSPScenario(ScenarioCreationOptions.AddToNewCareerGames | ScenarioCreationOptions.AddToExistingCareerGames, GameScenes.SPACECENTER)]
    public class KSPCasherData : ScenarioModule
    {

        public override void OnSave(ConfigNode node)
        {
            ConfigNode n = new ConfigNode("KSPCasher");
            n.AddValue("LastBudget", KSPCasher.LastBudget);
            n.AddValue("Multiplier", KSPCasher.BudgetMultiplier.ToString());
            n.AddValue("SciBuy", KSPCasher.ScienceBuyMultiplier.ToString());
            n.AddValue("SciSell", KSPCasher.ScienceSellMultiplier.ToString());
            node.AddNode(n);
            base.OnSave(node);
        }

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);

            var n = node.GetNode("KSPCasher");

            if (n != null)
            {
                string param = n.GetValue("LastBudget");
                if (param != null)
                    KSPCasher.LastBudget = param;

                param = n.GetValue("Multiplier");
                if (param != null)
                    KSPCasher.BudgetMultiplier = double.Parse(param);

                param = n.GetValue("SciBuy");
                if (param != null)
                    KSPCasher.ScienceBuyMultiplier = double.Parse(param);

                param = n.GetValue("SciSell");
                if (param != null)
                    KSPCasher.ScienceSellMultiplier = double.Parse(param);
            }
        }

    }


}
