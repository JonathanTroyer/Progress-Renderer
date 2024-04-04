using System;

namespace ProgressRenderer
{
    public static class Utils
    {
        public static bool GlobalRenderingLock;
        
        public static bool CloseEquals(this float a, float b, float tolerance = Single.Epsilon)
        {
            return Math.Abs(a - b) < tolerance;
        }
    }
}