using HarmonyLib;
using Verse;

namespace ProgressRenderer
{

    [HarmonyPatch(typeof(DesignationManager))]
    [HarmonyPatch("DrawDesignations")]
    public class Harmony_DesignationManager_DrawDesignations
    {

        public static bool Prefix(DesignationManager __instance)
        {
            if (!MapComponent_RenderManager.renderDesignations && __instance.map.GetComponent<MapComponent_RenderManager>().currentlyRendering)
            {
                return false;
            }
            return true;
        }

    }
    
}
