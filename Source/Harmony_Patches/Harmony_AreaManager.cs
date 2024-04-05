using HarmonyLib;
using Verse;

namespace ProgressRenderer
{
    [HarmonyPatch(typeof(AreaManager))]
    [HarmonyPatch("AreaManagerUpdate")]
    public class HarmonyAreaManagerAreaManagerUpdate
    {
        public static bool Prefix(AreaManager instance)
        {
            return !instance.map.GetComponent<MapComponentRenderManager>().Rendering;
        }
    }
}
