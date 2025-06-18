using HarmonyLib;
using Verse;
using System.Reflection;

namespace ProgressRenderer
{
    [HarmonyPatch(typeof(RoofGrid))]
    [HarmonyPatch("RoofGridUpdate")]
    public class Harmony_RoofGrid_RoofGridUpdate
    {
        public static bool Prefix(RoofGrid __instance)
        {
            // Use reflection to get the private 'map' field
            var mapField = typeof(RoofGrid).GetField("map", BindingFlags.NonPublic | BindingFlags.Instance);
            Map map = (Map)mapField.GetValue(__instance);

            if (map != null && map.GetComponent<MapComponent_RenderManager>().Rendering)
            {
                // Skip the original method if rendering is in progress
                return false;
            }
            // Allow the original method to run
            return true;
        }
    }
}