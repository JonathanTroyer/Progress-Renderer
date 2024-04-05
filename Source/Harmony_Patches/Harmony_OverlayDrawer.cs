using HarmonyLib;
using RimWorld;
using Verse;

namespace ProgressRenderer
{
    [HarmonyPatch(typeof(OverlayDrawer))]
    [HarmonyPatch("DrawAllOverlays")]
    public class HarmonyOverlayDrawerDrawAllOverlays
    {
        public static bool Prefix()
        {
            return PrModSettings.RenderThingIcons || !Find.CurrentMap.GetComponent<MapComponentRenderManager>().Rendering;
        }
    }
}
