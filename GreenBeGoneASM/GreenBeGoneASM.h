/*
 * Project: Green Screen - Chromakey Processing (Assembly)
 * Topic: Library Header Definition
 * Description: Exports functions implemented in Assembly and C++ Wrapper.
 * Date: Winter Semester 2025/2026
 * Author: Jakub Augustin
 * Version: 1.0
 */

#pragma once

#ifdef GREENBEGONEASM_EXPORTS
#define GREENBEGONEASM_API __declspec(dllexport)
#else
#define GREENBEGONEASM_API __declspec(dllimport)
#endif

extern "C" {
    /*
     * Function: ProcessImageSegment_ASM
     * Description: Low-level Assembly function that processes a single image segment.
     * It modifies the Alpha channel based on color distance.
     * Parameters:
     * data      - Pointer to the BGRA pixel buffer.
     * width     - Width of the image.
     * startRow  - Starting row index.
     * endRow    - Ending row index.
     * keyR, G, B - Key color components.
     * tolerance - Color matching tolerance.
     */
    void ProcessImageSegment_ASM(
        unsigned char* data,
        int width,
        int startRow,
        int endRow,
        int keyR,
        int keyG,
        int keyB,
        int tolerance
    );

    /*
     * Function: ProcessImage_ASM
     * Description: High-level C++ wrapper that manages multithreading.
     * It divides the work and calls ProcessImageSegment_ASM in parallel.
     * Parameters:
     * outputData - Pointer to the image buffer.
     * width, height - Image dimensions.
     * keyR, G, B - Key color.
     * tolerance - Threshold.
     * threadCount - Number of threads to utilize (1-64).
     */
    GREENBEGONEASM_API void ProcessImage_ASM(
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