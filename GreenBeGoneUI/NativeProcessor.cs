/*
 * Project: Green Screen - Chromakey Processing (UI)
 * Topic: P/Invoke Interface
 * Description: Defines imports for external C++ and Assembly DLL functions.
 * Handles marshalling of data between managed code and native libraries.
 * Date: Winter Semester 2025/2026
 * Author: Jakub Augustin
 * Version: 1.0
 */

using System;
using System.Runtime.InteropServices;

namespace GreenBeGoneUI
{
    /// <summary>
    /// Static class containing extern method definitions for Native DLLs.
    /// </summary>
    public static class NativeProcessor
    {
        /// <summary>
        /// Import C++ function from GreenBeGoneCPP.dll
        /// </summary>
        /// <param name="outputData">Pointer to the pixel buffer (BGRA format)</param>
        /// <param name="width">Image width in pixels</param>
        /// <param name="height">Image height in pixels</param>
        /// <param name="keyR">Red component of key color</param>
        /// <param name="keyG">Green component of key color</param>
        /// <param name="keyB">Blue component of key color</param>
        /// <param name="tolerance">Color matching tolerance</param>
        /// <param name="threadCount">Number of threads to use</param>
        [DllImport("GreenBeGoneCPP.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void ProcessImage_CPP(
            IntPtr outputData,
            int width,
            int height,
            int keyR,
            int keyG,
            int keyB,
            int tolerance,
            int threadCount
        );

        /// <summary>
        /// Import Assembly function from GreenBeGoneASM.dll
        /// </summary>
        /// <param name="outputData">Pointer to the pixel buffer (BGRA format)</param>
        /// <param name="width">Image width in pixels</param>
        /// <param name="height">Image height in pixels</param>
        /// <param name="keyR">Red component of key color</param>
        /// <param name="keyG">Green component of key color</param>
        /// <param name="keyB">Blue component of key color</param>
        /// <param name="tolerance">Color matching tolerance</param>
        /// <param name="threadCount">Number of threads to use</param>
        [DllImport("GreenBeGoneASM.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void ProcessImage_ASM(
            IntPtr outputData,
            int width,
            int height,
            int keyR,
            int keyG,
            int keyB,
            int tolerance,
            int threadCount
        );
    }
}