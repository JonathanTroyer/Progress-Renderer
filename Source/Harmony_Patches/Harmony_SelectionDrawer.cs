using HarmonyLib;
using RimWorld;
using Verse;

namespace ProgressRenderer
{
    [HarmonyPatch(typeof(SelectionDrawer))]
    [HarmonyPatch("DrawSelectionOverlays")]
    public class HarmonySelectionDrawerDrawSelectionOverlays
    {
        public static bool Prefix()
        {
            return !Find.CurrentMap.GetComponent<MapComponentRenderManager>().Rendering;
        }
    }
}
