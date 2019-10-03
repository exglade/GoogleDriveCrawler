using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System;
using System.IO;
using System.Threading;

namespace GoogleDriveCrawler
{
    public static class GoogleDriveServiceFactory
    {
        // https://developers.google.com/drive/api/v3/quickstart/dotnet

        // If modifying these scopes, delete your previously saved credentials
        // at ~/.credentials/drive-dotnet-quickstart.json
        private static readonly string[] Scopes = { DriveService.Scope.DriveReadonly };

        private const string ApplicationName = "Kai's Google Drive Crawler";

        public static DriveService GetService(string credentialsJsonPath, string tokenOutputPath)
        {
            var credential = GenerateDriveUserCredential(credentialsJsonPath, tokenOutputPath);

            return new DriveService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName,
            });
        }

        private static UserCredential GenerateDriveUserCredential(string credentialsJsonPath = "credentials.json", string tokenOutputPath = "token.json")
        {
            UserCredential credential;

            using (var stream =
                new FileStream(credentialsJsonPath, FileMode.Open, FileAccess.Read))
            {
                // The file token.json stores the user's access and refresh tokens, and is created
                // automatically when the authorization flow completes for the first time.
                string credPath = tokenOutputPath;
                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    Scopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore(credPath, true)).Result;
                Console.WriteLine("Credential file saved to: " + credPath);
            }

            return credential;
        }
    }
}
