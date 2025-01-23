using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using UnityEditor;
using UnityEngine.Assertions;

namespace Voidex.DataLabs.GoogleSheets.Editor
{
    public class Importer<T> where T : IImportUtility
    {
        public event Action<string, float> OnProgressChanged;

        public bool IsImporting
        {
            get { return m_isImporting; }
        }
        
        private ImportState m_state;

        private ServiceAccountCredential m_credentials;
        private SheetsService m_service;
        private CallbackBindings m_callbacks;
        private List<Spreadsheet> m_spreadsheets;
        private List<ValueRange> m_values;
        private CancellationTokenSource m_cancelToken;
        
        private bool m_isImporting;

        public void SetCredentials(ServiceAccountCredential blob)
        {
            m_credentials = blob;
        }

        public void SetService(SheetsService service)
        {
            m_service = service;
        }

        public void Import(DataLabsSheetsProfile profile, Action onComplete, Action<IEnumerable<Exception>> onError)
        {
            Import((IEnumerable<Profile>) profile, onComplete, onError);
        }

        public void Import(Profile profile, Action onComplete, Action<IEnumerable<Exception>> onError)
        {
            Import(new List<Profile>() {profile}, onComplete, onError);
        }
        
        public void Import(List<Profile> profiles, Action onComplete, Action<IEnumerable<Exception>> onError)
        {
            Import((IEnumerable<Profile>) profiles, onComplete, onError);
        }

        public void CancelImport()
        {
        }

        private UniTask Import(IEnumerable<Profile> profiles, Action onComplete, Action<IEnumerable<Exception>> onError)
        {
            ImportState state = new ImportState(profiles, onComplete, onError);
            // if we're already importing, ignore the request and error
            if (IsImporting)
            {
                state.LogError("Already trying to Import. Did you forget to Abort?");
            }

            // credentials are required to import
            if (m_credentials == null)
            {
                state.LogError("No credentials have been assigned. Call `SetCredentials' before importing");
            }

            m_isImporting = true;
            
            // confirm that all of the requested profiles can bind properly
            foreach (Profile profile in profiles)
            {
                if (state.HasAssetBinding(profile.AssetType))
                {
                    continue; // already included, don't need duplicates
                }

                AssetBindings binding = new AssetBindings(state, profile);
                if (!state.HasErrors)
                {
                    state.AddAssetBinding(profile.AssetType, binding);
                }
            }

            // confirm that all callbacks are bound properly as well
            m_callbacks = new CallbackBindings(state);

            if (state.HasErrors)
            {
                state.Complete();
                return UniTask.CompletedTask;
            }

            m_state = state;
            m_cancelToken = new CancellationTokenSource();
            return ImportRoutine(profiles, m_cancelToken.Token);
        }

