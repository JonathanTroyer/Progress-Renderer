using HarmonyLib;
using Verse;
using System.Reflection;

namespace ProgressRenderer
{
    [HarmonyPatch(typeof(DesignationManager))]
    [HarmonyPatch("DrawDesignations")]
    public class Harmony_DesignationManager_DrawDesignations
    {
        public static bool Prefix(DesignationManager __instance)
        {
            // Use reflection to access the private 'map' field
            var mapField = typeof(DesignationManager).GetField("map", BindingFlags.NonPublic | BindingFlags.Instance);
            Map map = (Map)mapField.GetValue(__instance);

            if (map != null && map.GetComponent<MapComponent_RenderManager>().Rendering)
            {
                return false;
            }
            return true;
        }
    }
}