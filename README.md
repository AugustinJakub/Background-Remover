# <img width="269" height="66" alt="logo_green" src="https://github.com/user-attachments/assets/636e81d5-d2b7-4609-93df-479fed094fe1" />

**ByeByeGreen** is a multi-threaded image processing app I built for the **x64 architecture**. The main goal of this project was to write and benchmark a green screen (chroma-key) removal algorithm using two different approaches: a standard **C++ object-oriented implementation** and a low-level **Assembly (ASM) SIMD-optimized implementation** to see which performs better.



## How it's Built

The project is split into three main parts that talk to each other using DLLs:

* **UI / Frontend (C# & WPF):** A very clean desktop interface made with WPF. It lets you load files, click to pick the key color, and adjust the tolerance sliders in real time.
* **C++ Layer:** Handles the multi-threading logic (scales from 1 to 64 threads) and runs the standard version of the color-distance algorithm.
* **Assembly Layer (x64 ASM):** The optimized backend. I manually wrote this using SSE vector instructions to process 4 pixels at the same time and get around compiler limits.



## 🎬 Demo


https://github.com/user-attachments/assets/c3af3d86-e0cc-44e8-aadb-f0eddb868b9f


## Performance Benchmarks

The manual Assembly version ended up being a lot faster than the C++ version because of direct register control and SIMD parallelism. Here is a quick look at how the execution times compared on a medium-sized image using different thread counts:

| Active Threads | Assembly Backend (ms) | Standard C++ Backend (ms) |
| :---: | :---: | :---: |
| **1 Thread** | 23.31 ms | 63.13 ms |
| **2 Threads** | 19.12 ms | 39.45 ms |
| **4 Threads** | 16.79 ms | 32.62 ms |
| **8 Threads** | 14.67 ms | 25.12 ms |
| **16 Threads** | 15.90 ms | 27.48 ms |

### Conclusion
Using **x64 Assembly with SSE instructions** gave me about a **3x speedup** over the native C++ code. Eventually, the performance gains leveled off at higher thread counts due to memory bandwidth limits and thread management overhead.

