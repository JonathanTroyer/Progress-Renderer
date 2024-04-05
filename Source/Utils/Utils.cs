using System;
using System.Collections;
using System.Collections.Generic;

namespace ProgressRenderer
{
    public static class Utils
    {
        public static readonly Queue<IEnumerator> Renderings = new Queue<IEnumerator>();
        private static bool _isProcessing;

        public static readonly object Lock = new object();

        public static IEnumerator ProcessRenderings()
        {
            lock (Lock)
            {
                if (_isProcessing) yield break;
                _isProcessing = true;
            }

            while (Renderings.Count > 0)
            {
                yield return Renderings.Dequeue();
            }

            _isProcessing = false;
        }

        public static bool CloseEquals(this float a, float b, float tolerance = Single.Epsilon)
        {
            return Math.Abs(a - b) < tolerance;
        }
    }
}
