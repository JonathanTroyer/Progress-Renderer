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
            try
            {
                var mapField = typeof(RoofGrid).GetField("map", BindingFlags.NonPublic | BindingFlags.Instance);
                Map map = (Map)mapField?.GetValue(__instance);
                if (map != null &&  map.GetComponent<MapComponent_RenderManager>()?.Rendering == true)
                    return false;
                return true;
            }
            catch (System.Exception ex)
            {
                Log.Warning($"[ProgressRenderer] RoofGridUpdate patch error: {ex.Message}");
                return true;
            }
        }
    }
}