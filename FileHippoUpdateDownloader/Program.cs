using System;
using System.Collections.Generic;
using System.Net;
using System.IO;
using System.Text.RegularExpressions;

namespace FileHippoUpdateDownloader
{
    class Program
    {

        static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.WriteLine("Usage: FileHippoUpdateDownloader.exe [result-page-url]");
                Console.ReadKey(true);
                return;
            }

            string directory =
                System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "DownloadedUpdates\\");
            if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);

            string statusPage = GetPageContents(args[0]);
            Regex regexOverviewPage = new Regex(@"""(http://filehippo\.com/download_([^/]+)/(\d+/)?download/[0-9a-f]+/)""", RegexOptions.Compiled);
            Regex regexDownloadPage = new Regex(@"href=""(/download/file/[^/]+/)""", RegexOptions.Compiled);

            List<KeyValuePair<string, string>> urls = new List<KeyValuePair<string, string>>();
            foreach (Match m in regexOverviewPage.Matches(statusPage))
            {
                urls.Add(new KeyValuePair<string, string>(m.Groups[2].Value, m.Groups[1].Value));
            }

            Console.WriteLine("Number of updates: " + urls.Count);

            WebClient wc = new WebClient();
            for (int i = 0; i < urls.Count; i++)
            {
                Console.WriteLine("({1," + urls.Count.ToString().Length + "}/{2}) Getting download URL for {0}...", urls[i].Key, i + 1, urls.Count);
                string programPage = GetPageContents(urls[i].Value);
                string realUrl = GetRedirectTarget("http://www.filehippo.com" + regexDownloadPage.Match(programPage).Groups[1].Value);
                string filename = System.IO.Path.GetFileName(realUrl);
                Console.WriteLine("({0," + urls.Count.ToString().Length + "}/{1}) Downloading {2}...", i + 1, urls.Count, filename);
                wc.DownloadFile(realUrl, System.IO.Path.Combine(directory, filename));
            }
            Console.WriteLine("Done!");
            Console.ReadKey(true);
        }

        static string GetPageContents(string url)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            return new StreamReader(response.GetResponseStream()).ReadToEnd();
        }

        static string GetRedirectTarget(string url)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.AllowAutoRedirect = false;
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            return response.Headers.Get("Location");
        }
    }
}
