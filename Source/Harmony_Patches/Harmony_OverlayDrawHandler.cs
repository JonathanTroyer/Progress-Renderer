using HarmonyLib;
using RimWorld;
using Verse;

namespace ProgressRenderer
{
    [HarmonyPatch(typeof(OverlayDrawHandler))]
    [HarmonyPatch("ShouldDrawPowerGrid", MethodType.Getter)]
    public class HarmonyOverlayDrawHandlerShouldDrawPowerGrid
    {
        public static bool Prefix()
        {
            return !Find.CurrentMap.GetComponent<MapComponentRenderManager>().Rendering;
        }
    }
}
