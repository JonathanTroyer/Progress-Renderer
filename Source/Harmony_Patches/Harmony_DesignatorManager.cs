using HarmonyLib;
using Verse;

namespace ProgressRenderer
{
    [HarmonyPatch(typeof(DesignatorManager))]
    [HarmonyPatch("DesignatorManagerUpdate")]
    public class HarmonyDesignatorManagerDesignatorManagerUpdate
    {
        public static bool Prefix()
        {
            return !Find.CurrentMap.GetComponent<MapComponentRenderManager>().Rendering;
        }
    }
}
