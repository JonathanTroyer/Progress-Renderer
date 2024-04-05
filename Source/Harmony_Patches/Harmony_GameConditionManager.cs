using HarmonyLib;
using RimWorld;
using Verse;

namespace ProgressRenderer
{
    [HarmonyPatch(typeof(GameConditionManager))]
    [HarmonyPatch("GameConditionManagerDraw")]
    public class HarmonyGameConditionManagerGameConditionManagerDraw
    {
        public static bool Prefix(Map map)
        {
            return PrModSettings.RenderGameConditions || !map.GetComponent<MapComponentRenderManager>().Rendering;
        }
    }
}
