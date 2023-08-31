using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace ImageUtil
{
    public class KumikoPanelInfo
    {
        public string filename { get; set; }

        public int[] size { get; set; }

        public int[][] panels { get; set; }
    }

    public class PanelInfo
    {
        public int TopLeftX { get; set; }

        public int TopLeftY { get; set; }

        public int Width { get; set; }

        public int Height { get; set; }
    }

    public class KumikoWrapper
    {
        private readonly string kumikopath;

        public KumikoWrapper(string kumikopath)
        {
            this.kumikopath = kumikopath;
        }

        public async Task<IEnumerable<PanelInfo>> GetImagePanels(string imagepath)
        {
            string outputfile = System.IO.Path.GetTempFileName().Replace(".tmp", ".json");

            var sb = new StringBuilder();

            using (Process p = new Process())
            {
                p.StartInfo.WorkingDirectory = kumikopath;
                p.StartInfo.FileName = "python";
                p.StartInfo.Arguments = $"kumiko -i {imagepath} -o \"{outputfile}\"";
                p.StartInfo.UseShellExecute = true;

                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.RedirectStandardError = true;

                // hookup the eventhandlers to capture the data that is received
                p.OutputDataReceived += (sender, args) => sb.AppendLine(args.Data);
                p.ErrorDataReceived += (sender, args) => sb.AppendLine(args.Data);

                // direct start
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.CreateNoWindow = true;
                p.Start();
                // start our event pumps
                p.BeginOutputReadLine();
                p.BeginErrorReadLine();

                await p.WaitForExitAsync();

                var output = sb.ToString();

                using (StreamReader r = new StreamReader(outputfile))
                {
                    string json = r.ReadToEnd();
                    List<KumikoPanelInfo>? panels = JsonConvert.DeserializeObject<List<KumikoPanelInfo>>(json);

                    if (panels != null)
                    {
                        return panels.First().panels.Select(paneldims => new PanelInfo
                        {
                            TopLeftX = paneldims[0],
                            TopLeftY = paneldims[1],
                            Width = paneldims[2],
                            Height = paneldims[3]
                        });
                    }

                    throw new Exception("Panel Extraction Failed.");
                }
            }
        }
    }
}