using HarmonyLib;
using Verse;

namespace ProgressRenderer
{
    [HarmonyPatch(typeof(DesignationManager))]
    [HarmonyPatch("DrawDesignations")]
    public class HarmonyDesignationManagerDrawDesignations
    {
        public static bool Prefix(DesignationManager instance)
        {
            return PrModSettings.RenderDesignations || !instance.map.GetComponent<MapComponentRenderManager>().Rendering;
        }
    }
}
