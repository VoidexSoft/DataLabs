using System;
using System.Collections.Generic;
using System.Globalization;
using Google.Apis.Sheets.v4.Data;
using UnityEngine;

namespace Voidex.DataLabs.GoogleSheets.Editor
{
    public class ImportState : ILogger
    {
        public bool HasErrors
        {
            get { return m_errors.Count > 0; }
        }

        public IEnumerable<Profile> Profiles { get; }

        private List<Exception> m_errors;
        private Action m_onComplete;
        private Action<IEnumerable<Exception>> m_onError;
        private Dictionary<WorksheetID, SheetProperties> m_metaData;
        private Dictionary<WorksheetID, DataSheet> m_dataSheets;
        private Dictionary<string, AssetBindings> m_assetBindings;

        private const string ALPHABET = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

        public ImportState(IEnumerable<Profile> profiles, Action onComplete, Action<IEnumerable<Exception>> onError)
        {
            m_errors = new List<Exception>();
            m_metaData = new Dictionary<WorksheetID, SheetProperties>();
            m_dataSheets = new Dictionary<WorksheetID, DataSheet>();
            m_assetBindings = new Dictionary<string, AssetBindings>();

            Profiles = profiles;
            m_onComplete = onComplete;
            m_onError = onError;
        }

        public void LogError(string error)
        {
            m_errors.Add(new Exception(error));
        }

        public void LogError(Exception error)
        {
            m_errors.Add(error);
        }

        public void LogWarning(string warning)
        {
            Debug.LogWarning(warning);
        }

        // public void AddMetaData(string sheetId, SpreadsheetBlob metaData)
        // {
        //     foreach (SheetBlob worksheet in metaData.sheets)
        //     {
        //         WorksheetID id = new WorksheetID(sheetId, worksheet.properties.title);
        //         m_metaData.Add(id, worksheet.properties);
        //     }
        // }

        public void AddMetaData(string sheetId, Spreadsheet metaData)
        {
            foreach (Sheet sheet in metaData.Sheets)
            {
                WorksheetID id = new WorksheetID(sheetId, sheet.Properties.Title);
                m_metaData.Add(id, sheet.Properties);
            }
        }

        // public void AddDataSheet(WorksheetID id, ValueRangeBlob valueRange)
        // {
        //     int frozenRows = m_metaData[id].gridProperties.frozenRowCount;
        //     m_dataSheets.Add(id, new DataSheet(id, valueRange, frozenRows));
        // }
        public void AddDataSheet(WorksheetID id, ValueRange valueRange)
        {
            int? frozenRows = m_metaData[id].GridProperties.FrozenRowCount;
            m_dataSheets.Add(id, new DataSheet(id, valueRange, frozenRows ?? 0));
        }

        public void AddAssetBinding(string assetType, AssetBindings bindings)
        {
            m_assetBindings.Add(assetType, bindings);
        }


        public bool HasWorksheet(WorksheetID worksheetID)
        {
            return m_metaData.ContainsKey(worksheetID);
        }

        public DataSheet GetDataSheet(WorksheetID worksheetID)
        {
            return m_dataSheets[worksheetID];
        }

        public List<DataSheet> GetDataSheets(string spreadSheetId, string[] worksheetNames)
        {
            List<DataSheet> dataSheets = new List<DataSheet>();
            foreach (string worksheetName in worksheetNames)
            {
                WorksheetID id = new WorksheetID(spreadSheetId, worksheetName);
                if (m_dataSheets.TryGetValue(id, out DataSheet dataSheet))
                {
                    dataSheets.Add(dataSheet);
                }
            }

            return dataSheets;
        }
        
        public SheetProperties GetSheetProperties(WorksheetID worksheetID)
        {
            return m_metaData[worksheetID];
        }

        public bool HasAssetBinding(string assetType)
        {
            return m_assetBindings.ContainsKey(assetType);
        }

        public AssetBindings GetAssetBindings(string assetType)
        {
            return m_assetBindings[assetType];
        }

        public bool TryGetA1Max(WorksheetID worksheetID, out string range)
        {
            if (m_metaData.TryGetValue(worksheetID, out SheetProperties blob))
            {
                if (blob.GridProperties.RowCount == 0 || blob.GridProperties.ColumnCount == 0)
                {
                    range = string.Empty;
                    return false;
                }

                range = ToA1Notation(blob.GridProperties.RowCount.GetValueOrDefault(), blob.GridProperties.ColumnCount.GetValueOrDefault());
                return true;
            }

            range = string.Empty;
            return false;
        }

        public void Complete()
        {
            if (HasErrors)
            {
                m_onError?.Invoke(m_errors);
            }
            else
            {
                m_onComplete?.Invoke();
            }
        }

        private string ToA1Notation(int rows, int columns)
        {
            string a1Notation = "";
            while (columns > 0)
            {
                int remainder = (columns - 1) % ALPHABET.Length;
                a1Notation = string.Concat(ALPHABET[remainder], a1Notation);
                columns = (columns - remainder) / ALPHABET.Length;
            }

            a1Notation = string.Concat(a1Notation, rows.ToString(CultureInfo.InvariantCulture));
            return a1Notation;
        }
    }
}