using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Un0
{
    public sealed class SingleInstance
    {
        private const string PROCESS_NAME = "Un0";
        private const string DLL_NAME = "Un0.dll";

        public static IntPtr ActivateExistingWindow()
        {
            try
            {
                Process current = Process.GetCurrentProcess();

                foreach (var p in Process.GetProcesses())
                {
                    try
                    {
                        if (p.Id == current.Id) continue;

                        bool isUn0 = p.ProcessName.Equals(PROCESS_NAME, StringComparison.OrdinalIgnoreCase) ||
                                     p.ProcessName.Equals($"{PROCESS_NAME}.exe", StringComparison.OrdinalIgnoreCase);

                        if (isUn0)
                        {
                            IntPtr hWnd = p.MainWindowHandle;

                            if (hWnd != IntPtr.Zero)
                            {
                                if (User32API.IsIconic(hWnd))
                                    User32API.ShowWindow(hWnd, User32API.SW_RESTORE);

                                User32API.SetForegroundWindow(hWnd);
                                User32API.FlashWindow(hWnd, true);
                            }
                            return hWnd;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error checking process {p.ProcessName}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ActivateExistingWindow error: {ex.Message}");
            }
            return IntPtr.Zero;
        }

        public static bool IsProcessRunning(string processName)
        {
            try { return Process.GetProcessesByName(processName).Length > 0; }
            catch { return false; }
        }

        public static bool IsAnyUn0ProcessRunning()
        {
            try
            {
                return Process.GetProcesses().Any(p =>
                {
                    try
                    {
                        return p.ProcessName.Equals(PROCESS_NAME, StringComparison.OrdinalIgnoreCase) ||
                               p.ProcessName.Equals($"{PROCESS_NAME}.exe", StringComparison.OrdinalIgnoreCase);
                    }
                    catch { return false; }
                });
            }
            catch { return false; }
        }

        public static bool IsUn0DllLoaded()
        {
            try
            {
                int currentId = Process.GetCurrentProcess().Id;
                foreach (var p in Process.GetProcesses())
                {
                    try
                    {
                        if (p.Id == currentId) continue;
                        foreach (ProcessModule module in p.Modules)
                        {
                            if (module.ModuleName.Equals(DLL_NAME, StringComparison.OrdinalIgnoreCase))
                            {
                                Debug.WriteLine($"Found {DLL_NAME} in {p.ProcessName} (PID: {p.Id})");
                                return true;
                            }
                        }
                    }
                    catch { continue; }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"IsUn0DllLoaded error: {ex.Message}");
            }
            return false;
        }

        public static bool IsUn0Running()
        {
            return IsAnyUn0ProcessRunning() || IsUn0DllLoaded();
        }

        public static List<string> GetProcessesWithUn0Dll()
        {
            var result = new List<string>();
            try
            {
                int currentId = Process.GetCurrentProcess().Id;
                foreach (var p in Process.GetProcesses())
                {
                    try
                    {
                        if (p.Id == currentId) continue;
                        foreach (ProcessModule module in p.Modules)
                        {
                            if (module.ModuleName.Equals(DLL_NAME, StringComparison.OrdinalIgnoreCase))
                            {
                                result.Add($"{p.ProcessName} (PID: {p.Id}) - {module.FileName}");
                                break;
                            }
                        }
                    }
                    catch { continue; }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"GetProcessesWithUn0Dll error: {ex.Message}");
            }
            return result;
        }
    }
}