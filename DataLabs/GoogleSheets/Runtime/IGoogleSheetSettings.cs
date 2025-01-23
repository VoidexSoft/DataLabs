using Voidex.DataLabs.GoogleSheets.Editor;

namespace Voidex.DataLabs.DataLabs.GoogleSheets.Runtime
{
    public interface IGoogleSheetSettings
    {
        string CredentialsPath { get; set; }
        DataLabsSheetsProfile Profile { get; set; }
        string ServiceAccountEmail { get; set; }
    }
}