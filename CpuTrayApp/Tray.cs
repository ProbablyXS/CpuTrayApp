using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace CpuTrayApp
{
    internal class Tray
    {
        // GUID for controlling the maximum CPU processor state
        private const string MaxProcessorStateGuid = "bc5038f7-23e0-4960-96da-33abaf5935ec";

        private ContextMenuStrip trayMenu;
        private NotifyIcon trayIcon;
        private ToolStripMenuItem plansMenu;
        private ToolStripMenuItem cpuMenu;
        private ToolStripMenuItem cpuGhzMenu;

        private Dictionary<string, string> powerPlans;

        public void InitializeTray()
        {
            trayMenu = new ContextMenuStrip();

            // --- Power Plans Menu ---
            plansMenu = new ToolStripMenuItem("Power plans");
            powerPlans = GetPowerPlans();

            // Add each power plan to the dropdown
            foreach (var plan in powerPlans)
            {
                var item = new ToolStripMenuItem(plan.Value) { Tag = plan.Key };
                item.Click += (s, e) =>
                {
                    string planGuid = ((ToolStripMenuItem)s).Tag.ToString();
                    SelectPlan(planGuid);
                };
                plansMenu.DropDownItems.Add(item);
            }
            trayMenu.Items.Add(plansMenu);

            // --- CPU Limit Menu ---
            InitializeCpuMenu();

            // --- CPU GHz Display (read-only) ---
            cpuGhzMenu = new ToolStripMenuItem("CPU: ... GHz") { Enabled = false };
            trayMenu.Items.Add(cpuGhzMenu);

            // --- Exit Menu Item ---
            trayMenu.Items.Add("Exit", null, (s, e) => Application.Exit());

            // --- System Tray Icon ---
            trayIcon = new NotifyIcon
            {
                Icon = Properties.Resources.favicon,
                ContextMenuStrip = trayMenu,
                Visible = true,
                Text = "CPU Power Manager"
            };

            // --- Update menus when the user opens the tray menu ---
            trayMenu.Opening += (s, e) =>
            {
                UpdateActivePlanMenu();
                UpdateCpuMenuSelection(GetCurrentCpuLimit());
                UpdateCpuGhz();
            };

            // --- Optionally update CPU speed when hovering over the icon ---
            trayIcon.MouseMove += (s, e) =>
            {
                UpdateCpuGhz();
                MemoryCleaner.CleanCurrentProcessMemory();
            };

            // --- Initial update at launch ---
            UpdateActivePlanMenu();
            UpdateCpuMenuSelection(GetCurrentCpuLimit());
            UpdateCpuGhz();

            // --- Clean memory ---
            MemoryCleaner.CleanCurrentProcessMemory();
        }

        private void InitializeCpuMenu()
        {
            cpuMenu = new ToolStripMenuItem("CPU limit (%)");

            // Create menu items from 10% to 100%
            for (int i = 10; i <= 100; i += 10)
            {
                var item = new ToolStripMenuItem(i.ToString())
                {
                    Tag = i,
                    CheckOnClick = true
                };
                item.Click += (s, e) =>
                {
                    int percent = (int)((ToolStripMenuItem)s).Tag;
                    SetCpuLimit(percent);
                };
                cpuMenu.DropDownItems.Add(item);
            }

            trayMenu.Items.Add(cpuMenu);
        }

        // Get all available power plans on the system (language independent)
        private Dictionary<string, string> GetPowerPlans()
        {
            var plans = new Dictionary<string, string>();
            string output = RunPowerCfg("/L");

            // Regex pour détecter un GUID
            Regex guidRegex = new Regex(@"([a-fA-F0-9]{8}-([a-fA-F0-9]{4}-){3}[a-fA-F0-9]{12})");

            foreach (var line in output.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var match = guidRegex.Match(line);
                if (match.Success)
                {
                    string guid = match.Groups[1].Value;

                    // Le nom est la partie après le GUID dans la ligne, on va la récupérer
                    int guidIndex = line.IndexOf(guid) + guid.Length;
                    string name = line.Substring(guidIndex).Trim();

                    // Nettoyer le nom en enlevant les éventuels symboles * ou parenthèses autour
                    name = name.Trim(new char[] { '*', ' ', '(', ')' });

                    plans[guid] = name;
                }
            }

            return plans;
        }

        // Update the checkmarks to show the currently active plan
        private void UpdateActivePlanMenu()
        {
            string activeGuid = GetActivePlanGuid();
            foreach (ToolStripMenuItem item in plansMenu.DropDownItems)
            {
                item.Checked = item.Tag.ToString() == activeGuid;
            }
        }

        // Fetch the GUID of the currently active power plan (language independent)
        private string GetActivePlanGuid()
        {
            string output = RunPowerCfg("/L");
            Regex guidRegex = new Regex(@"([a-fA-F0-9]{8}-([a-fA-F0-9]{4}-){3}[a-fA-F0-9]{12})");

            foreach (var line in output.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (line.Contains("*"))
                {
                    var match = guidRegex.Match(line);
                    if (match.Success)
                    {
                        return match.Groups[1].Value;
                    }
                }
            }
            return null;
        }

        // Read the current CPU limit from the active power plan
        private int GetCurrentCpuLimit()
        {
            string activePlan = GetActivePlanGuid();
            if (string.IsNullOrEmpty(activePlan)) return -1;

            string output = RunPowerCfg($"/query {activePlan} SUB_PROCESSOR {MaxProcessorStateGuid}");
            foreach (var line in output.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries))
            {
                // Le texte ici peut être localisé, donc on recherche la ligne qui contient "Current AC Power Setting Index" en insensible à la casse
                if (line.Trim().ToLower().Contains("current ac power setting index"))
                {
                    string value = line.Split(':')[1].Trim();
                    if (value.StartsWith("0x"))
                        return Convert.ToInt32(value, 16);
                    else if (int.TryParse(value, out int percent))
                        return percent;
                }
            }
            return -1;
        }

        // Change the CPU limit to a specific percentage
        private void SetCpuLimit(int percent)
        {
            string activePlan = GetActivePlanGuid();
            if (string.IsNullOrEmpty(activePlan)) return;

            RunPowerCfg($"-setacvalueindex SCHEME_CURRENT SUB_PROCESSOR {MaxProcessorStateGuid} {percent}");
            RunPowerCfg($"-setactive {activePlan}");
            UpdateCpuMenuSelection(percent);
        }

        // Highlight the selected CPU limit in the menu
        private void UpdateCpuMenuSelection(int activePercent)
        {
            foreach (ToolStripMenuItem item in cpuMenu.DropDownItems)
            {
                item.Checked = (int)item.Tag == activePercent;
            }
        }

        // Read the CPU speed and update the tray icon
        private void UpdateCpuGhz()
        {
            try
            {
                var searcher = new ManagementObjectSearcher("SELECT CurrentClockSpeed, MaxClockSpeed FROM Win32_Processor");
                var item = searcher.Get().Cast<ManagementObject>().FirstOrDefault();
                if (item == null) return;

                double currentGhz = Math.Round(Convert.ToUInt32(item["CurrentClockSpeed"]) / 1000.0, 2);
                double maxGhz = Math.Round(Convert.ToUInt32(item["MaxClockSpeed"]) / 1000.0, 2);

                string text = $"CPU: {currentGhz} GHz / {maxGhz} GHz";
                cpuGhzMenu.Text = text;
                trayIcon.Text = $"CPU Power Manager - {text}";
            }
            catch
            {
                cpuGhzMenu.Text = "CPU: N/A";
                trayIcon.Text = "CPU Power Manager - CPU: N/A";
            }
        }

        // Activate a power plan by GUID
        private void SelectPlan(string planGuid)
        {
            RunPowerCfg($"-setactive {planGuid}");
            UpdateActivePlanMenu();
        }

        // Run a PowerCfg command and return its output
        private string RunPowerCfg(string args)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "powercfg",
                    Arguments = args,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            return output;
        }
    }
}
