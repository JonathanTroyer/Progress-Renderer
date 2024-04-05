using RimWorld;
using Verse;

namespace ProgressRenderer
{

    [DefOf]
    public static class DesignationDefOf
    {

        public static DesignationDef CornerMarker;

        static DesignationDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(DesignationDefOf));
        }
    }

}
