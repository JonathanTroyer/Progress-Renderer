using HarmonyLib;
using Verse;

namespace ProgressRenderer
{

    [HarmonyPatch(typeof(DesignatorManager))]
    [HarmonyPatch("DesignatorManagerUpdate")]
    public class Harmony_DesignatorManager_DesignatorManagerUpdate
    {

        public static bool Prefix()
        {
            if (Find.CurrentMap.GetComponent<MapComponent_RenderManager>().currentlyRendering)
            {
                return false;
            }
            return true;
        }

    }
    
}
