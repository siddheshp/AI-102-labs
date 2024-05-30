using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Threading.Tasks;

// import namespaces
using Azure;
using Azure.AI.TextAnalytics;

namespace custom_entities
{
    class Program
    {
        static async Task Main(string[] args)
        {
            try
            {
                // Get config settings from AppSettings
                IConfigurationBuilder builder = new ConfigurationBuilder().AddJsonFile("appsettings.json");
                IConfigurationRoot configuration = builder.Build();
                string aiSvcEndpoint = configuration["AIServicesEndpoint"];
                string aiSvcKey = configuration["AIServicesKey"];
                string projectName = configuration["Project"];
                string deploymentName = configuration["Deployment"];

                // Create client using endpoint and key
AzureKeyCredential credentials = new(aiSvcKey);
Uri endpoint = new(aiSvcEndpoint);
TextAnalyticsClient aiClient = new(endpoint, credentials);

                // Read each text file in the ads folder
                List<TextDocumentInput> batchedDocuments = new();              
                var folderPath = Path.GetFullPath("./ads");  
                DirectoryInfo folder = new(folderPath);
                FileInfo[] files = folder.GetFiles("*.txt");
                foreach (var file in files)
                {
                    // Read the file contents
                    StreamReader sr = file.OpenText();
                    var text = sr.ReadToEnd();
                    sr.Close();
                    TextDocumentInput doc = new(file.Name, text)
                    {
                        Language = "en",
                    };
                    batchedDocuments.Add(doc);
                }

                // Extract entities
RecognizeCustomEntitiesOperation operation = await aiClient.RecognizeCustomEntitiesAsync(WaitUntil.Completed, batchedDocuments, projectName, deploymentName);

await foreach (RecognizeCustomEntitiesResultCollection documentsInPage in operation.Value)
{
    foreach (RecognizeEntitiesResult documentResult in documentsInPage)
    {
        Console.WriteLine($"Result for \"{documentResult.Id}\":");

        if (documentResult.HasError)
        {
            Console.WriteLine($"  Error!");
            Console.WriteLine($"  Document error code: {documentResult.Error.ErrorCode}");
            Console.WriteLine($"  Message: {documentResult.Error.Message}");
            Console.WriteLine();
            continue;
        }

        Console.WriteLine($"  Recognized {documentResult.Entities.Count} entities:");

        foreach (CategorizedEntity entity in documentResult.Entities)
        {
            Console.WriteLine($"  Entity: {entity.Text}");
            Console.WriteLine($"  Category: {entity.Category}");
            Console.WriteLine($"  Offset: {entity.Offset}");
            Console.WriteLine($"  Length: {entity.Length}");
            Console.WriteLine($"  ConfidenceScore: {entity.ConfidenceScore}");
            Console.WriteLine($"  SubCategory: {entity.SubCategory}");
            Console.WriteLine();
        }

        Console.WriteLine();
    }
}

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }



    }
}
