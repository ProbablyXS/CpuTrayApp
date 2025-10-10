using CpuTrayApp.Properties;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace CpuTrayApp
{
    internal class Tray
    {
        private const string MaxProcessorStateGuid = "bc5038f7-23e0-4960-96da-33abaf5935ec";
        private const string MinProcessorStateGuid = "893dee8e-2bef-41e0-89c6-b55d0929964c";

        private ContextMenuStrip trayMenu;
        private NotifyIcon trayIcon;
        private ToolStripMenuItem plansMenu;
        private ToolStripMenuItem cpuMinMenu;
        private ToolStripMenuItem cpuMaxMenu;
        private ToolStripMenuItem cpuGhzMenu;

        private Dictionary<string, string> powerPlans;
        private System.Windows.Forms.Timer cpuUpdateTimer;
        private readonly SynchronizationContext uiContext;

        public Tray()
        {
            uiContext = SynchronizationContext.Current ?? new SynchronizationContext();
        }

        public void InitializeTray()
        {
            _ = UpdateChecker.CheckForUpdatesAsync();

            trayMenu = new ContextMenuStrip();

            // --- Power Plans Menu ---
            plansMenu = new ToolStripMenuItem(Strings.Menu_PowerPlans);
            powerPlans = GetPowerPlans();

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

            // --- CPU Min and Max Limit Menus ---
            InitializeCpuMinMenu();
            InitializeCpuMaxMenu();

            // --- CPU GHz Display (read-only) ---
            cpuGhzMenu = new ToolStripMenuItem(Strings.Menu_CpuSpeed + ": ...") { Enabled = false };
            trayMenu.Items.Add(cpuGhzMenu);

            // --- Exit Menu Item ---
            trayMenu.Items.Add(Strings.Menu_Exit, null, (s, e) => Application.Exit());

            // --- Tray Icon ---
            trayIcon = new NotifyIcon
            {
                Icon = Properties.Resources.favicon,
                ContextMenuStrip = trayMenu,
                Visible = true,
                Text = Strings.Tray_Title
            };

            trayMenu.Opening += (s, e) =>
            {
                UpdateActivePlanMenu();
                UpdateCpuMinMenuSelection(GetCurrentCpuMinLimit());
                UpdateCpuMaxMenuSelection(GetCurrentCpuMaxLimit());
                _ = UpdateCpuGhzAsync();
            };

            trayIcon.MouseMove += async (s, e) =>
            {
                await UpdateCpuGhzAsync();
                MemoryCleaner.CleanCurrentProcessMemory();
            };

            Application.ApplicationExit += (s, e) => trayIcon.Dispose();

            // --- Timer for GHz refresh ---
            cpuUpdateTimer = new System.Windows.Forms.Timer { Interval = 5000 };
            cpuUpdateTimer.Tick += async (s, e) => await UpdateCpuGhzAsync();
            cpuUpdateTimer.Start();

            // --- Initial Update ---
            UpdateActivePlanMenu();
            UpdateCpuMinMenuSelection(GetCurrentCpuMinLimit());
            UpdateCpuMaxMenuSelection(GetCurrentCpuMaxLimit());
            _ = UpdateCpuGhzAsync();

            MemoryCleaner.CleanCurrentProcessMemory();
        }

        private Dictionary<string, string> GetPowerPlans()
        {
            var plans = new Dictionary<string, string>();
            string output = RunPowerCfg("/L");

            Regex guidRegex = new Regex(@"([a-fA-F0-9]{8}-([a-fA-F0-9]{4}-){3}[a-fA-F0-9]{12})");

            foreach (var line in output.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var match = guidRegex.Match(line);
                if (match.Success)
                {
                    string guid = match.Groups[1].Value;
                    int guidIndex = line.IndexOf(guid) + guid.Length;
                    string name = line.Substring(guidIndex).Trim();
                    name = name.Trim(new char[] { '*', ' ', '(', ')' });
                    plans[guid] = name;
                }
            }

            return plans;
        }

        private void InitializeCpuMinMenu()
        {
            cpuMinMenu = new ToolStripMenuItem(Strings.Menu_MinCpuLimit);
            for (int i = 0; i <= 100; i += 10)
            {
                var item = new ToolStripMenuItem(i.ToString())
                {
                    Tag = i,
                    CheckOnClick = true
                };
                item.Click += (s, e) =>
                {
                    int percent = (int)((ToolStripMenuItem)s).Tag;
                    SetCpuMinLimit(percent);
                };
                cpuMinMenu.DropDownItems.Add(item);
            }
            trayMenu.Items.Add(cpuMinMenu);
        }

        private void InitializeCpuMaxMenu()
        {
            cpuMaxMenu = new ToolStripMenuItem(Strings.Menu_MaxCpuLimit);
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
                    SetCpuMaxLimit(percent);
                };
                cpuMaxMenu.DropDownItems.Add(item);
            }
            trayMenu.Items.Add(cpuMaxMenu);
        }

        private void SetCpuMinLimit(int percent)
        {
            string activePlan = GetActivePlanGuid();
            if (string.IsNullOrEmpty(activePlan)) return;

            RunPowerCfg($"-setacvalueindex SCHEME_CURRENT SUB_PROCESSOR {MinProcessorStateGuid} {percent}");
            RunPowerCfg($"-setactive {activePlan}");
            UpdateCpuMinMenuSelection(percent);
        }

        private void SetCpuMaxLimit(int percent)
        {
            string activePlan = GetActivePlanGuid();
            if (string.IsNullOrEmpty(activePlan)) return;

            RunPowerCfg($"-setacvalueindex SCHEME_CURRENT SUB_PROCESSOR {MaxProcessorStateGuid} {percent}");
            RunPowerCfg($"-setactive {activePlan}");
            UpdateCpuMaxMenuSelection(percent);
        }

        private int GetCurrentCpuMinLimit() => GetCpuLimitFromPlan(MinProcessorStateGuid);
        private int GetCurrentCpuMaxLimit() => GetCpuLimitFromPlan(MaxProcessorStateGuid);

        private int GetCpuLimitFromPlan(string guid)
        {
            string activePlan = GetActivePlanGuid();
            if (string.IsNullOrEmpty(activePlan)) return -1;

            string output = RunPowerCfg($"/query {activePlan} SUB_PROCESSOR {guid}");
            foreach (var line in output.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries))
            {
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

        private void UpdateCpuMinMenuSelection(int activePercent)
        {
            foreach (ToolStripMenuItem item in cpuMinMenu.DropDownItems)
                item.Checked = (int)item.Tag == activePercent;
        }

        private void UpdateCpuMaxMenuSelection(int activePercent)
        {
            foreach (ToolStripMenuItem item in cpuMaxMenu.DropDownItems)
                item.Checked = (int)item.Tag == activePercent;
        }

        private void UpdateActivePlanMenu()
        {
            string activeGuid = GetActivePlanGuid();
            foreach (ToolStripMenuItem item in plansMenu.DropDownItems)
                item.Checked = item.Tag.ToString() == activeGuid;
        }

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
                        return match.Groups[1].Value;
                }
            }
            return null;
        }

        private async Task UpdateCpuGhzAsync()
        {
            await Task.Run(() =>
            {
                try
                {
                    using (var searcher = new ManagementObjectSearcher("SELECT CurrentClockSpeed, MaxClockSpeed FROM Win32_Processor"))
                    {
                        var item = searcher.Get().Cast<ManagementObject>().FirstOrDefault();
                        if (item == null) return;

                        double currentGhz = Math.Round(Convert.ToUInt32(item["CurrentClockSpeed"]) / 1000.0, 2);
                        double maxGhz = Math.Round(Convert.ToUInt32(item["MaxClockSpeed"]) / 1000.0, 2);
                        string text = $"{Strings.Menu_CpuSpeed}: {currentGhz} GHz / {maxGhz} GHz";

                        uiContext.Post(_ =>
                        {
                            cpuGhzMenu.Text = text;
                            trayIcon.Text = $"{Strings.Tray_Title} - {text}";
                        }, null);
                    }
                }
                catch
                {
                    uiContext.Post(_ =>
                    {
                        cpuGhzMenu.Text = $"{Strings.Menu_CpuSpeed}: N/A";
                        trayIcon.Text = $"{Strings.Tray_Title} - {Strings.Menu_CpuSpeed}: N/A";
                    }, null);
                }
            });
        }

        private void SelectPlan(string planGuid)
        {
            RunPowerCfg($"-setactive {planGuid}");
            UpdateActivePlanMenu();
        }

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
