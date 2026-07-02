/*
 * Project: Green Screen - Chromakey Processing (C++ Library)
 * Topic: Library Header Definition
 * Description:
 * Defines the interface for the C++ Dynamic Link Library (DLL).
 * Exports the main processing function for use by the UI application.
 *
 * Date: Winter Semester 2025/2026
 * Author: Jakub Augustin
 * Version: 2.1
 */

#pragma once

 // Macro definition for DLL export/import
#ifdef GREENBEGONECPP_EXPORTS
#define GREENBEGONE_API __declspec(dllexport)
#else
#define GREENBEGONE_API __declspec(dllimport)
#endif

extern "C" {
    /*
     * Function: ProcessImage_CPP
     * Description:
     * Exports the C++ implementation of the Green Screen algorithm.
     * Uses BGRA32 pixel format and supports multithreading.
     *
     * Input Parameters:
     * outputData  - Pointer to the pixel buffer (BGRA format).
     * width       - Width of the image in pixels.
     * height      - Height of the image in pixels.
     * keyR        - Red component of the key color (0-255).
     * keyG        - Green component of the key color (0-255).
     * keyB        - Blue component of the key color (0-255).
     * tolerance   - Color matching tolerance (0-255).
     * threadCount - Number of threads to utilize (1-64).
     */
    GREENBEGONE_API void ProcessImage_CPP(
        unsigned char* outputData,
        int width,
        int height,
        int keyR,
        int keyG,
        int keyB,
        int tolerance,
        int threadCount
    );
}