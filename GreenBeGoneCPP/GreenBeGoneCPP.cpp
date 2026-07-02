/*
 * Project: Green Screen - Chromakey Processing (C++ Library)
 * Topic: Parallel Image Processing using High-Level Language (C++)
 * Algorithm: Euclidean RGB Distance (High Precision)
 * Description:
 * Calculates the squared Euclidean distance between each pixel's RGB components
 * and the Key Color using double-precision floating-point arithmetic.
 * If the distance is less than the squared tolerance, the pixel becomes transparent.
 * This implementation is intentionally designed with high-precision math to serve
 * as a performance baseline for Assembly optimization.
 *
 * Date: Winter Semester 2025/2026
 * Author: Jakub Augustin
 * Version: 2.1
 * History:
 * v1.0 - Initial implementation (Green Dominance)
 * v2.0 - Switched to Euclidean Distance (Integer math)
 * v2.1 - Enhanced precision using double types for benchmarking
 */

#include "pch.h"
#include "GreenBeGoneCPP.h"

 /*
  * Function: ProcessImageSegment
  * Description:
  * Helper function executed by each thread. Processes a specific horizontal strip
  * of the image buffer. It iterates through pixels, converts channel values to
  * double precision, calculates Euclidean distance, and updates the Alpha channel.
  *
  * Input Parameters:
  * data      - Pointer to the BGRA image buffer (unsigned char*).
  * width     - Image width in pixels (Range: > 0).
  * startRow  - The starting row index for this segment (inclusive).
  * endRow    - The ending row index for this segment (exclusive).
  * keyR      - Red component of the key color (Range: 0-255).
  * keyG      - Green component of the key color (Range: 0-255).
  * keyB      - Blue component of the key color (Range: 0-255).
  * tolerance - Color matching tolerance threshold (Range: 0-255).
  *
  * Output Parameters:
  * None. The image buffer is modified in-place (Alpha channel only).
  */
void ProcessImageSegment(
    unsigned char* data,
    int width,
    int startRow,
    int endRow,
    int keyR, int keyG, int keyB, int tolerance
)
{
    // Convert tolerance to double squared for precise comparison
    // Multiplied by 3.0 to account for the sum of squared differences of 3 channels
    double toleranceSq = (double)tolerance * tolerance * 3.0;

    // Iterate through assigned rows
    for (int y = startRow; y < endRow; y++)
    {
        // Calculate the starting index for the current row
        // 4 bytes per pixel (BGRA format)
        int rowIndex = y * width * 4;

        // Iterate through pixels in the row
        for (int x = 0; x < width; x++)
        {
            int pixelIndex = rowIndex + (x * 4);

            // Load pixel color components
            unsigned char b = data[pixelIndex];
            unsigned char g = data[pixelIndex + 1];
            unsigned char r = data[pixelIndex + 2];

            // Convert to double precision for high-fidelity calculation.
            // This introduces conversion overhead compared to integer math.
            double diffR = (double)r - keyR;
            double diffG = (double)g - keyG;
            double diffB = (double)b - keyB;

            // Calculate Euclidean Distance Squared: (dR^2 + dG^2 + dB^2)
            double distanceSq = (diffR * diffR) + (diffG * diffG) + (diffB * diffB);

            // Compare calculated distance against the tolerance threshold
            if (distanceSq < toleranceSq)
            {
                // Match found (Background): Set Alpha to 0 (Transparent)
                data[pixelIndex + 3] = 0;
            }
            else
            {
                // No match (Foreground): Set Alpha to 255 (Opaque)
                data[pixelIndex + 3] = 255;
            }
        }
    }
}

/*
 * Function: ProcessImage_CPP
 * Description:
 * Main entry point exported by the DLL. It validates inputs, divides the image
 * processing workload among multiple threads, and manages thread execution.
 *
 * Input Parameters:
 * outputData  - Pointer to the BGRA image data.
 * width       - Image width in pixels.
 * height      - Image height in pixels.
 * keyR, G, B  - RGB components of the key color (0-255).
 * tolerance   - Tolerance for color matching (0-255).
 * threadCount - Number of threads to use for processing (1-64).
 *
 * Output Parameters:
 * None. Modifies outputData in-place.
 */
void ProcessImage_CPP(
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

    // Thread Safety: Ensure thread count does not exceed image height
    if (threadCount > height)
        threadCount = height;

    // Calculate workload distribution (rows per thread)
    int rowsPerThread = height / threadCount;
    int remainingRows = height % threadCount;

    std::vector<std::thread> threads;

    int currentRow = 0;

    // Create and launch threads
    for (int i = 0; i < threadCount; i++)
    {
        int startRow = currentRow;
        int endRow = startRow + rowsPerThread;

        // Assign remaining rows (if any) to the last thread to ensure full coverage
        if (i == threadCount - 1)
            endRow += remainingRows;

        // Launch thread with the helper function
        threads.emplace_back(
            ProcessImageSegment,
            outputData,
            width,
            startRow,
            endRow,
            keyR, keyG, keyB,
            tolerance
        );

        currentRow = endRow;
    }

    // Wait for all threads to complete execution
    for (auto& thread : threads)
    {
        if (thread.joinable())
            thread.join();
    }
}