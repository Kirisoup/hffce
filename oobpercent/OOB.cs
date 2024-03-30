using System.Collections.Generic;
using UnityEngine;
using HumanAPI;
using Multiplayer;
using Timer;

namespace oobpercent
{
    public partial class Plugin
    {
        static class OOB
        {
            public static void InitLevelInfo()
            {
                // Remove previous level info
                ResetLevelInfo();

                FallTrigger[] triggers = FindObjectsOfType<FallTrigger>();

                if (isLevelInvalid = triggers.Length == 0) return;

                // Gets all the FallTriggers inside the current level, that:
                //  - has a BoxCollider
                //  - is below spawn
                BoundZoneLogic.SelectValid(triggers);

                // Checks if there are no valid candidates
                if (isLevelInvalid = boundZoneCandidates.Count == 0) return;

                // store the largest FallTrigger from the list (in terms of horizontal scale)
                finalBound = BoundZoneLogic.FindLargest(boundZoneCandidates);

                // store its horizontal range
                finalRange = finalBound.HRange;

                // clear the unused candidates
                boundZoneCandidates = new();

                // Check if there is any passzone and remove passzones from the level
                // if a scene has no passzone, when players get oob they'll simple get respawned
                // - so that scenes that shouldn't be finished in the first place, can't be finished (e.g. multiplayer lobby -- you can still get oob tho :) )
                if (isPasszoneFound = PasszoneLogic.FindPasszones()) PasszoneLogic.RemovePasszones();

                // this is here to reset Player.collideRange
                InitPlayerInfo();
            }

            public static void ResetLevelInfo()
            {
                /*DEBUG*/__("@ reset!");
                isLevelInvalid = false;
                isPasszoneFound = false;
                spawnPos = Vector3.zero;
                boundZoneCandidates = new();
                finalRange = Vector4.zero;
                passed = false;
            }

            public static void InitPlayerInfo()
            {
                // clear previous player info
                // - this would bring extra steps regenerating information about already existing players, but is more robust
                ResetPlayerInfo();

                foreach (NetPlayer netPlayer in NetGame.instance.players)
                players.Add(new() { NetPlayer = netPlayer });

                /*DEBUG*/__($"Found {players.Count} players");
            }

            public static void ResetPlayerInfo() => players = new();

            static class BoundZoneLogic
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

                public static BoundZone FindLargest(List<BoundZone> boundZones)
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

                    /*DEBUG*/__($"finalRange: {finalRange}");
                    return largest;
                }
            }

            static class PasszoneLogic
            {
                public static bool FindPasszones()
                {
                    foreach (LevelPassTrigger Passzone in FindObjectsOfType<LevelPassTrigger>())
                    Passzones.Add(Passzone);

                    /*DEBUG*/__($"Passzones.Count: {Passzones.Count}");
                    return Passzones.Count > 0;
                }

                public static void RemovePasszones()
                {
                    foreach (LevelPassTrigger PassTrigger in Passzones)
                    Destroy(PassTrigger);
                }
            }

            public static class PlayerLogic
            {
                public static void CheckforPlayerOOB()
                {
                    if (isLevelInvalid) return;

                    foreach (Player player in players)
                    {
                        Vector3 coord = player.Collider.bounds.center;

                        if (IsOutofBound(coord, player.CollideRange)) OnOutofBound(player);
                    }
                }

                static void OnOutofBound(Player player)
                {
                    // prevent calling EnterPassZone() multiple times
                    // to prevent stalling the coroutine task Game.LevelLoadedServer in multiplayer
                    if (passed) return;
                    
                    // finish level only if the level contains any Passzones in the first place
                    if (isPasszoneFound)
                    {
                        Game.instance.EnterPassZone();
                        passed = true;
                    }

                    // the player will be respawed even if there is no passzone present in the level
                    Game.instance.Fall(player.Human);
                } 
            }

            // stores every information needed to find the appropriate bound
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

            // stores information about every player which is needed to check for oob
            // player position (coord) is not stored as it is updated each fixed time interval
            class Player
            {
                string name;
                Human human;
                SphereCollider collider;
                float radius;
                Vector4 collideRange;

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

                public float Radius
                {
                    get => (radius != 0) ? radius : (radius = Human.transform.localScale.x * Collider.radius);
                }

                public Vector4 CollideRange
                {
                    get {
                        if (collideRange != Vector4.zero) return collideRange;

                        if (finalRange != Vector4.zero) return collideRange = FindCollideRange(this, finalRange);
        
                        // If finalRange is not found, do not store collideRange of the player
                        return FindCollideRange(this, finalRange);
                    }
                }
            }

            static Vector2 HSizeOf(BoundZone boundZone) => new(boundZone.BoxCollider.bounds.size.x, boundZone.BoxCollider.bounds.size.z);

            static float YSizeOf(BoundZone boundZone) => boundZone.BoxCollider.bounds.size.y;

            static Vector2 HCenterOf(BoundZone boundZone) => new(boundZone.BoxCollider.bounds.center.x, boundZone.BoxCollider.bounds.center.z);

            static float YCenterOf(BoundZone boundZone) => boundZone.BoxCollider.bounds.center.y;

            static Vector4 FindRange(BoundZone boundZone)
            {
                Vector2 cntr = boundZone.HCenter;
                float exx = boundZone.HSize.x/2;
                float exz = boundZone.HSize.y/2;

                return new Vector4(cntr.x - exx, cntr.y - exz, cntr.x + exx, cntr.y + exz);
            }

            static Vector4 FindCollideRange(Player player, Vector4 range) => new(range.x + player.Radius, range.y + player.Radius, range.z - player.Radius, range.w - player.Radius);

            static bool IsOutofBound(Vector2 coord2, Vector4 range) => range.x >= coord2.x || range.y >= coord2.y || coord2.x >= range.z || coord2.y >= range.w;

            static bool IsOutofBound(Vector3 coord3, Vector4 range) => range.x >= coord3.x || range.y >= coord3.z || coord3.x >= range.z || coord3.z >= range.w;

            static Vector3 spawnPos;

            static BoundZone finalBound;

            static Vector4 finalRange;

            static List<BoundZone> boundZoneCandidates = new();

            static List<LevelPassTrigger> Passzones = new();

            static List<Player> players = new();

            public static bool passed;
        }
    }
}
