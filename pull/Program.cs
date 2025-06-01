using System.Diagnostics;
using System.Net;
using System.Security;
using System.Text;

namespace pull
{
    static class Program
    {
        private static void Println(string content)
        {
            Console.WriteLine("[" + DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") + "]" + content);
        }

        private static string PullDirectListText()
        {
#pragma warning disable IDE0090 // 使用 "new(...)"
            using WebClient wc = new WebClient();
#pragma warning restore IDE0090 // 使用 "new(...)"

            foreach (string url in new string[] { "https://raw.githubusercontent.com/Loyalsoldier/v2ray-rules-dat/release/direct-list.txt", "https://cdn.jsdelivr.net/gh/Loyalsoldier/v2ray-rules-dat@release/direct-list.txt" })
            {
                string texts;
                try
                {
                    texts = wc.DownloadString(url);
                }
                catch (Exception)
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(texts))
                {
                    return texts;
                }
            }

            return string.Empty;
        }

        private static bool PullDirectListToLocalFile(string texts, string path, string placeholders, out int events)
        {
            events = 0;
            if (string.IsNullOrEmpty(texts))
            {
                return false;
            }

            ISet<string> set = new HashSet<string>();
            IList<string> list = new List<string>();
            int maxLineLength = 0;

            foreach (string line in texts.Split('\r', '\n'))
            {
                if (string.IsNullOrEmpty(line))
                {
                    continue;
                }

                if (set.Add(line))
                {
                    list.Add(line);
                    maxLineLength = Math.Max(maxLineLength, line.Length);
                }
            }

            if (list.Count < 1)
            {
                return false;
            }

            StringBuilder content = new StringBuilder();
            maxLineLength++;

            foreach (string i in list)
            {
                string segment = i.PadRight(maxLineLength, ' ') + placeholders;
                if (content.Length > 0)
                {
                    segment = "\r\n" + segment;
                }

                events++;
                content.Append(segment);
            }

            try
            {
                File.WriteAllText(path, content.ToString(), Encoding.Default);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static void PullDirectList(string path, string placeholders)
        {
            Println("PULLING");
            for (; ; )
            {
                string texts = PullDirectListText();
                Println("PROCESSING");
                Println("PULLED STATUS: " + (PullDirectListToLocalFile(texts, path, placeholders, out int events) ? "OK" : "ER") + ", EVENTS: " + events);
                break;
            }

            Println("PUSHING");
            Git("status");
            Git("add .");
            Git("commit -m \"sync.\"");
            Git(@"push -f");
            Println("PUSHED");
        }

        private static bool Git(string arguments)
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = arguments,
                WorkingDirectory = Environment.CurrentDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process();
            try
            {
                process.StartInfo = processInfo;
                process.OutputDataReceived +=
                    (sender, e) =>
                {
                    if (e.Data != null)
                    {
                        Console.WriteLine(e.Data);
                    }
                };

                process.ErrorDataReceived +=
                    (sender, e) =>
                {
                    if (e.Data != null)
                    {
                        Console.WriteLine("ERROR: " + e.Data);
                    }
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        [STAThread]
        [SecurityCritical]
        static void Main(string[] args)
        {
            string placeholders = string.Empty;
            string path = string.Empty;
            int interval = 0;

            if (args.Length > 0)
            {
                path = args[0].TrimStart().TrimEnd();
            }
            else if (args.Length > 1)
            {
                placeholders = args[1].Trim();
            }
            else if (args.Length > 2)
            {
                if (!int.TryParse(args[2].Trim(), out interval))
                {
                    interval = 0;
                }
            }

            if (interval < 1)
            {
                interval = 3600;
            }

            placeholders = placeholders.Trim();
            if (string.IsNullOrEmpty(path))
            {
                path = "./dns-rules.txt";
            }

            path = Path.GetFullPath(path);
            if (string.IsNullOrEmpty(placeholders))
            {
                placeholders = "/223.5.5.5/nic";
            }

            Console.Title = "PPP PRIVATE NETWORK™ 2 AUTOMATIC PULLING GEOSITE:DIRECT-LIST";
            interval = (int)Math.Min(interval * 1000L, int.MaxValue);

            for (; ; )
            {
                Stopwatch sw = Stopwatch.StartNew();
                sw.Start();
                PullDirectList(path, placeholders);
                sw.Stop();
                Thread.Sleep((int)(interval - sw.ElapsedMilliseconds));
            }
        }
    }
}