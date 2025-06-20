using HarmonyLib;
using Verse;
using System.Reflection;
using RimWorld;

namespace ProgressRenderer
{
    [HarmonyPatch(typeof(DesignationManager))]
    [HarmonyPatch("DrawDesignations")]
    public class Harmony_DesignationManager_DrawDesignations
    {
        public static bool Prefix(DesignationManager __instance)
        {
            try
            {
                var mapField = typeof(DesignationManager).GetField("map", BindingFlags.NonPublic | BindingFlags.Instance);
                Map map = (Map)mapField?.GetValue(__instance);
                if (map != null && map.GetComponent<MapComponent_RenderManager>()?.Rendering == true)
                    return false;
                return true;
            }
            catch (System.Exception ex)
            {
                Log.Warning($"[ProgressRenderer] DrawDesignations patch error: {ex.Message}");
                return true;
            }
        }
    }
}
