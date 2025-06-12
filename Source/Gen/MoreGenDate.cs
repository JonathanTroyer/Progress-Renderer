using RimWorld;

namespace ProgressRenderer
{

    public static class MoreGenDate
    {

        public static int QuadrumInteger(long absTicks, float longitude)
        {
            var quadrum = GenDate.Quadrum(absTicks, longitude);
            if (quadrum == Quadrum.Aprimay) { return 1; }
            if (quadrum == Quadrum.Jugust) { return 2; }
            if (quadrum == Quadrum.Septober) { return 3; }
            if (quadrum == Quadrum.Decembary) { return 4; }
            return 0;
        }
        
        public static int HoursPassedInteger(int absTicks, float longitude)
        {
            var ticks = absTicks + LocalTicksOffsetFromLongitude(longitude);
            return ticks / 2500;
        }

        private static int LocalTicksOffsetFromLongitude(float longitude)
        {
            return GenDate.TimeZoneAt(longitude) * 2500;
        }

    }

}
