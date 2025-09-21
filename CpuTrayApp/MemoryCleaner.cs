using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

class MemoryCleaner
{
    [DllImport("psapi.dll")]
    private static extern bool EmptyWorkingSet(IntPtr hProcess);

    public static void CleanCurrentProcessMemory()
    {
        Process proc = Process.GetCurrentProcess();
        EmptyWorkingSet(proc.Handle); // Attempts to free RAM back to Windows
    }
}
