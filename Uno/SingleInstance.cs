using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace Un0
{
    public sealed class SingleInstance
    {
        private const string PROCESS_NAME = "Un0";
        private const string DLL_NAME = "Un0.dll";

        public static bool AlreadyRunning()
        {
            bool running = false;
            try
            {
                // Get the current process
                Process currentProcess = Process.GetCurrentProcess();
                string currentProcessName = currentProcess.ProcessName;

                // Check all processes for any "Un0" process
                var processes = Process.GetProcesses();

                foreach (var p in processes)
                {
                    try
                    {
                        // Skip current process
                        if (p.Id == currentProcess.Id)
                            continue;

                        // Check if process name contains "Un0" or matches exactly
                        bool isUn0Process = p.ProcessName.Equals("Un0", StringComparison.OrdinalIgnoreCase) ||
                                           p.ProcessName.Equals("Un0.exe", StringComparison.OrdinalIgnoreCase) ||
                                           p.ProcessName.Contains("Un0", StringComparison.OrdinalIgnoreCase);

                        if (isUn0Process)
                        {
                            running = true;
                            IntPtr hFound = p.MainWindowHandle;

                            // Try to bring existing window to foreground
                            if (hFound != IntPtr.Zero)
                            {
                                if (User32API.IsIconic(hFound))
                                    User32API.ShowWindow(hFound, User32API.SW_RESTORE);
                                User32API.SetForegroundWindow(hFound);
                            }
                            else
                            {
                                // Try to find any window belonging to this process
                                try
                                {
                                    var mainWindow = p.MainWindowHandle;
                                    if (mainWindow != IntPtr.Zero)
                                    {
                                        User32API.SetForegroundWindow(mainWindow);
                                    }
                                }
                                catch { }
                            }
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error checking process {p.ProcessName}: {ex.Message}");
                        continue;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"SingleInstance check error: {ex.Message}");
            }
            return running;
        }

        public static bool IsProcessRunning(string processName)
        {
            try
            {
                return Process.GetProcessesByName(processName).Length > 0;
            }
            catch
            {
                return false;
            }
        }

        public static bool IsAnyUn0ProcessRunning()
        {
            try
            {
                var processes = Process.GetProcesses();
                return processes.Any(p =>
                {
                    try
                    {
                        return p.ProcessName.Equals("Un0", StringComparison.OrdinalIgnoreCase) ||
                               p.ProcessName.Equals("Un0.exe", StringComparison.OrdinalIgnoreCase) ||
                               p.ProcessName.Contains("Un0", StringComparison.OrdinalIgnoreCase);
                    }
                    catch
                    {
                        return false;
                    }
                });
            }
            catch
            {
                return false;
            }
        }

        // NEW: Check if Un0.dll is loaded in any process
        public static bool IsUn0DllLoaded()
        {
            try
            {
                var processes = Process.GetProcesses();

                foreach (var p in processes)
                {
                    try
                    {
                        // Skip current process if you want to check other processes only
                        if (p.Id == Process.GetCurrentProcess().Id)
                            continue;

                        // Check if the process has loaded Un0.dll
                        foreach (ProcessModule module in p.Modules)
                        {
                            if (module.ModuleName.Equals(DLL_NAME, StringComparison.OrdinalIgnoreCase))
                            {
                                Debug.WriteLine($"Found {DLL_NAME} loaded in process: {p.ProcessName} (PID: {p.Id})");
                                return true;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // Access denied for some processes, skip them
                        Debug.WriteLine($"Cannot access modules for process {p.ProcessName}: {ex.Message}");
                        continue;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"IsUn0DllLoaded error: {ex.Message}");
            }
            return false;
        }

        // NEW: Comprehensive check - process or DLL
        public static bool IsUn0Running()
        {
            try
            {
                // Check for any Un0 process
                bool processFound = IsAnyUn0ProcessRunning();

                if (processFound)
                {
                    Debug.WriteLine("Un0 process found running.");
                    return true;
                }

                // Check for Un0.dll loaded in any process
                bool dllFound = IsUn0DllLoaded();

                if (dllFound)
                {
                    Debug.WriteLine("Un0.dll found loaded in a process.");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"IsUn0Running error: {ex.Message}");
                return false;
            }
        }

        // NEW: Get all processes that have Un0.dll loaded
        public static List<string> GetProcessesWithUn0Dll()
        {
            var result = new List<string>();
            try
            {
                var processes = Process.GetProcesses();

                foreach (var p in processes)
                {
                    try
                    {
                        if (p.Id == Process.GetCurrentProcess().Id)
                            continue;

                        foreach (ProcessModule module in p.Modules)
                        {
                            if (module.ModuleName.Equals(DLL_NAME, StringComparison.OrdinalIgnoreCase))
                            {
                                result.Add($"{p.ProcessName} (PID: {p.Id}) - {module.FileName}");
                                break;
                            }
                        }
                    }
                    catch
                    {
                        continue;
                    }
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