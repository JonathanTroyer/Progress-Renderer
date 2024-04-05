using HarmonyLib;
using Verse;

namespace ProgressRenderer
{
    [HarmonyPatch(typeof(RoofGrid))]
    [HarmonyPatch("RoofGridUpdate")]
    public class HarmonyRoofGridRoofGridUpdate
    {
        public static bool Prefix(Map map)
        {
            return !map.GetComponent<MapComponentRenderManager>().Rendering;
        }
    }
}
