using RimWorld;
using UnityEngine;
using Verse;

namespace ProgressRenderer
{
    public class DesignatorCornerMarkerAdd : DesignatorCornerMarker
    {

        public DesignatorCornerMarkerAdd() : base(DesignateMode.Add)
        {
            defaultLabel = "DesignatorCornerMarker".Translate();
            defaultDesc = "DesignatorCornerMarkerDesc".Translate();
            icon = ContentFinder<Texture2D>.Get("UI/Designators/CornerMarkerOn");
            soundSucceeded = SoundDefOf.Designate_PlanAdd;
        }
    }

}
