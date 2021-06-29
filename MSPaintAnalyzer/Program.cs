using System;
using System.Collections.Generic;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Threading;
using System.Linq;
using System.Net.Http;
using System.Runtime.Serialization.Formatters.Binary;
using System.Diagnostics;

//https://stackoverflow.com/questions/11779143/how-do-i-run-a-python-script-from-c
namespace MSPaintAnalyzer
{
    class Program
    {
        static string subscriptionKey = "subscriptionKey";
        static string endpoint = "endpoint";
        private const string FILEPATH = @"imagePath";
        private const string folderPath = @"folderPath";
        static string uriBase = endpoint + "vision/v2.1/ocr";
        private static string outputPath = @"outputPath";
        static async Task Main(string[] args)
        {
            var images = Directory.GetFiles(folderPath);
            while (true)
            {
                foreach (var img in images)
                {
                    ComputerVisionClient client = Authenticate(endpoint, subscriptionKey);
                    OCRLocal(client, img).Wait();
                    RunPython(outputPath);
                }
            }
        }


        public static ComputerVisionClient Authenticate(string endpoint, string key)
        {
            ComputerVisionClient client =
              new ComputerVisionClient(new ApiKeyServiceClientCredentials(key))
              { Endpoint = endpoint };
            return client;
        }

        public static void RunPython(string filepath)
        {
            ProcessStartInfo start = new ProcessStartInfo(@"C:\Python38\python.exe", filepath)
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using(Process process = Process.Start(start))
            {
                using(StreamReader reader = process.StandardOutput)
                {
                    string result = reader.ReadToEnd();
                    Console.WriteLine(result);
                }
            }
        }
        public static async Task OCRLocal(ComputerVisionClient client, string imgPath)
        {
            var textHeaders = await client.ReadInStreamAsync(File.OpenRead(imgPath), language: "en");
            string operationLocation = textHeaders.OperationLocation;
            Thread.Sleep(2000);

            const int numberOfCharsInOperationId = 36;
            string operationId = operationLocation.Substring(operationLocation.Length - numberOfCharsInOperationId);

            ReadOperationResult results;
            do
            {
                results = await client.GetReadResultAsync(Guid.Parse(operationId));
            }
            while ((results.Status == OperationStatusCodes.Running || results.Status == OperationStatusCodes.NotStarted));

            Console.WriteLine();
            var textResults = results.AnalyzeResult.ReadResults;
            List<string> addLines = new List<string>();
            foreach(ReadResult page in textResults)
            {
                foreach (Line line in page.Lines)
                {
                    Console.WriteLine("Input code: " + line.Text);
                    addLines.Append(line.Text);
                    await File.WriteAllTextAsync(outputPath+"output.py", line.Text);
                }
            }
            
            Console.WriteLine();
        }

    }
}
