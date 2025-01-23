using Voidex.DataLabs;
using UnityEditor;
using UnityEditor.IMGUI.Controls;

namespace Voidex.DataLabs.Dashboard
{
    internal class DataEntityDropdownItem : AdvancedDropdownItem
    {
        public DataEntity Entity;

        public DataEntityDropdownItem(string name, DataEntity entity) : base(name)
        {
            Entity = entity;
        }
    }

    internal class AdvancedDropdown : UnityEditor.IMGUI.Controls.AdvancedDropdown
    {
        /*
         TODO
         https://forum.unity.com/threads/dropdownfield-popup-height-in-runtime-how-to-limit.1197157/
         m_OuterContainer.style.height = Mathf.Min(300,
                    m_MenuContainer.layout.height - m_MenuContainer.layout.y - m_OuterContainer.layout.y,
                    m_ScrollView.layout.height + m_OuterContainer.resolvedStyle.borderBottomWidth + m_OuterContainer.resolvedStyle.borderTopWidth);
         */

        public SerializedProperty TargetProperty;

        public AdvancedDropdown(AdvancedDropdownState state) : base(state)
        {
        }

        protected override AdvancedDropdownItem BuildRoot()
        {
            AdvancedDropdownItem root = new AdvancedDropdownItem(AssetDropdownDrawer.CurrentFilterType.ToString());
            foreach (DataEntity data in AssetDropdownDrawer.CurrentContent)
            {
                root.AddChild(new DataEntityDropdownItem(data.Title, data));
            }

            return root;
        }

        protected override void ItemSelected(AdvancedDropdownItem item)
        {
            DataEntity entity = ((DataEntityDropdownItem) item).Entity;
            AssetDropdownDrawer.ItemSelected(TargetProperty, entity);
            TargetProperty.serializedObject.ApplyModifiedProperties();
        }
    }
}