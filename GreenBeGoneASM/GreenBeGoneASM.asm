; ============================================================================================
; Project: Green Screen - Chromakey Processing
; Topic: Parallel Image Processing using x64 Assembly and SIMD
; Algorithm: Euclidean RGB Distance (SSE4.1 Optimized)
; Description: 
;   Calculates the squared Euclidean distance between each pixel's RGB components 
;   and the Key Color. If the distance is less than the squared Tolerance, 
;   the pixel's Alpha channel is set to 0 (Transparent). Otherwise, it is set to 255 (Opaque).
;   This implementation uses SSE4.1 vector instructions to process 4 pixels simultaneously.
;
; Date: Winter Semester 2025/2026
; Author: Jakub Augustin
; Version: 9.0 (Final Optimized)
; History:
;   v1.0 - Basic scalar implementation
;   v5.0 - Initial SIMD vectorization
;   v9.0 - Optimized 4-pixel parallel processing, reduced branching, compact code
; ============================================================================================

.data
    ALIGN 16
    ; Alpha Mask: Used to force Alpha=255 (Opaque) logic. 
    ; Contains 0xFF000000 repeated 4 times (for 4 pixels).
    AlphaFull DWORD 0FF000000h, 0FF000000h, 0FF000000h, 0FF000000h

    ; MaskClearA: Used to zero out the Alpha difference during calculation.
    ; We only care about RGB differences. Layout: [FFFF, FFFF, FFFF, 0000] repeated.
    MaskClearA WORD 0FFFFh, 0FFFFh, 0FFFFh, 0000h, 0FFFFh, 0FFFFh, 0FFFFh, 0000h

.code

PUBLIC ProcessImageSegment_ASM

; ============================================================================================
; Procedure: ProcessImageSegment_ASM
; Description: 
;   Processes a specific slice of the image buffer. It iterates through the pixel data,
;   calculates color distance, and applies the transparency mask using SIMD instructions.
;
; Input Parameters (x64 calling convention):
;   RCX - outputData (unsigned char*): Pointer to the BGRA image buffer.
;   RDX - width (int): Width of the image in pixels (Range: > 0).
;   R8  - startRow (int): The starting row index for this thread (Range: 0 to height-1).
;   R9  - endRow (int): The ending row index (exclusive) for this thread.
;
; Stack Parameters (passed via stack, offset from RBP):
;   [RBP+48] - keyR (int): Red component of the key color (Range: 0-255).
;   [RBP+56] - keyG (int): Green component of the key color (Range: 0-255).
;   [RBP+64] - keyB (int): Blue component of the key color (Range: 0-255).
;   [RBP+72] - tolerance (int): Color matching tolerance (Range: 0-255).
;
; Output Parameters:
;   None. The image buffer at [RCX] is modified in-place.
;
; Modified Registers:
;   RAX, RBX, RCX, RDX, R8, R9, R10, R11, R12, R13, R14, R15
;   XMM0, XMM1, XMM2, XMM3, XMM10, XMM11, XMM12, XMM13
;   RFLAGS
; ============================================================================================
ProcessImageSegment_ASM PROC
    ; --- 1. Prologue ---
    push rbp                    ; Save the base pointer of the caller
    mov rbp, rsp                ; Set the base pointer to the current stack pointer
    push rbx                    ; Save non-volatile register RBX
    push rdi                    ; Save non-volatile register RDI
    push rsi                    ; Save non-volatile register RSI
    push r12                    ; Save non-volatile register R12
    push r13                    ; Save non-volatile register R13
    push r14                    ; Save non-volatile register R14
    push r15                    ; Save non-volatile register R15

    ; --- 2. Load Parameters ---
    mov rdi, rcx                ; RDI = Pointer to image data (RCX)
    mov rsi, rdx                ; RSI = Image Width (RDX)
    mov r12, r8                 ; R12 = Start Row Index (R8)
    mov r13, r9                 ; R13 = End Row Index (R9)

    ; --- 3. Prepare Constants (Key Color & Tolerance) ---
    
    ; Calculate Tolerance Squared * 3
    ; Logic: distSq < tol^2 * 3 (because we sum 3 channels: R, G, B)
    mov eax, dword ptr [rbp+72] ; Load 'tolerance' from stack
    imul eax, eax               ; EAX = tolerance * tolerance
    imul eax, 3                 ; EAX = tolerance^2 * 3
    movd xmm13, eax             ; Move scalar value to XMM13 (low dword)
    pshufd xmm13, xmm13, 0      ; Broadcast tolerance to all 4 dwords in XMM13

    ; Prepare Key Color Vector for subtraction
    ; We need a pattern [KeyB, KeyG, KeyR, 0] in 16-bit words.
    xor eax, eax                ; Clear EAX
    mov al, byte ptr [rbp+64]   ; AL = KeyB
    mov ah, byte ptr [rbp+56]   ; AH = KeyG
    shl eax, 16                 ; Shift BG to upper half of EAX
    mov al, byte ptr [rbp+48]   ; AL = KeyR
    ror eax, 16                 ; Rotate right to align formatting: 0x00RRGGBB
    
    movd xmm10, eax             ; Move the 32-bit pattern to XMM10
    pmovzxbw xmm10, xmm10       ; Zero-extend bytes to words: [B, G, R, 0] (16-bit integers)
    pshufd xmm10, xmm10, 01000100b ; Broadcast the pattern to fill XMM10: [BGR0, BGR0...]

    ; Load Helper Masks from Data Section
    movdqa xmm11, XMMWORD PTR [MaskClearA] ; XMM11 = Mask to zero out Alpha difference
    movdqa xmm12, XMMWORD PTR [AlphaFull]  ; XMM12 = Mask with 0xFF000000 for forcing opaque

    ; --- 4. Main Processing Loop (Rows) ---