        private async UniTask ImportRoutine(IEnumerable<Profile> profiles, CancellationToken token)
        {
            // 1. create a set of unique sheets that we need to poll data from
            SetProgress("Building Sheets Request...", 0.1f);
            List<string> sheetIDs = new List<string>();
            List<string> sheetProfiles = new List<string>();
            var enumerable = profiles as Profile[] ?? profiles.ToArray();
            foreach (Profile profile in enumerable)
            {
                bool idExists = false;
                for (int ix = 0; ix < sheetIDs.Count; ix++)
                {
                    if (StringComparer.Ordinal.Equals(sheetIDs[ix], profile.SheetID))
                    {
                        sheetProfiles[ix] = string.Concat(sheetProfiles[ix], ", ", profile.ProfileName);
                        idExists = true;
                        break;
                    }
                }

                if (!idExists)
                {
                    sheetIDs.Add(profile.SheetID);
                    sheetProfiles.Add(profile.ProfileName);
                }
            }

            Assert.AreEqual(sheetIDs.Count, sheetProfiles.Count);

            // 2. send requests for each of the sheet ids to get meta data
            SetProgress("Fetching Meta Data...", 0.2f);

            m_spreadsheets = new List<Spreadsheet>();

            foreach (string sheetID in sheetIDs)
            {
                try
                {
                    var request = new SpreadsheetsResource.GetRequest(m_service, sheetID);
                    var response = await request.ExecuteAsync(token);
                    m_spreadsheets.Add(response);
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogError(e);
                    throw;
                }
            }



            // 3. check for errors and end routine if there are any
            if (m_state.HasErrors)
            {
                Cleanup();
                return;
            }

            // 4. add the metadata to the state and poll needed individual worksheets
            SetProgress("Adding Meta Data...", 0.3f);
            for (int ix = 0; ix < sheetIDs.Count; ix++)
            {
                m_state.AddMetaData(sheetIDs[ix], m_spreadsheets[ix]);
            }


            // 5. validate that all of the needed worksheets exist on their respective spreadsheets
            SetProgress("Validating Worksheets...", 0.4f);
            List<WorksheetID> worksheets = new List<WorksheetID>();
            sheetProfiles.Clear();

            foreach (Profile profile in enumerable)
            {
                for (int i = 0; i < profile.WorksheetNames.Length; i++)
                {
                    WorksheetID id = new WorksheetID(profile.SheetID, profile.WorksheetNames[i]);
                    int existing = worksheets.IndexOf(id);
                    if (existing != -1)
                    {
                        sheetProfiles[existing] = string.Concat(sheetProfiles[existing], ", ", profile.ProfileName);
                        continue;
                    }

                    if (m_state.HasWorksheet(id))
                    {
                        worksheets.Add(id);
                        sheetProfiles.Add(profile.ProfileName);
                    }
                    else
                    {
                        m_state.LogError($"Error in profile {profile.ProfileName}: worksheet `{profile.WorksheetNames[i]}' does not exist on the specified spreadsheet identity");
                    }
                }

            }

            //Assert.AreEqual(worksheets.Count, sheetProfiles.Count);

            if (m_state.HasErrors)
            {
                Cleanup();
                return;
            }

            // 6. use the properties information provided in the worksheets set to poll needed spreadsheet values
            SetProgress("Fetching Worksheets...", 0.5f);
            m_values = new List<ValueRange>();

            for (int ix = 0; ix < worksheets.Count; ix++)
            {
                if (m_state.TryGetA1Max(worksheets[ix], out string a1Max))
                {
                    var request = new SpreadsheetsResource.ValuesResource.GetRequest(m_service, worksheets[ix].SheetID, $"{worksheets[ix].WorksheetName}!A1:{a1Max}");
                    var response = await request.ExecuteAsync(token);
                    m_values.Add(response);

                    await UniTask.WaitForSeconds(0.7f, cancellationToken: token);
                }
                else
                {
                    m_state.LogError($"Error in {sheetProfiles[ix]}: Could not determine the size of the worksheet. Is the google data bad?");
                }
            }

            if (m_state.HasErrors)
            {
                Cleanup();
                return;
            }


            Assert.AreEqual(worksheets.Count, m_values.Count);

            // 7. compile the values received and convert them to DataSheet objects
            for (int ix = 0; ix < worksheets.Count; ix++)
            {
                var valueRange = m_values[ix];
                var filteredValues = new List<IList<object>>();

                foreach (var row in valueRange.Values)
                {
                    // Skip rows with headers starting with '##'
                    if (row.Count > 0 && row[0] is string rowHeader && rowHeader.StartsWith("##"))
                    {
                        continue;
                    }
                    
                    // Skip columns with headers starting with '##'
                    var filteredRow = row.Where((cell, index) =>
                        index >= valueRange.Values[0].Count ||
                        !(valueRange.Values[0][index] is string columnHeader) ||
                        !columnHeader.StartsWith("##")).ToList();

                    filteredValues.Add(filteredRow);
                }

                valueRange.Values = filteredValues;
                m_state.AddDataSheet(worksheets[ix], valueRange);


                //m_state.AddDataSheet(worksheets[ix], m_values[ix]);
            }

            // 8. use added bindings to iterate over datasheets for profiles to do Import stage
            IImportUtility util = (IImportUtility) Activator.CreateInstance(typeof(T), m_state);
            List<DataSheet> dataSheets;
            AssetBindings bindings;
            Spreadsheet spreadsheet;
            float value = 0.6f;
            try
            {
                foreach (Profile profile in enumerable)
                {
                    SetProgress("Executing Import...", value);
                    // WorksheetID worksheetID = new WorksheetID(profile.SheetID, profile.WorksheetNames[0]);
                    dataSheets = m_state.GetDataSheets(profile.SheetID, profile.WorksheetNames);
                    bindings = m_state.GetAssetBindings(profile.AssetType);
                    spreadsheet = m_spreadsheets.First(x => x.SpreadsheetId == profile.SheetID);
                    util.Reset(dataSheets, spreadsheet, bindings.PrimaryKey, profile.AssetDirectory);

                    bindings.Import(util);
                    value += (1f / enumerable.Count()) * 0.15f;
                }
            }
            catch (Exception e)
            {
                m_state.LogError(e);
            }

            if (m_state.HasErrors)
            {
                Cleanup();
                return;
            }

            // 9. use added bindings to do the LateImport stage
            try
            {
                foreach (Profile profile in enumerable)
                {
                    SetProgress("Executing Late Import...", value);
                    //WorksheetID worksheetID = new WorksheetID(profile.SheetID, profile.WorksheetNames[0]);
                    dataSheets = m_state.GetDataSheets(profile.SheetID, profile.WorksheetNames);
                    bindings = m_state.GetAssetBindings(profile.AssetType);
                    spreadsheet = m_spreadsheets.First(x => x.SpreadsheetId == profile.SheetID);
                    util.Reset(dataSheets, spreadsheet, bindings.PrimaryKey, profile.AssetDirectory);

                    bindings.LateImport(util);
                    value += (1f / enumerable.Count()) * 0.15f;
                }
            }
            catch (Exception e)
            {
                m_state.LogError(e);
            }

            if (m_state.HasErrors)
            {
                Cleanup();
                return;
            }

            // 10. save the asset database now that everything is imported
            SetProgress("Saving Assets...", 0.9f);
            AssetDatabase.SaveAssets();
            await UniTask.Yield();

            // 11. send callbacks to ContentCallback attribute holders
            SetProgress("Sending Callbacks...", 0.95f);
            m_callbacks.OnComplete();
            await UniTask.Yield();

            // x. Cleanup and complete the import process
            Cleanup();
        }

        private void SetProgress(string message, float percent)
        {
            OnProgressChanged?.Invoke(message, percent);
        }

        private void Cleanup()
        {
            m_state.Complete();

            m_values = null;
            
            m_callbacks = null;

            m_spreadsheets.Clear();
            m_spreadsheets = null;

            m_cancelToken.Cancel();
            m_cancelToken.Dispose();
            m_isImporting = false;
        }
    }
}