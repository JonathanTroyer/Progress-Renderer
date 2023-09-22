using HarmonyLib;
using RimWorld;
using Verse;

namespace ProgressRenderer
{

    [HarmonyPatch(typeof(GameConditionManager))]
    [HarmonyPatch("GameConditionManagerDraw")]
    public class Harmony_GameConditionManager_GameConditionManagerDraw
    {

        public static bool Prefix(Map map)
        {
            if (!MapComponent_RenderManager.renderGameConditions && map.GetComponent<MapComponent_RenderManager>().currentlyRendering)
            {
                return false;
            }
            return true;
        }

    }
    
}
