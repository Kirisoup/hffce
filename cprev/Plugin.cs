using BepInEx;
using HarmonyLib;
using HumanAPI;
using Multiplayer;
using Timer;
using UnityEngine;

namespace cprev
{
        [BepInPlugin("com.kirisoup.hff.cprev", PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
        [BepInDependency("com.plcc.hff.timer", BepInDependency.DependencyFlags.SoftDependency)]
        [BepInProcess("Human.exe")]
        public partial class Plugin : BaseUnityPlugin
        {
                readonly Harmony harmony = new("com.kirisoup.hff.cprev");

                public void Awake() => harmony.PatchAll(typeof(Plugin));

                public void OnDestroy() => harmony.UnpatchSelf();

                public void Start()
                {
                        // for it to be used as a dependency of other plugins
                        if (!qolFeatures) return;

                        Commands.RegCmds();

                        if (timerLoaded) harmony.PatchAll(typeof(TimerInteg));
                }

                public void OnGUI()
                {
                        if (!enabledCPR || !qolFeatures) return;

                        if (!missedCp) return;

                        if (!timerLoaded || !Timer.Timer.timerOpened) BuiltInvalid("Missed Checkpoint");
                        
                        else if (!Speedrun.invalid) TimerInvalid("Missed Checkpoint");
                }

                static void TimerInvalid(string warning) => InvalidWarning(warning, new(300f, 80f));
                static void BuiltInvalid(string warning) => InvalidWarning(warning, new(20f, 20f));

                static void InvalidWarning(string warning, Vector2 pos) => GUI.Label(
                        new Rect(pos.x, pos.y, Screen.width - 20, Screen.height - 20),
                        "Invalid：" + warning,
                        new() { fontSize = 30, normal = { textColor = Color.red } });

                [HarmonyPatch(typeof(Game), "EnterCheckpoint"), HarmonyPrefix]
                static bool EnterCpReversed(ref int checkpoint)
                {
                        if (!enabledCPR) return true;

                        if (Game.currentLevel.nonLinearCheckpoints) return true;
                        
                        // store #cp before entering checkpoint
                        int prev = Game.instance.currentCheckpointNumber;

                        // do not execute cprev logic if #cp is not changed
                        if (prev == checkpoint) return true;

                        // if triggered cp is larger than prevcp, and prevcp is not 0 (i.e. is not spawnpoint), prevent cp from loading
                        else if (checkpoint > prev && prev != 0) return false;

                        // if triggered cp is smaller than prevcp, modify the level #cp to 0 so that it can be updated to the new smaller #cp
                        // - don't worry, the EnterCheckpoint event will override the #cp to newCp
                        // this avoids rewritting the entire EnterCheckpoint event
                        Game.instance.currentCheckpointNumber = 0;

                        // validate if checkpoints are skipped
                        missedCp = CheckInvalid(prev, checkpoint);

                        return true;
                }

                [HarmonyPatch(typeof(Game), "Fall"), HarmonyPrefix]
                static void PassValidate()
                {
                        // validate if checkpoints are skipped when finished a level
                        if (Game.instance.passedLevel) missedCp = CheckInvalid(Game.instance.currentCheckpointNumber);
                }

                // reset cpMissed state after exiting to menu (singleplayer)
                [HarmonyPatch(typeof(Game), "AfterUnload"), HarmonyPostfix]
                static void ResetInvalidSP() => missedCp = false;

                // reset cpMissed state after exiting to lobby (multiplayer)
                [HarmonyPatch(typeof(NetGame), "ServerLoadLobby"), HarmonyPostfix]
                static void ResetInvalidMP() => missedCp = false;

                public static bool CheckInvalid(int prevcp, int? newcp = null)
                {
                        // validation for finishing level
                        if (newcp is null) return prevcp != 1;

                        WorkshopItemSource lvltype = Game.instance.currentLevelType;
                        int lvl = Game.instance.currentLevelNumber;

                        bool buitin = lvltype == WorkshopItemSource.BuiltIn;
                        bool extra = lvltype == WorkshopItemSource.EditorPick;

                        bool dark = buitin && lvl == 9;
                        bool powerplant = buitin && lvl == 7;

                        // normal case: new cp should be one checkpoint before previous cp
                        if (newcp == prevcp - 1) return false;

                        // cp6 in dark is skipped
                        // cp10~7 are just alternate route to cp6~2, both routes are optional in normal cp%, and in cprev cp10~7 are unobtainable
                        // i.e. the checkpoint after cp11 is cp5
                        if (dark && prevcp == 11 && newcp == 5) return false;
                        
                        // if starting from spawnpoint
                        if (prevcp == 0)
                        {
                                // normal case: the first cp to trigger should be the last cp
                                if (newcp == Game.currentLevel.checkpoints.Length - 1) return false;

                                // cp14~11 are skipped in powerplant, as they are skipped in normal cp% anyway
                                // i.e. the first cp to hit in powerplant is cp10
                                if (powerplant && newcp == 10) return false;

                                // cp24~21 are skipped in dark (they are just different combinations of batteries and cable, and are unobtainable in cprev)
                                // i.e. the first cp to hit in dark is cp20
                                if (dark && newcp == 20) return false;
                        }

                        return true;
                }

                static class TimerInteg
                {
                        // removed plcc timer's MissedCheckpoint warning, instead implement my own
                        [HarmonyPatch(typeof(Speedrun), "SetInvalidType"), HarmonyPrefix]
                        static bool RemoveCpInvalid(ref InvalidType type) => !enabledCPR || type != InvalidType.MissedCheckpoint;
                }

                public static bool enabledCPR = true;
                public static bool qolFeatures = false;
                static bool missedCp;
                static readonly bool timerLoaded = BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("com.plcc.hff.timer");
        }
}