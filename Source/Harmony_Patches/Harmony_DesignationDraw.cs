using HarmonyLib;
using RimWorld;
using Verse;
using System;
using UnityEngine;

namespace ProgressRenderer
{
    [HarmonyPatch(typeof(Designation))]
    [HarmonyPatch("DesignationDraw")]
    public static class Harmony_Designation_DesignationDraw
    {
        public static bool Prefix(Designation __instance)
        {
            try
            {
                DesignationDef def = __instance.def;
                if (def == null) return true; 

                var target = Traverse.Create(__instance).Field("target").GetValue<LocalTargetInfo>();

                Map map = target.HasThing ?
                    target.Thing.MapHeld : 
                    Find.CurrentMap;

                var renderManager = map?.GetComponent<MapComponent_RenderManager>();
                if (!PrModSettings.RenderDesignations && (renderManager?.Rendering ?? false))
                {
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                // 1.6 UPDATE: Enhanced error reporting
                Log.Error($"[ProgressRenderer] DesignationDraw patch error in 1.6: {ex}\n{ex.StackTrace}");
                return true;
            }
        }
    }
}