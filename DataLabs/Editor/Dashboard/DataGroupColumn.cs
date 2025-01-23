using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Voidex.DataLabs.Dashboard
{
    public abstract class DataGroupColumn : DashboardColumn
    {
        protected static List<IGroupButton> AllButtonsCache;
        public abstract GroupFoldableButton SelectButtonByFullTypeName(string fullTypeName);
        public abstract GroupFoldableButton SelectButtonByDisplayTitle(string title);
        public abstract GroupFoldableButton SelectButtonDirectly(GroupFoldableButton button);
        public abstract void ScrollTo(VisualElement button);
        public abstract void Filter(string f);
    }
}