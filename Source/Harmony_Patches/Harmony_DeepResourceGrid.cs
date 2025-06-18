using HarmonyLib;
using Verse;
using System.Reflection;

namespace ProgressRenderer
{
    [HarmonyPatch(typeof(DeepResourceGrid))]
    [HarmonyPatch("DeepResourceGridUpdate")]
    public class Harmony_DeepResourceGrid_DeepResourceGridUpdate
    {
        public static bool Prefix(DeepResourceGrid __instance)
        {
            // Use reflection to get the private map field
            var mapField = typeof(DeepResourceGrid).GetField("map", BindingFlags.NonPublic | BindingFlags.Instance);
            Map map = (Map)mapField.GetValue(__instance);

            if (map.GetComponent<MapComponent_RenderManager>().Rendering)
            {
                return false;
            }
            return true;
        }
    }
}