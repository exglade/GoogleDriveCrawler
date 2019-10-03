using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;

namespace GoogleDriveCrawler
{
    public static class GoogleDriveFileHelper
    {
        public static void SaveAsJsonFile(IList<Google.Apis.Drive.v3.Data.File> files, string outputFilePath)
        {
            var json = JsonConvert.SerializeObject(files);
            File.WriteAllText(outputFilePath, json);
        }

        public static IList<Google.Apis.Drive.v3.Data.File> ReadFromJsonFile(string jsonFilePath)
        {
            var jsonInput = File.ReadAllText(jsonFilePath);
            return JsonConvert.DeserializeObject<IList<Google.Apis.Drive.v3.Data.File>>(jsonInput);
        }
    }
}
