namespace Voidex.DataLabs.GoogleSheets.Editor.Json
{
    internal interface IPattern
    {
        bool Matches(CharStream stream, out Token token);
    }
}