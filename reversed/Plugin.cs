using System;
using System.Reflection;
using System.Collections.Generic;
using BepInEx;
using HarmonyLib;
using HumanAPI;
using Multiplayer;
using UnityEngine;

namespace reversed
{
    [BepInPlugin("com.kirisoup.hff.reversed", PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInDependency("com.plcc.hff.timer", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInProcess("Human.exe")]
    public partial class Plugin : BaseUnityPlugin
    {
        Harmony harmony;

        void Start()
        {
            harmony = Harmony.CreateAndPatchAll(typeof(Rev), "com.kirisoup.hff.reversed");

            harmony.PatchAll(typeof(Rev.MakeClimb));

            Cmds.RegCmds();
        }

        void OnDestroy() => harmony.UnpatchSelf();

        static void TimerInvalidWarning(string warning) => InvalidWarning(warning, new(300f, 80f));
        static void FallbackInvalidWarning(string warning) => InvalidWarning(warning, new(20f, 20f));

        static void InvalidWarning(string warning, Vector2 pos)
        {
            GUI.Label(new Rect(pos.x, pos.y, Screen.width - 20, Screen.height - 20),
                "Invalid：" + warning,
                new() { fontSize = 30, normal = { textColor = Color.red } });
        }

        static int hoverLayer = 8;
        static int hoverLayerMask;

        static readonly AccessTools.FieldRef<Human, Vector3[]> velocities = AccessTools.FieldRefAccess<Vector3[]>(typeof(Human), "velocities");

        void FixedUpdate()
        {
            // if (Game.instance.currentCheckpointNumber != 0) return;

            foreach (NetPlayer player in NetGame.instance.players)
            {
                Vector3 pos = player.human.GetComponent<SphereCollider>().bounds.center;
                float py = pos.y;
                float vy = player.human.velocity.y;


                if (Physics.Raycast(pos, Vector3.up, levelMaxY, hoverLayerMask))
                {
                    float acc = 0;

                    // stopping

                    float h = Math.Abs(origFinishTopY - py);

                    float tsqr = (float)Math.Pow(7.5, 2);

                    float dtsqr = (float)(h/tsqr);

                    float acc0 = (float)(dtsqr + Math.Sqrt(Math.Abs(Math.Pow(dtsqr, 2) - Math.Pow(vy, 2)/tsqr)));

                    // accent

                    float desiredVy = (float)Math.Sqrt(h * 19.62);

                    float acc1 = Math.Abs(vy)*2;

                    if (vy < -1) acc = acc0;
                    else acc = acc1;
                    // acc = acc0;

                    print($"d {h} vy {vy} desiredVy {desiredVy} vy < desiredVy {vy < desiredVy} acc {acc}");

                    if (vy + acc < desiredVy)

                    foreach (Rigidbody rigidbody in player.human.rigidbodies)
                        rigidbody.velocity += new Vector3(0, acc, 0);
 
                }
            }
        }

        static void ThrowPlayer(string input = null)
        {
            if (!float.TryParse(input, out float height)) height = 100;

            float d = Mathf.Max(0, height - Human.Localplayer.GetComponent<Collider>().bounds.center.y);

            float b = 0.95f;
            float g = Physics.gravity.y;

            // float wtf = (float)((4*Math.Pow(b,2) - 4*g*b + Math.Pow(g,2))/(b-0.5*g));
            float wtf = 1171/50;

            // float desiredVy = (float)Math.Sqrt(-2 * Physics.gravity.y * d + 0.05 * Math.Pow(d, 2));

            float desiredVy = (float)Math.Sqrt(d * wtf);

            // desiredVy * 

            SetHumanVY(desiredVy);

        }

        static void SetHumanVY(float vy)
        {
            Human human = NetGame.instance.players[0].human;

            foreach (var item in Human.Localplayer.rigidbodies)
            {
                item.velocity = new(item.velocity.x, vy, item.velocity.z);                
            }

            for (int i = 0; i < Human.Localplayer.rigidbodies.Length; i++ )
            {
                velocities(human)[i] = new(velocities(human)[i].x, vy, velocities(human)[i].z);
            }
        }

        static class Rev
        {
            static WorkshopItemSource lvltype;
            static int lvlnum;

            static bool redrock;

            static bool onLevelBegin = false;

            [HarmonyPatch(typeof(Game), "AfterLoad")]
            [HarmonyPrefix]
            static void PreAfterLoad()
            {
                if (!enabledRev) return;

                // init a list of endzones
                origFinishes = FindObjectsOfType<LevelPassTrigger>();

                if (origFinishes is null || origFinishes.Length == 0) return;

                // handles edge case where there are multiple endzones found from the level
                origFinish = FindClosestFinish(origFinishes);
                origFinishPos = origFinish.gameObject.GetComponent<Collider>().bounds.center;
                origSpawnPos = FindObjectOfType<Level>().checkpoints[0].position;
                origFinish.gameObject.layer = hoverLayer;

                hoverLayerMask = 1 << hoverLayer;

                ReverseFinish(origSpawnPos);

                ReverseSpawn(origFinishPos, origFinish.gameObject.GetComponent<Collider>().bounds.extents.y);

                ModifyOldFinish(origFinish);

                onLevelBegin = true;

                lvltype = Game.instance.currentLevelType;
                lvlnum = Game.instance.currentLevelNumber;
            }

            [HarmonyPatch(typeof(Game), "AfterLoad")]
            [HarmonyPostfix]
            static void PostAfterLoad() => onLevelBegin = false;

            static bool executingSpawnAt = false;

            [HarmonyPatch(typeof(Human), "SpawnAt", new Type[] { typeof(Vector3) })]
            [HarmonyPrefix]
            static void PreSpawnAt(ref Vector3 pos)
            {
                spawnAtPos = pos;
                executingSpawnAt = true;
            }

            [HarmonyPatch(typeof(Human), "SpawnAt", new Type[] { typeof(Vector3) })]
            [HarmonyPostfix]
            static void PostSpawnAt() => executingSpawnAt = false;

            [HarmonyPatch(typeof(Human), "SetPosition")]
            [HarmonyPrefix]
            static void SetPosition(ref Vector3 spawnPos)
            {
                if (!enabledRev) return;
                if (!executingSpawnAt) return;
                if (Game.instance.currentCheckpointNumber != 0) return;
                if (Game.instance.currentLevelNumber == -1) return;

                redrock = lvltype == WorkshopItemSource.EditorPick && lvlnum == 7;

                bool alwaysSpawnInside = !redrock && Plugin.alwaysSpawnInside;

                if (!onLevelBegin && !alwaysSpawnInside) return;

                float? yCap = FindYCap() - uniRadius/2;

                if (yCap is null) return;

                if (spawnPos.y <= yCap) return;

                spawnPos.y = (float)yCap;

                float vcap = - (spawnPos.y - spawnAtPos.y)/2 - Physics.gravity.y;
                float vori = Human.instance.velocity.y;

                print($"vcap {vcap}");

                foreach (Rigidbody rigidbody in Human.instance.rigidbodies)
                rigidbody.velocity += new Vector3(0, vcap - vori, 0);

                print($"Human.instance.velocity {Human.instance.velocity}");
            }

            [HarmonyPatch(typeof(Human), "FixedUpdate")]
            public static class MakeClimb
            {
                static bool justSpawed;
                static bool keepGrabbing;

                static void Prefix()
                {
                    if (Game.instance.currentCheckpointNumber != 0) return;

                    print($"justSpawed {justSpawed} keepGrabbing {keepGrabbing}");

                    if (Human.instance.state == HumanState.Spawning) justSpawed = true;
                }

                static void Postfix()
                {
                    if (Game.instance.currentCheckpointNumber != 0) return;

                    if (!justSpawed && !keepGrabbing) return;

                    if (Human.instance.hasGrabbed) keepGrabbing = false;

                    if (Human.instance.state == HumanState.Spawning || Human.instance.state == HumanState.Unconscious) return;

                    justSpawed = false;
                    if (!Human.instance.hasGrabbed) keepGrabbing = true;

                    if (keepGrabbing) Human.instance.state = HumanState.Climb;
                }

            }
        }

        static LevelPassTrigger FindClosestFinish(LevelPassTrigger[] origFinishes)
        {
            Transform lastcp = Game.currentLevel.GetCheckpointTransform(Game.currentLevel.checkpoints.Length - 1);

            float closestDist = float.MaxValue;
            LevelPassTrigger closestFinish = null;

            foreach (LevelPassTrigger origFinish in origFinishes)
            {
                float distance = Vector3.Distance(origFinish.transform.position, lastcp.position);

                if (distance >= closestDist) continue;

                closestDist = distance;
                closestFinish = origFinish;
            }

            return closestFinish;
        }

        static void ReverseFinish(Vector3 pos)
        {
            GameObject revFinish = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            revFinish.name = "revfinish";
            revFinish.SetActive(true);
            revFinish.transform.SetParent(Game.currentLevel.gameObject.transform, false);
            revFinish.transform.localScale = Vector3.one * uniRadius *2;
            revFinish.GetComponent<Collider>().isTrigger = true;
            revFinish.AddComponent<LevelPassTrigger>();
            if (debug) Soup.ObjMajic.ShadePureColor(revFinish.GetComponent<Renderer>(), new(0, 1, 0, 0.33f));
            else revFinish.GetComponent<Renderer>().enabled = false;

            Vector3? surfacingPos = Clipped(pos);
            bool overlapped = surfacingPos is not null;

            Vector3? landingPos = Floating(pos);
            bool floating = landingPos is not null;

            // Handles spawnpoint underground or floating in midair: 

            // if upper half of the reversed spawn is clipping inside a collider, set its pos to the highest surface above it
            if (overlapped) revFinish.transform.position = (Vector3)surfacingPos;

            // else, if the reversed spawn is floating (and there is no collider above it), set its pos to the surface 
            else if (floating) revFinish.transform.position = (Vector3)landingPos;

            else revFinish.transform.position = pos;
        }

        static Vector3? Clipped(Vector3 point)
        {
            RaycastHit[] overlapped = FindClipped(point + Vector3.up * uniRadius/2, uniRadius/2);

            // only continue if any colliders overlaps with upper half of the reversed spawn 
            if (overlapped is null) return null;

            if (debug) Soup.ObjMajic.CreateDummySphere("Clipped", uniRadius, point, new Color(1, 0, 0, 0.33f));


            RaycastHit? hit = Soup.XrayHax.RayhitFirstSolid(OverrideY(point, levelMaxY), Vector3.down, float.PositiveInfinity, sels: overlapped);

            // return the highest point from overlapping colliders
            return hit != null ? ((RaycastHit)hit).point : null;
        }

        static RaycastHit[] FindClipped(Vector3 point, float radius = 0)
        {
            RaycastHit[] candidates = Soup.XrayHax.RayhitAllSolid(OverrideY(point, levelMaxY), Vector3.down, float.PositiveInfinity);

            if (debug) Soup.ObjMajic.CreateDummySphere("Clip Check", radius, point, Color.magenta);

            // only continue if any colliders found on the xz coord of the point 
            if (candidates is null || candidates.Length == 0) return null;

            List<RaycastHit> overlapped = new();

            foreach (RaycastHit hit in candidates)
            if (Vector3.Distance(hit.collider.ClosestPoint(point), point) < radius) overlapped.Add(hit);

            return overlapped.ToArray();
        }

        // assumes not overlapped
        static Vector3? Floating(Vector3 point)
        {
            bool noColliderAbove = Soup.XrayHax.RayhitFirstSolid(point + Vector3.up * (uniRadius/2), Vector3.up, float.PositiveInfinity) is null;

            // only continue if no collider found from above
            if (!noColliderAbove) return null;

            RaycastHit? hit = Soup.XrayHax.RayhitFirstSolid(point, Vector3.down, float.PositiveInfinity);
 
            // return first hit position downwards (null if nothing found)
            return hit != null ? ((RaycastHit)hit).point : null;
        }

        static void ModifyOldFinish(LevelPassTrigger origFinish)
        {
            if (origFinish is null || !origFinish.enabled) return;

            origFinish.gameObject.GetComponent<LevelPassTrigger>().enabled = false;

            Collider[] colliders = origFinish.gameObject.GetComponents<Collider>();

            RaycastHit? hit = Soup.XrayHax.RayhitFirstSolid(origFinishPos, Vector3.down, float.PositiveInfinity);

            bool overlappedBellow = hit is not null;

            // foreach (Collider collider in colliders)
            // {
            //     if (!collider.enabled) continue;
                
            //     collider.isTrigger = false;

            //     // if (overlappedBellow)
            //     // collider.enabled=false;
                
            //     // Bounds bounds = collider.bounds;

            //     // collider.transform.localScale = new Vector3(bounds.size.x + uniRadius, bounds.size.y + uniRadius, bounds.size.z + uniRadius);
            // }
        }

        static void ReverseSpawn(Vector3 finpos, float finhight)
        {
            Vector3 revSpawnPos = new(finpos.x, finpos.y + finhight, finpos.z);

            origFinishTopY = finpos.y + finhight;

            FindObjectOfType<Level>().checkpoints[0].position = revSpawnPos;
        }

        static float? FindYCap()
        {
            if (debug)
            {
                Soup.ObjMajic.CreateDummySphere("dropStart", safeZone, spawnAtPos + Vector3.up * dropDist, new(0, 1, 1, 0.5f));

                Soup.ObjMajic.CreateDummySphere("revspawn", 0.5f, spawnAtPos, new(1, 1, 0, 1f));

                Soup.ObjMajic.DrawLine(OverrideY(spawnAtPos, levelMaxY), spawnAtPos, Color.black);
            }

            // Ray above
            Vector3 rayAStart = OverrideY(spawnAtPos, levelMaxY);
            Vector3 rayAEnds = spawnAtPos + Vector3.up * (dropDist + safeZone);

            RaycastHit[] hitsA = Soup.XrayHax.RayhitAllSolid(rayAStart, rayAEnds);

            if (hitsA is null || hitsA.Length == 0)
            {
                if (debug) Soup.ObjMajic.DrawLine(rayAStart, rayAEnds, Color.yellow, Vector3.forward * 0.1f);
                return null;
            }

            if (debug)
            {
                Soup.ObjMajic.DrawBlockedLine(rayAStart, hitsA[0].point, rayAEnds, Color.yellow, Vector3.forward * 0.1f);

                foreach (RaycastHit hit in hitsA) Soup.ObjMajic.ShadePureColor(hit.transform.gameObject.GetComponent<Renderer>(), new Color(1, 0, 0.5f, 0.75f));
            }

            // ray ignore
            Vector3 rayIStart = spawnAtPos + Vector3.up * (dropDist + safeZone);
            Vector3 rayIEnds = spawnAtPos + Vector3.up * safeZone;

            RaycastHit[] hitsI = Soup.XrayHax.RayhitAllSolid(rayIStart, rayIEnds);

            if (hitsI is null || hitsI.Length == 0)
            {
                if (debug) Soup.ObjMajic.DrawLine(rayIStart, rayIEnds, new(1, 0, 0.5f), Vector3.forward * 0.2f);
                hitsI = new RaycastHit[0];
            }

            else if (debug)
            {
                Soup.ObjMajic.DrawBlockedLine(rayIStart, hitsI[0].point, rayIEnds, new(1, 0, 0.5f), Vector3.forward * 0.2f);

                foreach (RaycastHit hit in hitsI) Soup.ObjMajic.ShadePureColor(hit.transform.gameObject.GetComponent<Renderer>(), new Color(1, 1, 1, 0.5f));
            }

            // ray below
            Vector3 rayBStart = spawnAtPos + Vector3.up * safeZone;
            Vector3 rayBEnds = OverrideY(spawnAtPos, levelMaxY);

            RaycastHit hitB;
            RaycastHit? ihitB = Soup.XrayHax.RayhitFirstSolid(rayBStart, rayBEnds, igns: hitsI);

            if (ihitB is null)
            {
                if (debug) Soup.ObjMajic.DrawLine(rayBStart, rayBEnds, Color.cyan, Vector3.forward * 0.3f);
                return null;
            }

            hitB = (RaycastHit)ihitB;

            if (debug) Soup.ObjMajic.DrawBlockedLine(rayBStart, hitB.point, rayBEnds, Color.yellow, Vector3.forward * 0.3f);

            return hitB.point.y;
        }

        static LevelPassTrigger[] origFinishes;
        static LevelPassTrigger origFinish;
        static Vector3 origFinishPos;
        static float origFinishTopY;
        static Vector3 origSpawnPos;
        static Vector3 spawnAtPos;

        static Vector3 OverrideY(Vector3 original, float y) => new(original.x, y, original.z);
        static Vector3 OverrideY(Vector3 original, Vector3 y) => new(original.x, y.y, original.z);

        static readonly float uniRadius = 1.5f;

        static readonly float levelMaxY = 4000;

        static readonly float safeZone = uniRadius;

        static readonly float dropDist = -Physics.gravity.y * 2;

        public static bool enabledRev = true;
        
        public static bool alwaysSpawnInside = true;

        static readonly bool debug = true;

        static readonly bool timerLoaded = BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("com.plcc.hff.timer");
    }
}