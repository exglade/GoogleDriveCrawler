using CsvHelper;
using Google.Apis.Drive.v3;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace GoogleDriveCrawler
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            // Crawl all files.

            Console.Write("Credential path: ");

            var credentialPath = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(credentialPath))
                credentialPath = "credential.json";

            IList<Google.Apis.Drive.v3.Data.File> files = null;
            using (var service = GoogleDriveServiceFactory.GetService(credentialPath, "token.json"))
            {
                Console.Write("Cralwing...");
                files = CrawlMyDrive(service);
                Console.WriteLine("DONE");
            }
            Console.WriteLine($"Files found: {files.Count}");

            // Save files as JSON format as raw output.

            Console.Write("JSON output path: ");

            var jsonOutputPath = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(jsonOutputPath))
                jsonOutputPath = $"output-{DateTime.Now.ToString("yyyyMMddhhmmss")}.json";

            Console.Write("Saving as JSON format...");
            GoogleDriveFileHelper.SaveAsJsonFile(files, jsonOutputPath);
            Console.WriteLine($"DONE: {jsonOutputPath}");

            // Save files as CSV.

            Console.Write("CSV output path: ");

            var csvOutputPath = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(csvOutputPath))
                csvOutputPath = $"output-{DateTime.Now.ToString("yyyyMMddhhmmss")}.csv";

            var folders = files
                .Where(file => file.MimeType == "application/vnd.google-apps.folder")
                .Select(folder => new Folder(folder.Id, folder.Name, folder.Parents))
                .ToList();
            GenerateBottomUpFolderTree(folders);

            Console.Write("Saving as CSV format...");
            WriteToCsv(files, csvOutputPath, folders);
            Console.WriteLine($"DONE: {csvOutputPath}");

            Console.Read();
        }

        public static void GenerateBottomUpFolderTree(IList<Folder> folders)
        {
            var map = folders.ToDictionary(x => x.Id);

            for (int i = 0; i < folders.Count; i++)
            {
                Console.Write($"\rGenerate folder tree...{i}/{folders.Count}");
                var folder = folders[i];
                foreach (var parentId in folder.ParentIds)
                {
                    var parent = map.GetValueOrDefault(parentId);
                    if (parent != null)
                        folder.Parents.Add(parent);
                }
            }
            Console.WriteLine();
        }

        /// <summary>
        /// Represent a folder.
        /// </summary>
        public class Folder
        {
            /// <summary>
            /// Google Folder Id.
            /// </summary>
            public string Id { get; set; }

            /// <summary>
            /// Folder Name.
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// Google Parent Folder Id.
            /// </summary>
            public IList<string> ParentIds { get; set; }

            /// <summary>
            /// Parents.
            /// </summary>
            public IList<Folder> Parents { get; set; }

            /// <summary>
            /// The full path to folder.
            /// </summary>
            private string _fullPath;
            public string FullPath
            {
                get
                {
                    if (_fullPath == null)
                    {
                        var folders = new List<string>();

                        Folder current = this;
                        while (current != null)
                        {
                            folders.Add(current.Name);
                            current = current.Parents.FirstOrDefault();
                        }

                        folders.Reverse();
                        _fullPath = string.Join("\\", folders);
                    }

                    return _fullPath;
                }
            }

            public Folder(string id, string name, IList<string> parentIds)
            {
                Id = id;
                Name = name;
                ParentIds = parentIds ?? new List<string>();
                Parents = new List<Folder>();
            }
        }

        /// <summary>
        /// Write files' details to CSV.
        /// </summary>
        /// <param name="files">Files</param>
        /// <param name="csvOutputPath">CSV output file path.</param>
        private static void WriteToCsv(IList<Google.Apis.Drive.v3.Data.File> files, string csvOutputPath, IList<Folder> folders)
        {
            var map = folders.ToDictionary(x => x.Id);

            using (var writer = new StreamWriter(csvOutputPath))
            using (var csvWriter = new CsvWriter(writer))
            {
                csvWriter.WriteField("id");
                csvWriter.WriteField("name");
                csvWriter.WriteField("mimeType");
                csvWriter.WriteField("starred");
                csvWriter.WriteField("trashed");
                csvWriter.WriteField("description");
                csvWriter.WriteField("parents");
                csvWriter.WriteField("createdTime");
                csvWriter.WriteField("modifiedTime");
                csvWriter.WriteField("modifiedByMeTime");
                csvWriter.WriteField("sharedWithMeTime");
                csvWriter.WriteField("sharingUser.displayName");
                csvWriter.WriteField("sharingUser.emailAddress");
                csvWriter.WriteField("sharingUser.me");
                csvWriter.WriteField("shared");
                csvWriter.WriteField("ownedByMe");
                csvWriter.WriteField("size");
                csvWriter.WriteField("trashedTime");
                csvWriter.WriteField("driveId");
                csvWriter.WriteField("fullFileExtension");
                csvWriter.WriteField("CUSTOM:folderPath");
                csvWriter.NextRecord();

                foreach (var file in files)
                {
                    csvWriter.WriteField(file.Id);
                    csvWriter.WriteField(file.Name);
                    csvWriter.WriteField(file.MimeType);
                    csvWriter.WriteField(file.Starred);
                    csvWriter.WriteField(file.Trashed);
                    csvWriter.WriteField(file.Description);
                    csvWriter.WriteField(string.Join(",", file.Parents ?? new List<string>()));
                    csvWriter.WriteField(file.CreatedTime);
                    csvWriter.WriteField(file.ModifiedTime);
                    csvWriter.WriteField(file.ModifiedByMeTime);
                    csvWriter.WriteField(file.SharedWithMeTime);
                    csvWriter.WriteField(file.SharingUser?.DisplayName);
                    csvWriter.WriteField(file.SharingUser?.EmailAddress);
                    csvWriter.WriteField(file.SharingUser?.Me);
                    csvWriter.WriteField(file.Shared);
                    csvWriter.WriteField(file.OwnedByMe);
                    csvWriter.WriteField(file.Size);
                    csvWriter.WriteField(file.TrashedTime);
                    csvWriter.WriteField(file.DriveId);
                    csvWriter.WriteField(file.FullFileExtension);
                    var parentId = file.Parents?.FirstOrDefault();
                    csvWriter.WriteField(parentId == null ? null : map.GetValueOrDefault(parentId)?.FullPath);
                    csvWriter.NextRecord();
                }

                writer.Flush();
            }
        }

        /// <summary>
        /// Crawl My Drive in Google Drive for list of all files and folders.
        /// </summary>
        /// <param name="service"><see cref="DriveService"/> for API call.</param>
        /// <returns>List of Google Files</returns>
        /// <remarks>
        /// Request - https://developers.google.com/drive/api/v3/reference/files/list
        /// Response - https://developers.google.com/drive/api/v3/reference/files#resource
        /// Search query - https://developers.google.com/drive/api/v3/search-files
        /// </remarks>
        private static IList<Google.Apis.Drive.v3.Data.File> CrawlMyDrive(DriveService service)
        {
            var request = service.Files.List();

            var output = new List<Google.Apis.Drive.v3.Data.File>();

            var fileParams = new[] {
                "id",
                "name",
                "mimeType",
                "starred",
                "trashed",
                "description",
                "parents",
                "createdTime",
                "modifiedTime",
                "modifiedByMeTime",
                "sharedWithMeTime",
                "sharingUser",
                "shared",
                "ownedByMe",
                "size",
                "trashedTime",
                "driveId",
                "fullFileExtension"
            };

            var i = 0;
            const int maxRounds = 100;
            const int pageSize = 1000; // 0 - 1000

            string pageToken = null;
            do
            {
                request.Spaces = "drive";
                request.Fields = $"nextPageToken, files({string.Join(", ", fileParams)})";
                request.PageSize = pageSize;
                request.PageToken = pageToken;

                var fileList = request.Execute();

                output.AddRange(fileList.Files);

                pageToken = fileList.NextPageToken;

                // Free quota
                // 1,000,000,000 queries per day.
                // 1,000 queries per 100 seconds per user.
                // 1,000 queries per 100 second.

                Thread.Sleep(1000); // Throttle.

                i++;
            }
            while (pageToken != null && i < maxRounds);

            return output;
        }
    }
}
