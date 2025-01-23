using System;
using System.IO;
using UnityEngine;
using System.Security.Cryptography.X509Certificates;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Voidex.DataLabs.DataLabs.GoogleSheets.Runtime;

namespace Voidex.DataLabs.GoogleSheets.Editor
{
    public class Authentication
    {
        public bool IsAuthenticating { get; private set; } = false;

        private string m_credentialsPath;
        private Action<string> m_onError;
        
        public SheetsService Authenticate(IGoogleSheetSettings settings, Action<ServiceAccountCredential> onComplete, Action<string> onError)
        {
            try
            {
                var certificate = new X509Certificate2(Application.dataPath + Path.DirectorySeparatorChar + settings.CredentialsPath, "notasecret", X509KeyStorageFlags.Exportable);

                ServiceAccountCredential credential = new ServiceAccountCredential(
                    new ServiceAccountCredential.Initializer(settings.ServiceAccountEmail)
                    {
                        Scopes = new[] {SheetsService.Scope.SpreadsheetsReadonly, SheetsService.Scope.Spreadsheets, SheetsService.Scope.Drive}
                    }.FromCertificate(certificate));

                var service = new SheetsService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential
                });

                onComplete?.Invoke(credential);
                
                return service;
            }
            catch (Exception ex)
            {
                onError?.Invoke($"Error during authentication: {ex.Message}");
            }
            return null;
        }

        public void CancelAuthentication()
        {
            m_onError?.Invoke("Authentication Aborted");
            Reset();
        }

        private void Reset()
        {
            m_onError = null;
            m_credentialsPath = null;
            IsAuthenticating = false;
        }
    }
}