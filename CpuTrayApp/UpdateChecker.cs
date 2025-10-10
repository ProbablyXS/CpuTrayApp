using Newtonsoft.Json.Linq;
using System;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using CpuTrayApp.Properties; // pour accéder à Strings

namespace CpuTrayApp
{
    public static class UpdateChecker
    {
        private const string ApiLatestReleaseUrl = "https://api.github.com/repos/ProbablyXS/CpuTrayApp/releases/latest";
        private const string ReleasePageUrl = "https://github.com/ProbablyXS/CpuTrayApp/releases/latest";

        public static async Task CheckForUpdatesAsync()
        {
            try
            {
                using (WebClient client = new WebClient())
                {
                    client.Headers.Add("User-Agent", "CpuTrayApp");

                    string json = await client.DownloadStringTaskAsync(ApiLatestReleaseUrl);
                    JObject obj = JObject.Parse(json);
                    string latestTag = (string)obj["tag_name"];

                    string latestVersionStr = latestTag.TrimStart('v', 'V');
                    string currentVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();

                    Version latestVer;
                    Version curVer;

                    if (Version.TryParse(latestVersionStr, out latestVer) &&
                        Version.TryParse(currentVersion, out curVer) &&
                        latestVer > curVer)
                    {
                        string message = string.Format(
                            Strings.Update_NewVersionAvailable,
                            latestTag,
                            currentVersion
                        );

                        DialogResult result = MessageBox.Show(
                            message,
                            Strings.Update_Title,
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Information);

                        if (result == DialogResult.Yes)
                            System.Diagnostics.Process.Start(ReleasePageUrl);
                    }
                }
            }
            catch
            {
#if DEBUG
                MessageBox.Show(Strings.Update_Error, Strings.Update_Title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
#endif
            }
        }
    }
}
