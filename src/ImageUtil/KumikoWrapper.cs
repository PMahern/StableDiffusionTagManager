﻿using Newtonsoft.Json;
using System.Diagnostics;
using System.Text;

namespace ImageUtil
{
    public class KumikoPanelInfo
    {
        public string filename { get; set; } = "";

        public int[] size { get; set; } = new int[0];

        public int[][] panels { get; set; } = new int[0][];
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
        private readonly string pythonpath;
        private readonly string kumikopath;

        public KumikoWrapper(string pythonpath, string kumikopath)
        {
            this.pythonpath = pythonpath;
            this.kumikopath = kumikopath;
        }

        public async Task<IEnumerable<PanelInfo>> GetImagePanels(string imagepath, string tempfilepath)
        {
            string outputfile = Path.Combine(tempfilepath, $"{Guid.NewGuid().ToString()}.json");

            var sb = new StringBuilder();

            using (Process p = new Process())
            {
                p.StartInfo.WorkingDirectory = kumikopath;
                p.StartInfo.FileName = pythonpath;
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

                try
                {
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

                        throw new Exception($"Panel Extraction Failed. No panels were found in the output file.\n\r Console output from kumiko execution was: {output}");
                    }
                } catch (Exception)
                {
                    throw new Exception($"Error occured reading kumiko output file.\n\r Console output from kumiko execution was:\n\r {output}");
                }
            }
        }
    }
}