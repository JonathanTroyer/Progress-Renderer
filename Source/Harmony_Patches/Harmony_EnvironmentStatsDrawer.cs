using HarmonyLib;
using Verse;

namespace ProgressRenderer
{

    [HarmonyPatch(typeof(EnvironmentStatsDrawer))]
    [HarmonyPatch("DrawRoomOverlays")]
    public class Harmony_EnvironmentStatsDrawer_DrawRoomOverlays
    {

        public static bool Prefix()
        {
            if (Find.CurrentMap.GetComponent<MapComponent_RenderManager>().currentlyRendering)
            {
                return false;
            }
            return true;
        }

    }
    
}
