using HarmonyLib;
using Verse;

namespace ProgressRenderer
{
    [HarmonyPatch(typeof(EnvironmentStatsDrawer))]
    [HarmonyPatch("DrawRoomOverlays")]
    public class HarmonyEnvironmentStatsDrawerDrawRoomOverlays
    {
        public static bool Prefix()
        {
            return !Find.CurrentMap.GetComponent<MapComponentRenderManager>().Rendering;
        }
    }
}
