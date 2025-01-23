using UnityEngine.UIElements;

namespace Voidex.DataLabs.Dashboard
{
    public interface IGroupButton
    {
        string DisplayTitle { get; }
        string FullTypeName { get; }
        IDataGroup DataGroup { get; set; }
        VisualElement MainElement { get; set; }
        VisualElement InternalElement { get; set; }

        void SetAsCurrent();
        void SetIsSelected(bool state);
        void SetIsHighlighted(bool state);
        void SetShowFoldout(bool show);
    }
}