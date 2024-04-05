using RimWorld;

namespace ProgressRenderer
{
    public static class MoreGenDate
    {
        public static int QuadrumInteger(long absTicks, float longitude)
        {
            var quadrum = GenDate.Quadrum(absTicks, longitude);
            switch (quadrum)
            {
                case Quadrum.Aprimay:
                    return 1;
                case Quadrum.Jugust:
                    return 2;
                case Quadrum.Septober:
                    return 3;
                case Quadrum.Decembary:
                    return 4;
                case Quadrum.Undefined:
                default:
                    return 0;
            }
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
