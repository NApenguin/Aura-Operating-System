﻿using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.CompilerServices;
using Cosmos.System.Graphics;

namespace Aura_OS.System.Processing.Application.Emulators.GameBoyEmu.Utils
{
    public class DirectBitmap
    {
        public Bitmap Bitmap { get; private set; }
        public static int Height = 144;
        public static int Width = 160;

        public DirectBitmap(int height, int width)
        {
            Width = width;
            Height = height;
            Bitmap = new Bitmap((uint)Width, (uint)Height, ColorDepth.ColorDepth32);
        }

        public DirectBitmap()
        {
            Bitmap = new Bitmap((uint)Width, (uint)Height, ColorDepth.ColorDepth32);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetPixel(int x, int y, int colour)
        {
            int index = x + y * Width;
            Bitmap.RawData[index] = colour;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetPixel(int x, int y)
        {
            int index = x + y * Width;
            return Bitmap.RawData[index];
        }
    }
}