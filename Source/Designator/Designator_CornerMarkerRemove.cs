using RimWorld;
using UnityEngine;
using Verse;

namespace ProgressRenderer
{
	public class Designator_CornerMarkerRemove : Designator_CornerMarker
	{

        public Designator_CornerMarkerRemove() : base(DesignateMode.Remove)
		{
			defaultLabel = "DesignatorConerMarkerRemove".Translate();
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
