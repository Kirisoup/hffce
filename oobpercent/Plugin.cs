using BepInEx;
using HarmonyLib;
using UnityEngine;
using Timer;
using Multiplayer;

namespace oobpercent
{
    [BepInPlugin("com.kirisoup.hff.oobpercent", PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInDependency("com.plcc.hff.timer", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInProcess("Human.exe")]
    public partial class Plugin : BaseUnityPlugin
    {
        public Harmony harmony;

        public void Start()
        {
            harmony = Harmony.CreateAndPatchAll(typeof(InfoUpdate), "com.kirisoup.hff.oobpercent");

            Cmds.RegCmds();

            // only patch timer if it is found
            if (timerLoaded) harmony.PatchAll(typeof(TimerInteg));
        }

        public void OnDestroy() => harmony.UnpatchSelf();

        // Where main logic is executed
        public void FixedUpdate()
        {
            if (enabledOOB) OOB.PlayerLogic.CheckforPlayerOOB();
        }

        static class TimerInteg
        {
            [HarmonyPatch(typeof(Speedrun), "OnGUI")]
            [HarmonyPostfix]
            static void OnGUI()
            {
                if (Speedrun.invalid) return;

                if (isCheated) InvalidWarning("(oob%) Toggled. Restart level to take effect");

                else if (isLevelInvalid) InvalidWarning("(oob%) level doesn't satisfy the requirements");
            }

            static void InvalidWarning(string warning)
            {
                GUI.Label(new Rect(300f, 80f, Screen.width - 20, Screen.height - 20),
                    "Invalid：" + warning,
                    new() { fontSize = 30, normal = { textColor = Color.red } });
            }
        }

        public void OnGUI()
        {
            // only add my own gui if plcc timer is not present
            if (timerLoaded) return;

            if (isCheated) InvalidWarning("(oob%) Toggled. Restart level to take effect");

            else if (isLevelInvalid) InvalidWarning("(oob%) level doesn't satisfy the requirements");
        }

        static void InvalidWarning(string warning)
        {
            GUI.Label(new Rect(20f, 20f, Screen.width - 20, Screen.height - 20),
                "Invalid：" + warning,
                new() { fontSize = 30, normal = { textColor = Color.red } });
        }

        static class InfoUpdate
        {
            // This is triggered immediately after a level finished loading
            // Store important info about the level for oob% logic to rely on
            [HarmonyPatch(typeof(Game), "AfterLoad")]
            [HarmonyPostfix]
            static void AfterLoad()
            {
                if (enabledOOB) OOB.InitLevelInfo();
            }

            // This is triggered immediately when you exit from the level back to the main menu
            // Reset storage
            [HarmonyPatch(typeof(Game), "AfterUnload")]
            [HarmonyPostfix]
            static void AfterUnload()
            {
                OOB.ResetLevelInfo();
            }

            [HarmonyPatch(typeof(App), "OnClientCountChanged")]
            [HarmonyPostfix]
            static void OnClientCountChanged()
            {
                if (enabledOOB) OOB.InitPlayerInfo();
            }
        }

        // Debug useful info
        // FIXME: set it to false before releasing
        static bool __(string msg = null)
        {
            bool debugMode = false;

            if (debugMode && msg is not null) print(msg);

            return debugMode;
        }

        // represents whether if the level satisfy the requirements of oob%
        public static bool isLevelInvalid;

        // represents whether if the player has toggled oob% during a run
        public static bool isCheated;

        // represents whether is there any passzone found
        public static bool isPasszoneFound;

        // universal toggle
        public static bool enabledOOB = true;

        // is plcc timer found
        static readonly bool timerLoaded = BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("com.plcc.hff.timer");
    }
}