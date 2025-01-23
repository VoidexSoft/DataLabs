using UnityEngine.UIElements;

namespace Voidex.DataLabs.Dashboard
{
    public abstract class DashboardColumn : VisualElement
    {
        public new class UxmlFactory : UxmlFactory<VisualElement, UxmlTraits>
        {
        }

        public abstract void PanelReload();
    }
}