row_loop:
    cmp r12, r13                ; Compare current row (R12) with end row (R13)
    jge end_proc                ; If current >= end, we are done, jump to epilogue

    ; Calculate memory offset for the current row
    mov rax, r12                ; RAX = current row index
    imul rax, rsi               ; RAX = row * width
    shl rax, 2                  ; RAX = (row * width) * 4 bytes per pixel
    lea rbx, [rdi + rax]        ; RBX = Pointer to the start of the current row

    mov r14, rsi                ; R14 = Column Counter (number of pixels in width)

    ; --- 5. Column Loop (Process 4 pixels at a time) ---
col_loop:
    cmp r14, 4                  ; Check if at least 4 pixels are remaining
    jl scalar_loop              ; If less than 4, jump to scalar fallback loop

    ; A. Load and Expand Pixel Data
    movdqu xmm0, [rbx]          ; Load 16 bytes (4 pixels) into XMM0
    pmovzxbw xmm1, xmm0         ; Unpack low 8 bytes (Pixels 0, 1) to words in XMM1
    psrldq xmm0, 8              ; Shift XMM0 right by 8 bytes
    pmovzxbw xmm2, xmm0         ; Unpack high 8 bytes (Pixels 2, 3) to words in XMM2

    ; B. Calculate Differences and Square them
    psubw xmm1, xmm10           ; XMM1 = Pixel[0,1] - KeyVector (Signed 16-bit differences)
    psubw xmm2, xmm10           ; XMM2 = Pixel[2,3] - KeyVector
    
    pand xmm1, xmm11            ; Mask out the Alpha channel difference in XMM1
    pand xmm2, xmm11            ; Mask out the Alpha channel difference in XMM2
    
    pmaddwd xmm1, xmm1          ; Square and Horizontal Add: XMM1 = [R^2, G^2+B^2] pairs for P0, P1
    pmaddwd xmm2, xmm2          ; Square and Horizontal Add: XMM2 = [R^2, G^2+B^2] pairs for P2, P3

    ; C. Horizontal Sum to get Final Distance
    phaddd xmm1, xmm2           ; Horizontally add dwords. XMM1 now contains [Dist3, Dist2, Dist1, Dist0]

    ; D. Compare Distance vs Tolerance
    movdqa xmm3, xmm13          ; Copy Tolerance Squared to XMM3
    pcmpgtd xmm3, xmm1          ; Compare: XMM3 = (Tolerance > Distance) ? 0xFFFFFFFF : 0x00000000
                                ; XMM3 is now the Transparency Mask (-1 = Transparent)

    ; E. Apply Logic to Alpha Channel
    movdqu xmm0, [rbx]          ; Reload original 4 pixels into XMM0
    por xmm0, xmm12             ; Force Alpha to 255 (0xFF) everywhere initially (Opaque state)
    
    pand xmm3, xmm12            ; Mask the Comparison Result to only affect Alpha bytes (0xFF000000 or 0)
    pxor xmm0, xmm3             ; Flip bits: 
                                ; If Transparent (Mask=FF): 255 XOR 255 = 0 (Alpha becomes 0)
                                ; If Opaque (Mask=00): 255 XOR 0 = 255 (Alpha stays 255)

    movdqu [rbx], xmm0          ; Write the modified 4 pixels back to memory

    ; F. Loop Maintenance
    add rbx, 16                 ; Advance memory pointer by 16 bytes (4 pixels)
    sub r14, 4                  ; Decrease pixel counter by 4
    jmp col_loop                ; Repeat column loop

    ; --- 6. Scalar Fallback Loop (For remaining 1-3 pixels) ---
