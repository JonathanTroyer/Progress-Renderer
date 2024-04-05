using HarmonyLib;
using Verse;

namespace ProgressRenderer
{
    [HarmonyPatch(typeof(DeepResourceGrid))]
    [HarmonyPatch("DeepResourceGridUpdate")]
    public class HarmonyDeepResourceGridDeepResourceGridUpdate
    {
        public static bool Prefix(Map map)
        {
            return !map.GetComponent<MapComponentRenderManager>().Rendering;
        }
    }
}
