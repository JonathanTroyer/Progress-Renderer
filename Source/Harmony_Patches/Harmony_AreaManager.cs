using HarmonyLib;
using Verse;

namespace ProgressRenderer
{

    [HarmonyPatch(typeof(AreaManager))]
    [HarmonyPatch("AreaManagerUpdate")]
    public class Harmony_AreaManager_AreaManagerUpdate
    {

        public static bool Prefix(AreaManager instance)
        {
            if (instance.map.GetComponent<MapComponent_RenderManager>().Rendering)
            {
                return false;
            }
            return true;
        }

    }
    
}
