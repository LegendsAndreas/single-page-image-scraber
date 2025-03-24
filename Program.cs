using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using HtmlAgilityPack;

// https://www.dansklimousine.dk/galleri/billedegalleri/kimbrerskuet.htm
// https://www.dansklimousine.dk/galleri/billedegalleri/kimbrerskuet.htm#gallery-1

namespace ImageScraber;

class Program
{
    static async Task Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Usage: ImageScraber.exe <URL> <Option> <Download Folder>");
            return;
        }

        string url = args[0];

        string nodeElement = "";
        string nodeAttribute = "";

        if (args[1] == "a")
        {
            nodeElement = "a";
            nodeAttribute = "href";
        }
        else if (args[1] == "img")
        {
            nodeElement = "img";
            nodeAttribute = "src";
        }
        else
        {
            Console.WriteLine("Option has to be either \"img\" or \"a\". Cant be: " + args[1]);
            return;
        }

        string downloadFolder = Path.Combine(Environment.CurrentDirectory, "DownloadedImages");
        try
        {
            downloadFolder = Path.Combine(downloadFolder, args[2]);
        }
        catch (IndexOutOfRangeException ex)
        {
            Console.WriteLine("No download folder specified.");
        }
        catch (Exception ex)
        {
            Console.WriteLine("Unknown error: " + ex);
            throw;
        }

        Directory.CreateDirectory(downloadFolder);

        HttpClient client = new HttpClient();
        string html = "";
        try
        {
            html = await client.GetStringAsync(url);
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine("Error: " + ex.Message);
            return;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Unhandled Error: " + ex);
            return;
        }

        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var imgNodes = doc.DocumentNode.SelectNodes("//" + nodeElement + "[@" + nodeAttribute + "]");

        if (imgNodes == null)
        {
            Console.WriteLine("No images found.");
            return;
        }

        int count = 0;
        foreach (var img in imgNodes)
        {
            string imgUrl = img.GetAttributeValue(nodeAttribute, "");
            if (string.IsNullOrEmpty(imgUrl))
                continue;

            if (!imgUrl.Contains(".jpg") && !imgUrl.Contains(".png") && !imgUrl.Contains(".jpeg") &&
                !imgUrl.Contains(".webp"))
                continue;

            // Handle relative URLs
            if (!imgUrl.StartsWith("http"))
            {
                Uri baseUri = new Uri(url);
                imgUrl = new Uri(baseUri, imgUrl).ToString();
            }

            string fileName = Path.GetFileName(new Uri(imgUrl).LocalPath);
            string filePath = Path.Combine(downloadFolder, fileName);

            using HttpClient httpClient = new HttpClient();
            try
            {
                var response = await httpClient.GetAsync(imgUrl);
                response.EnsureSuccessStatusCode();

                await using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
                await response.Content.CopyToAsync(fs);

                Console.WriteLine($"Downloaded: {fileName}");
                count++;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to download {imgUrl}: {ex.Message}");
            }
        }

        Console.WriteLine($"Finished. Downloaded {count} images to {downloadFolder}");
    }
}