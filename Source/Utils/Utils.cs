using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
        
        public static bool IsAllBlack(this Texture2D texture)
        {
            if (texture.format != TextureFormat.RGBA32)
            {
                throw new ArgumentException("Texture must be in RGBA32 format.");
            }

            uint mask = BitConverter.IsLittleEndian ? 0x00FFFFFF : 0xFFFFFF00;
            using (var data = texture.GetRawTextureData<uint>())
            {
                for (int i = 0; i < data.Length; i++)
                {
                    if ((data[i] & mask) != 0)
                    {
                        return false;
                    }
                }
                return true;
            }
        }
    }
}