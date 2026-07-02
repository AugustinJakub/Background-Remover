/*
 * Project: Green Screen - Chromakey Processing (Assembly Wrapper)
 * Topic: Multithreading Management
 * Description: Implements thread management for the Assembly DLL.
 * Divides the image into horizontal strips and assigns them to threads.
 * Date: Winter Semester 2025/2026
 * Author: Jakub Augustin
 * Version: 1.0
 */

#include "pch.h"
#include "GreenBeGoneASM.h"
#include <thread>
#include <vector>

 // Explicit declaration of the external Assembly function
extern "C" void ProcessImageSegment_ASM(
    unsigned char* data,
    int width,
    int startRow,
    int endRow,
    int keyR, int keyG, int keyB, int tolerance
);

/*
 * Function: ProcessImage_ASM
 * Description: Main entry point for the DLL. Validates input and launches threads.
 * * Input Parameters:
 * outputData - Pointer to BGRA image data.
 * width      - Image width in pixels (>0).
 * height     - Image height in pixels (>0).
 * keyR, G, B - RGB components of the key color to remove (0-255).
 * tolerance  - Distance threshold for removal (0-255).
 * threadCount - Requested number of threads (1-64).
 *
 * Output Parameters:
 * None (Data modified in place).
 */
void ProcessImage_ASM(
    unsigned char* outputData,
    int width,
    int height,
    int keyR,
    int keyG,
    int keyB,
    int tolerance,
    int threadCount)
{
    // Input Validation: Check for null pointers and invalid dimensions
    if (outputData == nullptr || width <= 0 || height <= 0 || threadCount <= 0)
        return;

    // Thread Safety: Clamp thread count to image height to avoid empty rows
    if (threadCount > height)
        threadCount = height;

    // Calculate workload per thread
    int rowsPerThread = height / threadCount;
    int remainingRows = height % threadCount;

    std::vector<std::thread> threads;

    int currentRow = 0;

    // Create and launch threads
    for (int i = 0; i < threadCount; i++)
    {
        int startRow = currentRow;
        int endRow = startRow + rowsPerThread;

        // Assign any remaining rows to the last thread
        if (i == threadCount - 1)
            endRow += remainingRows;

        // Launch thread executing the Assembly function
        threads.emplace_back(
            ProcessImageSegment_ASM,
            outputData,
            width,
            startRow,
            endRow,
            keyR, keyG, keyB,
            tolerance
        );

        currentRow = endRow;
    }

    // Join threads (Wait for completion)
    for (auto& thread : threads)
    {
        if (thread.joinable())
            thread.join();
    }
}