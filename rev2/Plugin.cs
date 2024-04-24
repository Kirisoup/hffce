using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using BepInEx;
using HarmonyLib;
using HumanAPI;
using Multiplayer;
using UnityEngine;

namespace rev2 {

        [BepInPlugin("com.kirisoup.hff.rev2", PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
        // [BepInDependency("com.plcc.hff.timer", BepInDependency.DependencyFlags.SoftDependency)]
        [BepInProcess("Human.exe")]
        public partial class Plugin : BaseUnityPlugin
        {
                readonly Harmony harmony = new("com.kirisoup.hff.rev2");

                public static bool Enabled = true;

                public void Awake()
                {
                        harmony.PatchAll(typeof(Plugin));
                        Commands.RegCmds();
                }

                public void OnDestroy() => harmony.UnpatchSelf();

                static LevelPassTrigger[] ptrgs;
                static LevelPassTrigger ptrg;

                [HarmonyPatch(typeof(Game), "AfterLoad"), HarmonyPrefix]
                static void PreAfterLoad()
                {
                        if (!Enabled) return;

                        ptrgs = FindObjectsOfType<LevelPassTrigger>();

                        if (ptrgs is null || ptrgs.Length == 0) return;

                        var fincp = Game.currentLevel.GetCheckpointTransform(Game.currentLevel.checkpoints.Length - 1).position;

                        ptrg = GetClosest(ptrgs, fincp);

                        var ptrgBounds = ptrg.gameObject.GetComponent<Collider>().bounds;
                        
                        FindObjectOfType<Level>().checkpoints[0].position = ptrgBounds.center + ptrgBounds.extents.y * Vector3.up;
                }

                static LevelPassTrigger GetClosest(LevelPassTrigger[] ptrgs, Vector3 fincp) => ptrgs.MinBy(ptrg => Vector3.Distance(ptrg.transform.position, fincp));
        }
}


