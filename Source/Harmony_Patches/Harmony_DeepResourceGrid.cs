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
            try
            {
                var mapField = typeof(DeepResourceGrid).GetField("map", BindingFlags.NonPublic | BindingFlags.Instance);
                Map map = (Map)mapField?.GetValue(__instance);
                if (map != null && map.GetComponent<MapComponent_RenderManager>()?.Rendering == true)
                    return false;
                return true;
            }
            catch (System.Exception ex)
            {
                Log.Warning($"[ProgressRenderer] DeepResourceGridUpdate patch error: {ex.Message}");
                return true;
            }
        }
    }
}