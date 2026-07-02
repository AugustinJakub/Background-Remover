/*
 * Project: Green Screen - Chromakey Processing (C++ Library)
 * Topic: DLL Entry Point
 * Description:
 * Defines the entry point for the DLL application.
 * Handles process/thread attachment and detachment events.
 *
 * Date: Winter Semester 2025/2026
 * Author: Jakub Augustin
 * Version: 1.0
 */

#include "pch.h"

BOOL APIENTRY DllMain(HMODULE hModule,
    DWORD  ul_reason_for_call,
    LPVOID lpReserved)
{
    switch (ul_reason_for_call)
    {
    case DLL_PROCESS_ATTACH:
        // Code to run when the DLL is loaded into the process address space.
        break;
    case DLL_THREAD_ATTACH:
        // Code to run when a new thread is created in the process.
        break;
    case DLL_THREAD_DETACH:
        // Code to run when a thread exits cleanly.
        break;
    case DLL_PROCESS_DETACH:
        // Code to run when the DLL is unloaded.
        break;
    }
    return TRUE;
}