using RimWorld;
using UnityEngine;
using Verse;

namespace ProgressRenderer
{
    public class DesignatorCornerMarkerRemove : DesignatorCornerMarker
    {

        public DesignatorCornerMarkerRemove() : base(DesignateMode.Remove)
        {
            defaultLabel = "DesignatorCornerMarkerRemove".Translate();
            defaultDesc = "DesignatorCornerMarkerRemoveDesc".Translate();
            icon = ContentFinder<Texture2D>.Get("UI/Designators/CornerMarkerOff");
            soundSucceeded = SoundDefOf.Designate_PlanRemove;
        }

        public override int DraggableDimensions
        {
            get
            {
                return 2;
            }
        }
    }
}
