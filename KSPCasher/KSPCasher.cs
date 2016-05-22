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
    public class KSPCasherData : ScenarioModule
    {
        public override void OnSave(ConfigNode node)
        {
            base.OnSave(node);

            ConfigNode n = new ConfigNode("KSPCasher");
            n.SetValue("LastBudget", KSPCasher.instance.LastBudget);
            n.SetValue("Multiplier", KSPCasher.BudgetMultiplier.ToString());
            n.SetValue("SciBuy", KSPCasher.ScienceBuyMultiplier.ToString());
            n.SetValue("SciSell", KSPCasher.ScienceSellMultiplier.ToString());
            node.AddNode(n);
        }

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);

            var n = node.GetNode("KSPCasher");

            if (n != null)
            {
                string param = n.GetValue("LastBudget");
                if(param != null)
                    KSPCasher.instance.LastBudget = param;

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
        ApplicationLauncherButton ToolbarButton;

        public static double BudgetMultiplier = 10;
        public static double ScienceBuyMultiplier = 10000;
        public static double ScienceSellMultiplier = 10000;

        private bool CasherDebug = false;

        internal KSPCasher()
        {
            instance = this;            
        }

        public void Start()
        {

            GameEvents.OnTechnologyResearched.Add(TechUnlockEvent);
            GameEvents.onGUIRnDComplexDespawn.Add(TechDisableEvent);
            GameEvents.onGUIRnDComplexSpawn.Add(HideGUI);
            GameEvents.onGUIApplicationLauncherReady.Add(OnGUIApplicationLauncherReady);

            var game = HighLogic.CurrentGame;
            ProtoScenarioModule psm = game.scenarios.Find(s => s.moduleName == typeof(KSPCasherData).Name);
            if (psm == null)
            {
                psm = game.AddProtoScenarioModule(typeof(KSPCasherData), GameScenes.SPACECENTER);
            }
        }

        public void OnDisable()
        {
            GameEvents.OnTechnologyResearched.Remove(TechUnlockEvent);
            GameEvents.onGUIRnDComplexDespawn.Remove(TechDisableEvent);
            GameEvents.onGUIRnDComplexSpawn.Remove(HideGUI);
            GameEvents.onGUIApplicationLauncherReady.Remove(OnGUIApplicationLauncherReady);
            ApplicationLauncher.Instance.RemoveModApplication(ToolbarButton);
            GameEvents.onGUIApplicationLauncherReady.Remove(OnGUIApplicationLauncherReady);
        }

        #region GUI
        private bool stylesSetup = false;
        private Rect windowPos = new Rect(580f, 40f, 1f, 1f);
        private bool ShowSettings = false;
        private bool ShowGUI = false;
        private static GUIStyle headerText;
        private static GUIStyle bigHeaderText;
        private static GUIStyle normalText;

        private void SetupStyles()
        {
            stylesSetup = true;

            headerText = new GUIStyle(GUI.skin.label);
            headerText.normal.textColor = Color.white;
            headerText.fontStyle = FontStyle.Bold;
            headerText.alignment = TextAnchor.MiddleLeft;

            bigHeaderText = new GUIStyle(GUI.skin.label);
            bigHeaderText.normal.textColor = Color.white;
            bigHeaderText.fontSize = 18;
            bigHeaderText.fontStyle = FontStyle.Bold;
            bigHeaderText.alignment = TextAnchor.MiddleCenter;

            normalText = new GUIStyle(GUI.skin.label);
            normalText.normal.textColor = Color.white;
            normalText.fontStyle = FontStyle.Normal;
            normalText.alignment = TextAnchor.MiddleLeft;
        }

        private void OnGUIApplicationLauncherReady()
        {
            if (ToolbarButton == null)
            {
                ToolbarButton = ApplicationLauncher.Instance.AddModApplication(GUISwitch, GUISwitch, null, null, null, null, ApplicationLauncher.AppScenes.SPACECENTER,
                                                                                GameDatabase.Instance.GetTexture("KSPCasher/Icon", false));
            }
        }

        public void HideGUI()
        {
            ShowGUI = false;
        }

        public void GUISwitch()
        {
            if (ShowGUI == false)
            {
                ShowGUI = true;
            }
            else
            {
                ShowGUI = !ShowGUI;
            }
        }
        
        //OnDraw Shows the MainGUI Window
        private void OnGUI()
        {
            GUI.skin.window.richText = true;
            if (ShowGUI == true)
            {
                if (!stylesSetup)
                {
                    SetupStyles();
                }

                GUI.skin = HighLogic.Skin;

                windowPos.xMin = Screen.width - 336 - 14;
                windowPos.yMin = Screen.height - windowPos.height - 40f;
                windowPos.yMax = Screen.height - 40f;
                windowPos = GUILayout.Window(
                    typeof(KSPCasher).FullName.GetHashCode(),
                    windowPos,
                    MainGUI,
                    "KSP-Casher");

                GUI.depth = 0;
            }
        }

        //MainGUI Window Content
        private void MainGUI(int WindowID)
        {
            double budget = (Reputation.Instance.reputation * BudgetMultiplier);
            double bonus = (ResearchAndDevelopment.Instance.Science * ScienceSellMultiplier);

            double time = Planetarium.GetUniversalTime();
            KSPDateTime dt = new KSPDateTime(time);
            KSPDateTime next = dt.AddDays(1);
            next = next.AddHours(4-next.Hour);
            next = next.AddMinutes(-next.Minute);
            next = next.AddSeconds(-next.Second);

            KSPDateTime span = next.Subtract(dt);
            GUILayout.BeginVertical(GUILayout.Width(300), GUILayout.ExpandWidth(false));

            if (ShowSettings)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Budget Multiplier", headerText, GUILayout.Width(150));
                string text = GUILayout.TextField(BudgetMultiplier.ToString());
                int temp = 0;
                if (int.TryParse(text, out temp))
                {
                    BudgetMultiplier = Mathf.Clamp(temp,0,1000000);
                }
                else if (text == "") BudgetMultiplier = 10;
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Science Bonus", headerText, GUILayout.Width(150));
                text = GUILayout.TextField(ScienceSellMultiplier.ToString());
                temp = 0;
                if (int.TryParse(text, out temp))
                {
                    ScienceSellMultiplier = Mathf.Clamp(temp, 0, 1000000);
                }
                else if (text == "") ScienceSellMultiplier = 10000;
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Tech Multiplier", headerText, GUILayout.Width(150));
                text = GUILayout.TextField(ScienceBuyMultiplier.ToString());
                temp = 0;
                if (int.TryParse(text, out temp))
                {
                    ScienceBuyMultiplier = Mathf.Clamp(temp, 0, 1000000);
                }
                else if (text == "") ScienceBuyMultiplier = 10000;
                GUILayout.EndHorizontal();

                if (GUILayout.Button("< Back"))
                {
                    ShowSettings = false;
                }
            }
            else
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Next Budget", headerText, GUILayout.Width(100));
                GUILayout.Label(budget.ToString("C"), normalText);
                if (GUILayout.Button("$", GUILayout.Width(20), GUILayout.ExpandHeight(false)))
                {
                    ShowSettings = true;
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Due In", headerText, GUILayout.Width(100));
                GUILayout.Label(span.ToString("H\\h m\\m s\\s"), normalText);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Science Bonus", headerText, GUILayout.Width(100));
                GUILayout.Label(bonus.ToString("C"), normalText);
                GUILayout.EndHorizontal();

                GUILayout.Label("Cash out science", bigHeaderText);

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("1"))
                {
                    CashOutScience(1);
                }
                if (GUILayout.Button("10"))
                {
                    CashOutScience(10);
                }
                if (GUILayout.Button("100"))
                {
                    CashOutScience(100);
                }
                GUILayout.EndHorizontal();

                if (GUILayout.Button("All"))
                {
                    CashOutScience(ResearchAndDevelopment.Instance.Science);
                }
            }

            GUILayout.EndVertical();

        }

        #endregion



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

        public string LastBudget = "";        

        public void Update()
        {
            if (BudgetMultiplier <= 0 || BudgetMultiplier > 1000000) return; //overflow protection

            double time = Planetarium.GetUniversalTime();
            KSPDateTime dt = new KSPDateTime(time);
            if (TimeWarp.CurrentRate < 1001 && dt.Hour < 4) return; //We do budgets at 4am (so you can warp to next morning for it)
            string budgetCode = dt.Year.ToString() + dt.Month.ToString() + dt.Day.ToString();
            
            if(budgetCode != LastBudget) {
                Log("Doing budget " + budgetCode);
                LastBudget = budgetCode;
                if (CasherDebug)
                    ScreenMessages.PostScreenMessage("[Casher] " + budgetCode);
                double budget = (Reputation.Instance.reputation * BudgetMultiplier);
                if (budget > 0)
                {
                    ScreenMessages.PostScreenMessage("[Casher] Budget received: " + budget.ToString("C"));
                    Funding.Instance.AddFunds(budget, TransactionReasons.None);
                }else
                {
                    ScreenMessages.PostScreenMessage("[Casher] No budget received, you need to work on your reputation.");
                }         
            }
        }

        private void CashOutScience(float amt)
        {
            if (ResearchAndDevelopment.Instance.Science < amt) amt = ResearchAndDevelopment.Instance.Science;
            double bonus = (double)amt * ScienceSellMultiplier;
            if (bonus > 0)
            {
                ScreenMessages.PostScreenMessage("[Casher] Science Bonus: " + bonus.ToString("C"));
                Funding.Instance.AddFunds(bonus, TransactionReasons.None);
                //And take the science
                ResearchAndDevelopment.Instance.AddScience(-amt, TransactionReasons.None);
            }
        }

        private void Log(string msg)
        {
            if(CasherDebug)
                Debug.Log("[Casher] " + msg);
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