scalar_loop:
    cmp r14, 0                  ; Check if any pixels are left
    jle next_row                ; If 0, move to the next row

    ; Load single pixel components
    movzx eax, byte ptr [rbx]   ; EAX = Blue
    movzx ecx, byte ptr [rbx+1] ; ECX = Green
    movzx edx, byte ptr [rbx+2] ; EDX = Red
    
    ; Calculate Differences
    sub eax, dword ptr [rbp+64] ; Subtract KeyB
    sub ecx, dword ptr [rbp+56] ; Subtract KeyG
    sub edx, dword ptr [rbp+48] ; Subtract KeyR

    ; Square the differences
    imul eax, eax               ; B^2
    imul ecx, ecx               ; G^2
    imul edx, edx               ; R^2

    ; Sum the squares
    add eax, ecx
    add eax, edx                ; EAX = Distance Squared
    
    ; Re-calculate scalar Tolerance Squared (avoiding register shuffling overhead)
    push rax                    ; Save DistanceSq
    mov eax, dword ptr [rbp+72] ; Load Tolerance
    imul eax, eax               ; Tol^2
    imul eax, 3                 ; Tol^2 * 3
    mov ecx, eax                ; ECX = Tolerance Threshold
    pop rax                     ; Restore DistanceSq

    ; Compare Distance vs Tolerance
    cmp eax, ecx                ; Compare DistSq (EAX) with TolSq (ECX)
    jl make_transp              ; If Dist < Tol, jump to make transparent
    
    mov byte ptr [rbx+3], 255   ; Else, set Alpha = 255 (Opaque)
    jmp next_px                 ; Continue
    
make_transp:
    mov byte ptr [rbx+3], 0     ; Set Alpha = 0 (Transparent)

next_px:
    add rbx, 4                  ; Move pointer to next pixel (4 bytes)
    dec r14                     ; Decrease pixel counter
    jmp scalar_loop             ; Repeat scalar loop

next_row:
    inc r12                     ; Increment row index
    jmp row_loop                ; Repeat row loop

end_proc:
    ; --- 7. Epilogue ---
    pop r15                     ; Restore R15
    pop r14                     ; Restore R14
    pop r13                     ; Restore R13
    pop r12                     ; Restore R12
    pop rsi                     ; Restore RSI
    pop rdi                     ; Restore RDI
    pop rbx                     ; Restore RBX
    pop rbp                     ; Restore Base Pointer
    ret                         ; Return from procedure
ProcessImageSegment_ASM ENDP
END