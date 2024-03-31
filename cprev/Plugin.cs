using BepInEx;
using HarmonyLib;
using Timer;

namespace cprev
{
    [BepInPlugin("com.kirisoup.hff.cprev", PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInDependency("com.plcc.hff.timer", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInProcess("Human.exe")]
    public partial class Plugin : BaseUnityPlugin
    {
        public Harmony harmony;

        public void Start()
        {
            harmony = Harmony.CreateAndPatchAll(typeof(CpRev), "com.kirisoup.hff.cprev");

            if (timerLoaded) harmony.PatchAll(typeof(TimerInteg));

            Cmds.RegCmds();
        }

        [HarmonyPatch(typeof(Game), "EnterCheckpoint")]
        class CpRev
        {
            static bool isCpLarger;
            static int previousCp = 0;

            static void Prefix(ref int checkpoint)
            {
                if (!enabledCPR) return;

                if (Game.currentLevel.nonLinearCheckpoints) return;
                
                // store #cp before entering checkpoint
                previousCp = Game.instance.currentCheckpointNumber;

                // do not execute cprev logic if previousCp is 0 (spawnpoint) or if #cp is not changed
                if (previousCp == 0 || previousCp == checkpoint) return;

                // if triggered cp is smaller than previousCp, modify the level #cp to 0 so that it can be updated to the new smaller #cp
                // - don't worry, the EnterCheckpoint event will override the #cp to newCp
                // I know this is hacky, but this avoids rewritting the entire EnterCheckpoint event
                if (checkpoint < previousCp) Game.instance.currentCheckpointNumber = 0;

                // if triggered cp is larger than previousCp, modify the level #cp to super large so that it can't be updated to the new larger #cp
                // - this new #cp will not be overrided
                // set isCpLarger to tell the postfix method to do its job
                else { isCpLarger = true; Game.instance.currentCheckpointNumber = int.MaxValue; }
            }

            static void Postfix()
            {
                if (!enabledCPR) return;
                if (!isCpLarger) return;

                // if isCpLarger (where level #cp is modified to a super large number), fix the level #cp back to normal :3
                Game.instance.currentCheckpointNumber = previousCp;

                // reset
                isCpLarger = false;
            }
        }

        static class TimerInteg
        {
            [HarmonyPatch(typeof(Speedrun), "SetInvalidType")]
            [HarmonyPrefix]
            // It is too complicated to try and spoof the timer to do the correct cp% checking logic
            // therefore cp% related invalid checks are skipped
            // TODO: Hijack the cp% related invalid type and run my own cp% reversed invalid check
            static bool RemoveCpInvalid(ref InvalidType type)
            {
                if (!enabledCPR) return true;

                return !(type == InvalidType.MissedCheckpoint);
            }
        }

        public void OnDestroy() => harmony.UnpatchSelf();

        static bool enabledCPR = true;
        static readonly bool timerLoaded = BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("com.plcc.hff.timer");
    }
}