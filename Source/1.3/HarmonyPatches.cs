using System;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace DisableRoofCheck
{
    [StaticConstructorOnStartup]
    internal class HarmonyPatches
    {
        static HarmonyPatches()
        {
            var harmony = new Harmony("katana.disableroofcheck");
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            Log.Message("[DisableRoofCheck] Loaded");
        }

        [HarmonyPatch(typeof(RimWorld.Need_Indoors), "NeedInterval")]
        static class Patch_Need_Indoors
        {
            static readonly float Delta_Indoors_ThickRoof = 2f;
            static readonly float Delta_Indoors_NoRoof = 0f;
            static readonly float Delta_Outdoors_ThickRoof = 0f;
            static readonly float Delta_Outdoors_NoRoof = -0.25f;

            [HarmonyPrefix]
            static bool Prefix(Need_Indoors __instance, Pawn ___pawn, NeedDef ___def, ref float ___lastEffectiveDelta)
            {
                bool disabled = Traverse.Create(__instance).Property("Disabled").GetValue<bool>();
                bool isFrozen = Traverse.Create(__instance).Property("IsFrozen").GetValue<bool>();

                if (disabled)
                {
                    __instance.CurLevel = 1f;
                }
                else if (!isFrozen)
                {
                    float num = 0f;
                    bool flag = !___pawn.Spawned || ___pawn.Position.UsesOutdoorTemperature(___pawn.Map);
                    RoofDef roofDef = (___pawn.Spawned ? ___pawn.Position.GetRoof(___pawn.Map) : null);
                    float curLevel = __instance.CurLevel;

                    if (!flag)
                    {
                        num = (roofDef == null) ? Patch_Need_Indoors.Delta_Indoors_NoRoof : Patch_Need_Indoors.Delta_Indoors_ThickRoof;
                    }
                    else
                    {
                        num = (roofDef == null) ? Patch_Need_Indoors.Delta_Outdoors_NoRoof : Patch_Need_Indoors.Delta_Outdoors_ThickRoof;
                    }

                    num *= 0.0025f;
                    if (num < 0f)
                    {
                        __instance.CurLevel = Mathf.Min(__instance.CurLevel, __instance.CurLevel + num);
                    }
                    else
                    {
                        __instance.CurLevel = Mathf.Min(__instance.CurLevel + num, 1f);
                    }
                    ___lastEffectiveDelta = __instance.CurLevel - curLevel;
                }

                return false;
            }
        }

        [HarmonyPatch(typeof(ThoughtWorker_IsIndoorsForUndergrounder), "IsAwakeAndIndoors")]
        public class ThoughtWorker_IsIndoorsForUndergrounderPatch
        {
            [HarmonyPrefix]
            public static bool Prefix(ref bool __result, Pawn p, out bool isNaturalRoof)
            {
                isNaturalRoof = true;
                __result = (p.Awake() && !(p.Position.UsesOutdoorTemperature(p.Map) || !p.Position.Roofed(p.Map)));
                return false;
            }
        }
    }
}
