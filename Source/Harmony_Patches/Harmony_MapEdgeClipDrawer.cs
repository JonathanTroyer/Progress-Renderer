using HarmonyLib;
using Verse;

namespace ProgressRenderer
{
    [HarmonyPatch(typeof(MapEdgeClipDrawer))]
    [HarmonyPatch("DrawClippers")]
    public class HarmonyMapEdgeClipDrawerDrawClippers
    {
        public static bool Prefix(Map map)
        {
            return !map.GetComponent<MapComponentRenderManager>().Rendering;
        }
    }
}
