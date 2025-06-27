using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

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

            lock (Lock)
            {
                _isProcessing = false;
            }
        }


        public static bool CloseEquals(this float a, float b, float tolerance = Single.Epsilon)
        {
            return Math.Abs(a - b) < tolerance;
        }

        // About 17% faster than a regular for-loop with indexing
        public static bool IsAllBlack(this Texture2D texture)
        {
            if (texture.format != TextureFormat.RGB24)
            {
                throw new ArgumentException("Texture must be in RGB24 format.");
            }

            using var data = texture.GetRawTextureData<byte>();
            int byteLength = data.Length;

            // Process as many complete uint values as possible
            // Every 12 bytes = 4 RGB pixels = 3 uints
            int i = 0;
            int uintAlignedEnd = byteLength - byteLength % 12;

            for (; i < uintAlignedEnd; i += 12)
            {
                uint val1 = (uint)data[i] | (uint)data[i + 1] << 8 |
                            (uint)data[i + 2] << 16 | (uint)data[i + 3] << 24;
                uint val2 = (uint)data[i + 4] | (uint)data[i + 5] << 8 |
                            (uint)data[i + 6] << 16 | (uint)data[i + 7] << 24;
                uint val3 = (uint)data[i + 8] | (uint)data[i + 9] << 8 |
                            (uint)data[i + 10] << 16 | (uint)data[i + 11] << 24;

                if ((val1 | val2 | val3) != 0)
                    return false;
            }

            // Check remaining bytes
            for (; i < byteLength; i++)
            {
                if (data[i] != 0)
                    return false;
            }

            return true;
        }
    }
}
