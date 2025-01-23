using System;
using System.Collections.Generic;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Sheets.v4;
using UnityEngine;
using Voidex.DataLabs.GoogleSheets.Editor;

namespace Voidex.DataLabs.DataLabs.Editor.GoogleSheets
{
    public sealed class GoogleSheetImporter<T> where T : IImportUtility
    {
        private Importer<T>? _importer;
        private Authentication? _authenticator;
        
        private ServiceAccountCredential? _credential;
        private SheetsService? _service;
        
        public event Action OnAuthenticationStart;
        public event Action OnAuthenticationComplete;
        public event Action<string> OnAuthenticationError;
        public event Action OnImportStart;
        public event Action OnImportComplete;
        public event Action<IEnumerable<Exception>> OnImportErrors;
        
        public Importer<T> Importer => _importer;
        public Authentication Authenticator => _authenticator;
        
        private const string m_credentialsPath = "../Credentials/game-data-444415-5c2bbd742ceb.p12";
        private const string m_serviceAccountEmail = "game-data@game-data-444415.iam.gserviceaccount.com";
        
        public GoogleSheetImporter()
        {
            _importer = new Importer<T>();
            _authenticator = new Authentication();
        }
        
        public void DoAuthentication(Action importCallback)
        {
            OnAuthenticationStart?.Invoke();
            GoogleSheetSettings.instance.CredentialsPath = m_credentialsPath;
            GoogleSheetSettings.instance.ServiceAccountEmail = m_serviceAccountEmail;
            
            _service = _authenticator.Authenticate(GoogleSheetSettings.instance, 
                credential => HandleAuthenticationComplete(credential), 
                HandleAuthenticationError);
            _importer.SetService(_service);
            importCallback?.Invoke();
        }

        private void HandleAuthenticationError(string obj)
        {
            OnAuthenticationError?.Invoke(obj);
        }

        private void HandleAuthenticationComplete(ServiceAccountCredential credential)
        {
            _credential = credential;
            OnAuthenticationComplete?.Invoke();
        }
        
        public void DoImport(Profile profile)
        {
            if (profile == null)
            {
                return;
            }

            _importer.SetCredentials(_credential);
            
            OnImportStart?.Invoke();
            _importer.Import(profile, HandleImportComplete, HandleImportErrors);
        }

        public void DoImportAll(List<Profile> profiles)
        {
            if (profiles == null)
            {
                return;
            }

            _importer.SetCredentials(_credential);
            _importer.SetService(_service);
            
            OnImportStart?.Invoke();
            _importer.Import(profiles, HandleImportComplete, HandleImportErrors);
        }
        
        private void HandleImportComplete()
        {
            OnImportComplete?.Invoke();
        }

        private void HandleImportErrors(IEnumerable<Exception> errors)
        {
            OnImportErrors?.Invoke(errors);
        }
    }
}