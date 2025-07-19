using System;
using HarmonyLib;
using Verse;

namespace ProgressRenderer
{
    [HarmonyPatchCategory("PlanningExtended")]
    [HarmonyPatch("PlanDesignation", nameof(Designation.DesignationDraw))]
    public static class Harmony_PlanDesignation_DesignationDraw
    {
        public static bool Prefix(Designation __instance)
        {
            try
            {
                if (__instance.def == null) return true;

                var target = __instance.target;

                Map map = target.HasThing ? target.Thing.MapHeld : Find.CurrentMap;

                var renderManager = map?.GetComponent<MapComponent_RenderManager>();

                if (renderManager?.Rendering == false) return true;

                return PrModSettings.RenderDesignations;
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