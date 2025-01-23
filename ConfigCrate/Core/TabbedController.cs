using UnityEngine.UIElements;

namespace Voidex.ConfigCrate.Configurations
{
    public class TabbedController
    {
        /* Define member variables*/
        private const string tabClassName = "tab";
        private const string currentlySelectedTabClassName = "currentlySelectedTab";

        //private const string unselectedContentClassName = "unselectedContent";

        // Tab and tab content have the same prefix but different suffix
        // Define the suffix of the tab name
        private const string tabNameSuffix = "Tab";

        // Define the suffix of the tab content name
        private const string contentNameSuffix = "Content";

        private readonly VisualElement root = default;

        public TabbedController(in VisualElement root)
        {
            this.root = root;
            //RegisterTabCallbacks();
        }

        public void RegisterTabCallbacks()
        {
            UQueryBuilder<Label> tabs = GetAllTabs();
            tabs.ForEach(RegisterTabCallbacks);
        }

        private void RegisterTabCallbacks(Label tab)
        {
            tab.RegisterCallback<MouseDownEvent>(TabOnClick);
        }

        /* Method for the tab on-click event:

           - If it is not selected, find other tabs that are selected, unselect them
           - Then select the tab that was clicked on
        */
        private void TabOnClick(MouseDownEvent evt)
        {
            Label clickedTab = evt.currentTarget as Label;
            if (!TabIsCurrentlySelected(clickedTab))
            {
                UQueryBuilder<Label> tabs = GetAllTabs();
                UQueryBuilder<Label> otherSelectedTabs =
                    tabs.Where((Label tab) => tab != clickedTab && TabIsCurrentlySelected(tab));
                otherSelectedTabs.ForEach(UnselectTab);
                SelectTab(clickedTab);
            }
        }

        //Method that returns a Boolean indicating whether a tab is currently selected
        private static bool TabIsCurrentlySelected(in Label tab)
        {
            return tab.ClassListContains(currentlySelectedTabClassName);
        }

        private UQueryBuilder<Label> GetAllTabs()
        {
            return root.Query<Label>(className: tabClassName);
        }

        /* Method for the selected tab:
           -  Takes a tab as a parameter and adds the currentlySelectedTab class
           -  Then finds the tab content and removes the unselectedContent class */
        private void SelectTab(in Label tab)
        {
            tab.AddToClassList(currentlySelectedTabClassName);
            VisualElement content = FindContent(tab);
            //content.RemoveFromClassList(unselectedContentClassName);
            content.style.display = DisplayStyle.Flex;
        }

        /* Method for the unselected tab:
           -  Takes a tab as a parameter and removes the currentlySelectedTab class
           -  Then finds the tab content and adds the unselectedContent class */
        private void UnselectTab(Label tab)
        {
            tab.RemoveFromClassList(currentlySelectedTabClassName);
            VisualElement content = FindContent(tab);
            //content.AddToClassList(unselectedContentClassName);
            content.style.display = DisplayStyle.None;
        }
        
        public void SelectTab(string tabName)
        {
            Label tab = root.Q<Label>(tabName);
            if (tab != null)
            {
                SelectTab(tab);
            }
        }

        // Method to generate the associated tab content name by for the given tab name
        private static string GenerateContentName(in Label tab)
        {
            int prefixLength = tab.name.Length - tabNameSuffix.Length;
            string prefix = tab.name.Substring(0, prefixLength);
            return prefix + contentNameSuffix;
        }

        // Method that takes a tab as a parameter and returns the associated content element
        private VisualElement FindContent(in Label tab)
        {
            return root.Q(GenerateContentName(tab));
        }
    }
}