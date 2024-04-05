using RimWorld;
using Verse;

namespace ProgressRenderer
{

    [DefOf]
    public static class KeyBindingDefOf
    {

        public static KeyBindingDef LprManualRendering;
        public static KeyBindingDef LprManualRenderingForceFullMap;

        static KeyBindingDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(KeyBindingDefOf));
        }
    }

}
