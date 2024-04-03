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
        Harmony harmony;

        void Start()
        {
            harmony = Harmony.CreateAndPatchAll(typeof(CpRev), "com.kirisoup.hff.cprev");

            if (timerLoaded) harmony.PatchAll(typeof(TimerInteg));

            Cmds.RegCmds();
        }

        void OnDestroy() => harmony.UnpatchSelf();

        void OnGUI()
        {
            if (!cpMissed) return;

            if (timerLoaded && Timer.Timer.timerOpened) { if(!Speedrun.invalid) TimerInvalidWarning("Missed Checkpoint"); }

            else FallbackInvalidWarning("(cp%rev) Missed Checkpoint");
        }

        static void TimerInvalidWarning(string warning) => InvalidWarning(warning, new(300f, 80f));
        static void FallbackInvalidWarning(string warning) => InvalidWarning(warning, new(20f, 20f));

        static void InvalidWarning(string warning, Vector2 pos)
        {
            GUI.Label(new Rect(pos.x, pos.y, Screen.width - 20, Screen.height - 20),
                "Invalid：" + warning,
                new() { fontSize = 30, normal = { textColor = Color.red } });
        }


        static class CpRev
        {
            [HarmonyPatch(typeof(Game), "EnterCheckpoint")]
            [HarmonyPrefix]
            static bool EnterCheckpoint(ref int checkpoint)
            {
                if (!enabledCPR) return true;

                if (Game.currentLevel.nonLinearCheckpoints) return true;
                
                // store #cp before entering checkpoint
                int prevcp = Game.instance.currentCheckpointNumber;

                // do not execute cprev logic if #cp is not changed
                if (prevcp == checkpoint) return true;

                // if triggered cp is larger than prevcp, and prevcp is not 0 (i.e. is not spawnpoint), prevent cp from loading
                else if (checkpoint > prevcp && prevcp != 0) return false;

                // if triggered cp is smaller than prevcp, modify the level #cp to 0 so that it can be updated to the new smaller #cp
                // - don't worry, the EnterCheckpoint event will override the #cp to newCp
                // this avoids rewritting the entire EnterCheckpoint event
                Game.instance.currentCheckpointNumber = 0;

                // validate if checkpoints are skipped
                CPValidation(prevcp, checkpoint);

                return true;
            }

            [HarmonyPatch(typeof(Game), "Fall")]
            [HarmonyPrefix]
            static void Fall()
            {
                // validate if checkpoints are skipped when finished a level
                if (Game.instance.passedLevel) CPValidation(Game.instance.currentCheckpointNumber);
            }

            // reset cpMissed state after exiting to menu (singleplayer)
            [HarmonyPatch(typeof(Game), "AfterUnload")]
            [HarmonyPostfix]
            static void AfterUnload() => cpMissed = false;

            // reset cpMissed state after exiting to lobby (multiplayer)
            [HarmonyPatch(typeof(NetGame), "ServerLoadLobby")]
            [HarmonyPostfix]
            static void ServerLoadLobby() => cpMissed = false;
        }

        public static void CPValidation(int prevcp, int? newcp = null)
        {
            // validation on finished level
            if (newcp is null && prevcp != 1) goto invalid;

            WorkshopItemSource lvltype = Game.instance.currentLevelType;
            int lvlnum = Game.instance.currentLevelNumber;

            bool buitin = lvltype == WorkshopItemSource.BuiltIn;
            bool editorpick = lvltype == WorkshopItemSource.EditorPick;

            bool dark = buitin && lvlnum == 9;
            bool powerplant = buitin && lvlnum == 7;

            // normal case: new cp should be one checkpoint before previous cp
            if (newcp == prevcp - 1) return;

            // cp6 in dark is skipped
            // cp10~7 are just alternate route to cp6~2, both routes are optional in normal cp%, and in cprev cp10~7 are unobtainable
            // i.e. the checkpoint after cp11 is cp5
            if (dark && prevcp == 11 && newcp == 5) return;
            
            // if starting from spawnpoint
            if (prevcp == 0)
            {
                // normal case: the first cp to trigger should be the last cp
                if (newcp == Game.currentLevel.checkpoints.Length - 1) return;

                // cp14~11 are skipped in powerplant, as they are skipped in normal cp% anyway
                // i.e. the first cp to hit in powerplant is cp10
                if (powerplant && newcp == 10) return;

                // cp24~21 are skipped in dark (they are just different combinations of batteries and cable, and are unobtainable in cprev)
                // i.e. the first cp to hit in dark is cp20
                if (dark && newcp == 20) return;
            }

            invalid: cpMissed = true;
        }

        static class TimerInteg
        {
            [HarmonyPatch(typeof(Speedrun), "SetInvalidType")]
            [HarmonyPrefix]
            // removed plcc timer's MissedCheckpoint warning, instead implement my own
            static bool RemoveCpInvalid(ref InvalidType type) => !enabledCPR || type != InvalidType.MissedCheckpoint;
        }

        public static bool enabledCPR = true;
        static bool cpMissed;
        static readonly bool timerLoaded = BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("com.plcc.hff.timer");
    }
}