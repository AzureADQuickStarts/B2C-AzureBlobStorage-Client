using Microsoft.WindowsAzure.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using Microsoft.WindowsAzure.Storage.Blob;
using System.Globalization;
using Microsoft.WindowsAzure.Storage.Shared.Protocol;
using System.IO;

namespace B2CAzureStorageClient
{
    class Program
    {
        private static string connectionStringTemplate = "DefaultEndpointsProtocol=https;AccountName={0};AccountKey={1}";

        static void Main(string[] args)
        {
            ConsoleColor original = Console.ForegroundColor;

            try 
            {
                // Get Info From User
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("***************** Azure AD B2C Blob Storage Helper Tool *********************");
                Console.WriteLine("This tool will upload all contents of a directory to an Azure Blob Storage Container you specify.");
                Console.WriteLine("It will also enable CORS access from all origins on each of the files.");
                Console.WriteLine("");
                Console.WriteLine("Enter your Azure Storage Account name: ");
                string storageAccountName = Console.ReadLine();
                Console.WriteLine("Enter your Azure Blob Storage Primary Access Key: ");
                string storageKey = Console.ReadLine();
                Console.WriteLine("Enter your Azure Blob Storage Container name: ");
                string containerName = Console.ReadLine();
                Console.WriteLine("Enter the path to the directory whose contents you wish to upload: ");
                string directoryPath = Console.ReadLine();


                // Upload File to Blob Storage
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(String.Format(CultureInfo.InvariantCulture, connectionStringTemplate, storageAccountName, storageKey));
                CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
                CloudBlobContainer container = blobClient.GetContainerReference(containerName);

                string absoluteDirPath = Path.GetFullPath(directoryPath);
                string[] allFiles = Directory.GetFiles(absoluteDirPath, "*.*", SearchOption.AllDirectories);
                foreach (string filePath in allFiles)
                {
                    string relativePathAndFileName = filePath.Substring(filePath.IndexOf(absoluteDirPath) + absoluteDirPath.Length);
                    relativePathAndFileName = relativePathAndFileName[0] == '\\' ? relativePathAndFileName.Substring(1) : relativePathAndFileName;
                    CloudBlockBlob blockBlob = container.GetBlockBlobReference(relativePathAndFileName);
                    blockBlob.Properties.ContentType = MapFileExtensionToContentType(relativePathAndFileName);
                    using (var fileStream = System.IO.File.OpenRead(filePath))
                    {
                        blockBlob.UploadFromStream(fileStream);
                    }

                    Console.WriteLine("Sucessfully uploaded file " + relativePathAndFileName + " to Azure Blob Storage");
                }

                // Enable CORS
                CorsProperties corsProps = new CorsProperties();
                corsProps.CorsRules.Add(new CorsRule
                {
                    AllowedHeaders = new List<string> { "*" },
                    AllowedMethods = CorsHttpMethods.Get,
                    AllowedOrigins = new List<string> { "*" },
                    ExposedHeaders = new List<string> { "*" },
                    MaxAgeInSeconds = 200
                });

                ServiceProperties serviceProps = new ServiceProperties
                {
                    Cors = corsProps,
                    Logging = new LoggingProperties
                    {
                        Version = "1.0",
                    },
                    HourMetrics = new MetricsProperties
                    {
                        Version = "1.0"
                    },
                    MinuteMetrics = new MetricsProperties
                    {
                        Version = "1.0"
                    },
                };
                blobClient.SetServiceProperties(serviceProps);

                Console.WriteLine("Successfully set CORS policy, allowing GET on all origins.  See https://msdn.microsoft.com/en-us/library/azure/dn535601.aspx for more.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error Making Request to Azure Blob Storage: ");
                Console.WriteLine("");
                Console.WriteLine(ex.Message);
            }
            finally
            {
                Console.WriteLine("Press Enter to close...");
                Console.ReadLine();
                Console.ForegroundColor = original;
            }
        }

        private static string MapFileExtensionToContentType(string relativePathAndFileName)
        {
            string extension = relativePathAndFileName.Substring(relativePathAndFileName.IndexOf('.'));
            return MimeTypes.MimeTypeMap.GetMimeType(extension);
        }

    }
}