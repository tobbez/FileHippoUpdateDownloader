using System;
using System.Collections.Generic;
using System.Net;
using System.IO;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

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

            
            List<DownloadDescription> downloadDescriptions = GetDownloadDescriptions(statusPage);

            Console.WriteLine("Number of updates: " + downloadDescriptions.Count);

            foreach (DownloadDescription d in downloadDescriptions)
            {
                DownloadUsingDescription(d, directory);
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

        struct DownloadDescription
        {
            public string program;
            public string installedVersion;
            public string installPath;
            public Int64 fileSize;
            public string iconUrl;
            public string downloadPageLink;

            override public string ToString()
            {
                return program;
            }
        }

        static List<DownloadDescription> GetDownloadDescriptions(string html)
        {
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(html);

            List<DownloadDescription> results = new List<DownloadDescription>();

            foreach (HtmlNode n in doc.DocumentNode.SelectNodes("//ul[@class='result-list']//li[not(contains(@class, 'installed')) and not(contains(@class, 'beta'))]"))
            {
                DownloadDescription d;
                d.program = n.SelectSingleNode(".//span[@class='program-name']").InnerText.Trim();
                d.installedVersion = n.SelectSingleNode(".//span[@class='installed-version']").InnerText.Trim();
                d.installPath = n.SelectSingleNode(".//span[@class='install-path']").InnerText.Trim();
                d.fileSize = Int64.Parse(n.GetAttributeValue("data-filesize", "-1"));
                d.iconUrl = n.SelectSingleNode(".//span[@class='program-name']//img").GetAttributeValue("src", "");
                d.downloadPageLink = n.SelectSingleNode(".//a[@class='update-download-link']").GetAttributeValue("href", "");
                results.Add(d);
            }

            return results;
        }

        /// <summary>
        /// Downloads the file described by the DownloadDescription into the specified directory.
        /// </summary>
        /// <param name="d">A DownloadDescription for the file to download.</param>
        /// <param name="directory">The directory the file will be downloaded into.</param>
        static void DownloadUsingDescription(DownloadDescription d, string directory)
        {
            Regex regexDownloadPage = new Regex(@"href=""(/download/file/[^/]+/)""", RegexOptions.Compiled);
            WebClient wc = new WebClient();

            Console.Write("Getting download link for {0}...", d.program);
            string downloadPage = GetPageContents(d.downloadPageLink);
            string downloadUrl = GetRedirectTarget("http://filehippo.com" + regexDownloadPage.Match(downloadPage).Groups[1].Value);
            string filename = System.IO.Path.GetFileName(downloadUrl);

            Console.Write("\r                                                                                       ");
            Console.Write("\rDownloading {0} ({1}K)... ", d.program, d.fileSize/1024);
            wc.DownloadFile(downloadUrl, System.IO.Path.Combine(directory, filename.Split('?')[0]));
            Console.WriteLine("Complete");
        }
    }
}
