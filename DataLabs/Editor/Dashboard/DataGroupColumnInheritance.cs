using System.Collections.Generic;
using Voidex.DataLabs;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Voidex.DataLabs.Dashboard
{
    public class FilterColumnInheritance : DataGroupColumn
    {
        protected static ScrollView ScrollElement;
        public static Foldout CustomGroupsFoldout;
        public static Foldout DefaultGroupsFoldout;
        public List<DataLabsCustomDataGroup> CustomGroups = new List<DataLabsCustomDataGroup>();
        private readonly List<IGroupButton> m_filteringMustShow = new List<IGroupButton>();

        public static Texture IconBoxGreen;
        public static Texture IconBoxBlue;
        public static Texture IconBoxWireframe;

        public override void PanelReload()
        {
            RebuildPrep();
            RebuildCustomGroups();
            RebuildStaticGroups();
        }

        private void RebuildPrep()
        {
            Clear();
            if (IconBoxGreen == null)
            {
                IconBoxGreen = DataLabsEditorUtility.GetEditorImage("box_full_green");
                IconBoxBlue = DataLabsEditorUtility.GetEditorImage("box_full_blue");
                IconBoxWireframe = DataLabsEditorUtility.GetEditorImage("box_empty");
            }

            AllButtonsCache = new List<IGroupButton>();

            ScrollElement = new ScrollView();
            ScrollElement.style.flexGrow = 1;

#if UNITY_2021_3_OR_NEWER
            // scroll view behavior and controls changed in 2021.1
            ScrollElement.mode = ScrollViewMode.Vertical;
            ScrollElement.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            ScrollElement.verticalScrollerVisibility = ScrollerVisibility.Auto;
            this.style.flexGrow = 1;
            this.style.flexShrink = 1;
#endif

            this.Add(ScrollElement);
        }

        private void RebuildCustomGroups()
        {
            CustomGroups = DatabaseBuilder.GetAllCustomDataGroupAssets();
            if (CustomGroups == null) return;

            //Debug.Log($"....Adding {CustomGroups.Count} Custom Groups to DataLabs List");
            CustomGroups.RemoveAll(group => group == null);
            if (CustomGroups.Count == 0) return;

            CustomGroupsFoldout = new Foldout();
            CustomGroupsFoldout.text = "Custom Data Groups";
            CustomGroupsFoldout.contentContainer.style.marginLeft = -5;
            CustomGroups.Sort((x, y) => string.CompareOrdinal(x.SourceType.FullName, y.SourceType.FullName));

            ScrollElement.Add(CustomGroupsFoldout);

            foreach (DataLabsCustomDataGroup g in CustomGroups)
            {
                GroupFoldableButton currentButton = new GroupFoldableButton(g, IconBoxWireframe, true);
                CustomGroupsFoldout.Add(currentButton.MainElement);

                UnityEditor.Editor ed = UnityEditor.Editor.CreateEditor((DataLabsCustomDataGroup) currentButton.DataGroup);
                SerializedObject so = ed.serializedObject;
                Button button = currentButton.MainElement.Q<Button>();
                button.bindingPath = "m_title";
                button.BindProperty(so);
            }
        }

        private void RebuildStaticGroups()
        {
            DefaultGroupsFoldout = new Foldout();
            DefaultGroupsFoldout.text = "Class Hierarchy";
            DefaultGroupsFoldout.contentContainer.style.marginLeft = -5;
            DefaultGroupsFoldout.style.marginLeft = 5;

            ScrollElement.Add(DefaultGroupsFoldout);

            foreach (DataLabsStaticDataGroup group in DataLab.Db.GetAllStaticGroups())
            {
                GroupFoldableButton labsGroupButton = new GroupFoldableButton(group, group.SourceType.IsAbstract ? IconBoxBlue : IconBoxGreen, false);
                AllButtonsCache.Add(labsGroupButton);
            }

            AllButtonsCache.Sort((x, y) => string.CompareOrdinal(x.DisplayTitle, y.DisplayTitle));

            foreach (IGroupButton curButton in AllButtonsCache)
            {
                DefaultGroupsFoldout.Add(curButton.MainElement);
            }

            foreach (IGroupButton curButton in AllButtonsCache)
            {
                // if it is a first level class
                if (curButton.DataGroup.SourceType.BaseType == typeof(DataEntity) || curButton.DataGroup.SourceType == typeof(DataEntity)) continue;

                // if not, find parent class button
                IGroupButton targetParent = AllButtonsCache.Find(otherButton => otherButton.DataGroup.SourceType == curButton.DataGroup.SourceType.BaseType);
                if (targetParent == null) continue;

                targetParent.InternalElement.Add(curButton.MainElement);
                targetParent.SetShowFoldout(true);
            }
        }

        public override GroupFoldableButton SelectButtonByFullTypeName(string fullTypeName)
        {
            GroupFoldableButton button = ScrollElement.Q<GroupFoldableButton>(fullTypeName);
            if (button == null) return null;

            ScrollTo(button);
            button.SetAsCurrent();
            return button;
        }

        public override GroupFoldableButton SelectButtonByDisplayTitle(string title)
        {
            GroupFoldableButton button = ScrollElement.Q<GroupFoldableButton>(title);
            if (button == null) return null;

            ScrollTo(button);
            button.SetAsCurrent();
            return button;
        }

        public override GroupFoldableButton SelectButtonDirectly(GroupFoldableButton button)
        {
            if (button == null) return null;

            ScrollTo(button);
            button.SetAsCurrent();
            return button;
        }

        public override void ScrollTo(VisualElement button)
        {
            ScrollElement.ScrollTo(button);
        }

        public override void Filter(string filter)
        {
            m_filteringMustShow.Clear();
            if (string.IsNullOrEmpty(filter))
            {
                foreach (IGroupButton button in AllButtonsCache)
                {
                    button.SetIsHighlighted(false);
                }
            }
            else
            {
                foreach (IGroupButton button in AllButtonsCache)
                {
                    // turn it off
                    button.SetIsHighlighted(false);

                    // if there's a name match, turn it back on
                    bool isNameMatch = button.DataGroup.SourceType.Name.ToLower().Contains(filter.ToLower());
                    if (!isNameMatch) continue;

                    button.SetIsHighlighted(true);
                    FilterUpHierarchy(button);
                }

                foreach (IGroupButton button in m_filteringMustShow)
                {
                    button.SetIsHighlighted(true);
                }
            }
        }

        private void FilterUpHierarchy(IGroupButton button)
        {
            IGroupButton buttonParent = AllButtonsCache.Find(x => x.DataGroup.SourceType == button.DataGroup.SourceType.BaseType);
            if (buttonParent != null)
            {
                m_filteringMustShow.Add(buttonParent);
                FilterUpHierarchy(buttonParent);
            }
        }
    }
}