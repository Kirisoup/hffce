using System;
using System.Collections.Generic;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using HumanAPI;
using Multiplayer;

namespace oobpercent
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInProcess("Human.exe")]
    public partial class Plugin : BaseUnityPlugin
    {
        public Harmony harmony;

        public void Awake() => harmony = new Harmony(PluginInfo.PLUGIN_GUID);

        public void Start()
        {
            harmony.PatchAll();
            Cmds.RegCmds();
        }

        public void OnDestroy() => harmony.UnpatchSelf();

        // represents whether if the level satisfy the requirements of oob%
        static bool isLevelInvalid = false;

        // universal toggle
        public static bool enabledOOB = true;

        // should the plugin integrate with plcc's timer
        static bool timerInteg = false;

        // Patching! Bellow are where vannila behaviors are altered
        // pay close attention to what is happening here
        // TODO: adopt local - server - client enviroments differently
        static class Modding
        {
            // If the level is valid for oob%, remove the PassZone event entirely
            // - A similar event will be triggeres when the player gets out-of-bound,
            //   to ensure that level passing behaviour is similar to vanilla 
            // - Game.EnterPassZone never execute when playing as a client
            [HarmonyPatch(typeof(Game), "EnterPassZone")]
            static class NoPass
            {
                static bool Prefix()
                {
                    /*DEBUG*/__("@ Game.EnterPassZone()");
                    if (enabledOOB && !isLevelInvalid) return false;
                    return true;
                }
            }

            // This is triggered immediately after a level finished loading
            [HarmonyPatch(typeof(Game), "AfterLoad")]
            static class AfterLoad
            {
                // Store important info about the level for oob% logic to rely on
                static void Postfix()
                {
                    if (!enabledOOB) return;

                    OOB.InitLevelInfo();
                    OOB.InitPlayerInfo();
                }
            }

            // This is triggered immediately when you exit from the level back to the main menu
            [HarmonyPatch(typeof(Game), "AfterUnload")]
            static class AfterUnload
            {
                // Reset storage (to avoid weird af edge-cases that I'm not aware of)
                static void Postfix()
                {
                    /*DEBUG*/__("@ Game.AfterUnload()");
                    if (enabledOOB) OOB.ResetLevelInfo();
                }
            }
        }

        void FixedUpdate()
        {
            // print("FixedUpdate");
            OOB.CheckforPlayerOOB();
        }

        // Debug useful info
        // FIXME: set it to false before releasing

        static bool __(string msg = null)
        {
            bool debugMode = true;

            if (!debugMode)
            return false;

            if (msg is not null) print(msg);
            return true;
        }

        // TODO: Implement GUI
        static class GUI
        {
            static bool LookforTimer()
            {
                // if (/* has timer*/ false) plccTimerInteg = true;
                return timerInteg;
            }
        }

        static partial class OOB
        {
            static Vector3 spawnPos;
            static BoundZone finalBound;
            static Vector4 finalRange;

            static List<BoundZone> boundZoneCandidates = new();
            static List<Player> players = new();

            class BoundZone
            {
                BoxCollider boxCollider;

                Vector2 hCenter;
                Vector2 hSize;
                float hSizeM;
                Vector4 hRange;

                float yCenter;
                float ySize;

                public FallTrigger FallTrigger { get; set; }

                public BoxCollider BoxCollider 
                {
                    get => boxCollider ??= FallTrigger.GetComponent<BoxCollider>(); 
                }

                public Vector2 HCenter
                {
                    get => hCenter != Vector2.zero ? hCenter : (hCenter = HCenterOf(this));
                }

                public Vector2 HSize
                {
                    get => hSize != Vector2.zero ? hSize : (hSize = HSizeOf(this));
                }

                public float HSizeM
                {
                    get => hSizeM != 0 ? hSizeM : (hSizeM = HSize.sqrMagnitude);
                }

                public Vector4 HRange
                {
                    get => hRange != Vector4.zero ? hRange : (hRange = FindRange(this));
                }

                public float YCenter
                {
                    get => yCenter != 0 ? yCenter : (yCenter = YCenterOf(this));
                }

                public float YSize
                {
                    get => ySize != 0 ? ySize : (ySize = YSizeOf(this));
                }
            }

            class Player
            {
                string name;
                Human human;
                SphereCollider collider;
                Vector3 coord;
                Vector3 scale;

                public NetPlayer NetPlayer { get; set; }

                public string Name
                {
                    get => name ??= NetPlayer.name;
                }

                public Human Human
                {
                    get => human ??= NetPlayer.human;
                }

                public SphereCollider Collider
                {
                    get => collider ??= Human.GetComponent<SphereCollider>();
                }

                public Vector3 Coord
                {
                    get => coord != Vector3.zero ? coord : (coord = Collider.bounds.center) ;
                }

                public Vector3 Scale
                {
                    get => (scale != Vector3.zero) ? scale : (scale = Human.transform.localScale * Collider.radius);
                }
            }

            static class BoundCalc
            {
                public static void SelectValid(FallTrigger[] fallTriggers)
                {
                    /*DEBUG*/__("@ SelectValid()");
                    /*DEBUG*/__($"boundBox candidates count: {boundZoneCandidates.Count}");
                    spawnPos = Game.currentLevel.GetCheckpointTransform(0).position;
                    /*DEBUG*/__($"spawnPos.y: {spawnPos.y}");
                    /*DEBUG*/__($"hspawnPos: {new Vector2(spawnPos.x, spawnPos.z)}");

                    foreach (FallTrigger fallTrigger in fallTriggers)
                    {
                        BoundZone zone = new() { FallTrigger = fallTrigger };
                        /*DEBUG*/__($"found {zone} from level");
                        /*DEBUG*/__($"\t- boxCollider: {zone.BoxCollider is not null}");

                        // skip ones without a BoxCollider
                        if (zone.BoxCollider is null) continue;
                        /*DEBUG*/__($"\t{zone} passed test for BoxCollider");

                        /*DEBUG*/__($"\t- hCenter: {zone.HCenter}");
                        /*DEBUG*/__($"\t- hSize: {zone.HSize}");
                        /*DEBUG*/__($"\t- hRange: {zone.HRange}");
                        /*DEBUG*/__($"\t- yCenter: {zone.YCenter}");
                        /*DEBUG*/__($"\t- ySize: {zone.YSize}");

                        // skip ones with their top face not under the Y level of the spawn (select ones below spawn)
                        if (zone.YCenter + zone.YSize/2 >= spawnPos.y) continue;
                        /*DEBUG*/__($"\t{zone} passed test for below spawn");

                        // skip ones with the spawnpoint out of their bound
                        if (IsOutofBound(spawnPos, zone.HRange)) continue; 
                        /*DEBUG*/__($"\t{zone} passed test with spawn in bound");

                        boundZoneCandidates.Add(zone);
                        /*DEBUG*/__($"!!\tAdded {zone} as valid candidate"); 
                    }
                    /*DEBUG*/__($"boundBox candidates count: {boundZoneCandidates.Count}");
                }

                public static void FindLargestRange(List<BoundZone> boundZones)
                {
                    /*DEBUG*/__("@ FindLargestRange()");
                    float maxSize = 0;
                    BoundZone largest = new();

                    if (boundZones.Count > 1)
                    {
                        foreach (BoundZone zone in boundZones)
                        {
                            /*DEBUG*/__($"\tCompairing {zone} with size (sqrMagnitude) {zone.HSizeM} against size {maxSize}");
                            if (zone.HSizeM <= maxSize) continue;

                            maxSize = zone.HSizeM;
                            largest = zone;
                            /*DEBUG*/__($"\tUpdated largest with {largest} of size (sqrMagnitude) {maxSize}");
                        }
                    }
                    else largest = boundZones[0]; // skip compairing if there is only one valid candidate 

                    finalBound = largest;
                    finalRange = largest.HRange;
                    /*DEBUG*/__($"finalRange: {finalRange}");
                }
            }

            public static void InitLevelInfo()
            {
                // Remove previous level info
                ResetLevelInfo();

                // Gets all the FallTriggers inside the current level, that:
                //  - has a BoxCollider
                //  - is below spawn
                //
                //  they are then store inside the fallTriggers list, and
                //  their colliders are stored in boxColliders list.
                BoundCalc.SelectValid(FindObjectsOfType<FallTrigger>());

                // Checks if there are no FallTriggers matching the rule
                if (boundZoneCandidates.Count == 0 || boundZoneCandidates is null)
                {
                    LevelInvalid();
                    return;
                }

                if (LevelInvalid()) return;

                // Gets the largest FallTrigger from the list (in terms of horizontal scale), and store its Index
                BoundCalc.FindLargestRange(boundZoneCandidates);
            }

            public static bool LevelInvalid()
            {
                isLevelInvalid = boundZoneCandidates.Count == 0 || boundZoneCandidates is null;
                if (isLevelInvalid)
                {
                    // call gui methods
                }
                return isLevelInvalid;
            }


            public static void ResetLevelInfo()
            {
                /*DEBUG*/__("@ reset!");
                isLevelInvalid = false;
                spawnPos = Vector3.zero;
                boundZoneCandidates = new();
                finalRange = Vector4.zero;
            }

            public static void InitPlayerInfo()
            {
                // improve robustness
                ResetPlayerInfo();
                
                // if (!NetGame.isClient)
                foreach (NetPlayer netPlayer in NetGame.instance.players)
                {
                    players.Add(new() { NetPlayer = netPlayer });
                    /*DEBUG*/__("Found player: " + netPlayer.name);
                }
            }

            public static void ResetPlayerInfo() => players = new();

            public static void CheckforPlayerOOB()
            {
                // print("CheckforPlayerOOB()");
                foreach (Player player in players)
                {
                    // if (IsOutofBound(player.Coord, finalRange))
                    // {
                    //     // yepee
                    // }

                    // int layerMask = 1 << finalBound.BoxCollider.gameObject.layer;

                    // Vector3 position = player.Human.transform.position;
                    // Physics.Raycast(position, Vector3.down, out RaycastHit hit, Mathf.Infinity, layerMask);
                    // print(hit.collider);
                    // if (hit.collider.transform == finalBound.BoxCollider);

                }
            }

            static Vector2 HSizeOf(BoundZone boundZone) => new(
                (float)((double)boundZone.BoxCollider.size.x * (double)boundZone.FallTrigger.transform.lossyScale.x),
                boundZone.BoxCollider.size.z * boundZone.FallTrigger.transform.lossyScale.z
            );

            static float YSizeOf(BoundZone boundZone) => boundZone.BoxCollider.size.y * boundZone.FallTrigger.transform.localScale.y;

            static Vector2 HCenterOf(BoundZone boundZone) => new(boundZone.BoxCollider.bounds.center.x, boundZone.BoxCollider.bounds.center.z);

            static float YCenterOf(BoundZone boundZone) => boundZone.BoxCollider.bounds.center.y;

            static Vector4 FindRange(BoundZone boundZone)
            {
                Vector2 cntr = boundZone.HCenter;
                Vector2 hsize = boundZone.HSize;
                float halfX = hsize.x/2;
                float halfY = hsize.y/2;

                return new Vector4(cntr.x - halfX, cntr.y - halfY, cntr.x + halfX, cntr.y + halfY);
            }

            static bool IsOutofBound(Vector2 coord2, Vector4 range) => range.x >= coord2.x || range.y >= coord2.y || coord2.x >= range.z || coord2.y >= range.w;

            static bool IsOutofBound(Vector3 coord3, Vector4 range) => range.x >= coord3.x || range.y >= coord3.z || coord3.x >= range.z || coord3.z >= range.w;
        }
    }
}