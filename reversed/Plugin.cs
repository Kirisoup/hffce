using System;
using System.Collections.Generic;
using BepInEx;
using HarmonyLib;
using HumanAPI;
using Multiplayer;
using Timer;
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

        static LevelPassTrigger[] finishes;


        
        static void PlayerRt()
        {
            Ray.CollisionOverlapped(Human.Localplayer.GetComponent<SphereCollider>().bounds.center);
        }

        static class Ray
        {
            public static bool CollisionOverlapped(Vector2 pos)
            {
                RaycastHit[] hitsAbove = Physics.RaycastAll(pos, Vector3.up, float.MaxValue);

                bool hasCollisionAbove = false;

                foreach (RaycastHit hit in hitsAbove)
                {
                    if (hit.collider is null || hit.collider.isTrigger) continue;
                    hasCollisionAbove = true; break;
                }

                if (hasCollisionAbove) return true;

                RaycastHit[] hitsBellow = Physics.RaycastAll(pos, Vector3.down, float.MaxValue);

                bool hasCollisionBellow = false;

                foreach (RaycastHit hit in hitsBellow)
                {
                    if (hit.collider is null || hit.collider.isTrigger) continue;
                    hasCollisionBellow = true; break;
                }

                return hasCollisionBellow;
            }

            // public static float FindUpLimit(Vector2 pos)
            // {
                
            // }
        }





        static void ModifyFinish(LevelPassTrigger finish)
        {
            if (finish is null || !finish.enabled) return;

            finish.gameObject.GetComponent<LevelPassTrigger>().enabled = false;

            Collider[] colliders = finish.gameObject.GetComponents<Collider>();

            bool overlapped = Ray.CollisionOverlapped(finish.transform.position);

            foreach (Collider collider in colliders)
            {
                if (!collider.enabled) continue;
                
                // collider.isTrigger = false;

                if (overlapped) collider.enabled=false;
            }

            if (overlapped) return;

            WindEffector winde = finish.gameObject.AddComponent<WindEffector>();

            winde.ignoreParents = new Transform[] {finish.gameObject.transform};

            winde.humanFlyForce = 100;

            winde.applyAcceleration = false;

            winde.cDamp = 0.02f;

            winde.centerBend = 0;

            winde.coefDrag = 0;

            winde.distPower = 1;

            winde.maxDist = 200;

            winde.respectArea = false;

            winde.wind = new(0, 1, 0);

            winde.enabled = true;

        }

        static class Rev
        {
            [HarmonyPatch(typeof(Game), "AfterLoad")]
            [HarmonyPrefix]
            static void AfterLoad()
            {
                finishes = FindObjectsOfType<LevelPassTrigger>();

                LevelPassTrigger finish = FindClosestFinish(finishes);

                if (finish is null) return;

                Vector3 finpos = finish.transform.position;
                float finhight = finish.gameObject.transform.lossyScale.y/2;

                CreateDummyFinish(FindObjectOfType<Level>().checkpoints[0].position, finish.gameObject);

                ModifyFinish(finish);

                ChangeSpawnTo(finpos, finhight);
            }
        }

        static void CreateDummyFinish(Vector3 pos, GameObject src)
        {
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            obj.name = "revfinish";
            obj.SetActive(true);
            obj.transform.SetParent(src.transform.parent, false);
            obj.transform.localScale = Vector3.one * 8;
            obj.transform.position = pos;
            obj.GetComponent<Collider>().isTrigger = true;
            obj.AddComponent<LevelPassTrigger>();
            obj.GetComponent<MeshRenderer>().enabled = false;
        }

        static LevelPassTrigger FindClosestFinish(LevelPassTrigger[] finishes)
        {
            Transform lastcp = Game.currentLevel.GetCheckpointTransform(Game.currentLevel.checkpoints.Length - 1);

            float closestDist = float.MaxValue;
            LevelPassTrigger closestFinish = null;

            foreach (LevelPassTrigger finish in finishes)
            {
                print("found endzone");

                float distance = Vector3.Distance(finish.transform.position, lastcp.position);

                if (distance >= closestDist) continue;

                print($"selected endzone with distance to endcp {distance}");

                closestDist = distance;
                closestFinish = finish;
            }

            return closestFinish;
        }

        static void ChangeSpawnTo(Vector3 finpos, float finhight) => FindObjectOfType<Level>().checkpoints[0].position = new(finpos.x, finpos.y + finhight, finpos.z);

        static class Storage
        {
            class Endzone
            {

            }
        }

        public static bool enabledRev = true;
        static readonly bool timerLoaded = BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("com.plcc.hff.timer");
    }
}