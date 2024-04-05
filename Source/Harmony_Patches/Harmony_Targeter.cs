using HarmonyLib;
using RimWorld;
using Verse;

namespace ProgressRenderer
{
    [HarmonyPatch(typeof(Targeter))]
    [HarmonyPatch("TargeterUpdate")]
    public class HarmonyTargeterTargeterUpdate
    {
        public static bool Prefix()
        {
            return !Find.CurrentMap.GetComponent<MapComponentRenderManager>().Rendering;
        }
    }
}
