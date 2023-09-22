using HarmonyLib;
using Verse;

namespace ProgressRenderer
{

    [HarmonyPatch(typeof(AreaManager))]
    [HarmonyPatch("AreaManagerUpdate")]
    public class Harmony_AreaManager_AreaManagerUpdate
    {

        public static bool Prefix(AreaManager __instance)
        {
            if (__instance.map.GetComponent<MapComponent_RenderManager>().currentlyRendering)
            {
                return false;
            }
            return true;
        }

    }
    
}
