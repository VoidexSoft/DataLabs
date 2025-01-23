using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Voidex.DataLabs.Dashboard
{
    public class AssetInspector : DashboardColumn
    {
        protected SerializedObject TargetSerializedObject;
        private readonly ScrollView m_contentWindow;

        public AssetInspector()
        {
            TargetSerializedObject = GetSerializedObj();
            m_contentWindow = new ScrollView();
            m_contentWindow.style.flexShrink = 1f;
            m_contentWindow.style.flexGrow = 1f;
            m_contentWindow.style.paddingBottom = 10;
            m_contentWindow.style.paddingLeft = 10;
            m_contentWindow.style.paddingRight = 10;
            m_contentWindow.style.paddingTop = 10;
            this.style.flexShrink = 1f;
            this.style.flexGrow = 1f;
            
            this.name = "Asset Inspector";
            this.viewDataKey = "ASSET_INSPECTOR";
            this.style.flexShrink = 1f;
            this.style.flexGrow = 1f;
            this.style.paddingBottom = 10;
            this.style.paddingLeft = 10;
            this.style.paddingRight = 10;
            this.style.paddingTop = 10;
            this.Add(m_contentWindow);
        }

        public override void PanelReload()
        {
            m_contentWindow.Clear();
            if (DataLabsDashboard.CurrentSelectedAsset == null)
            {
                InspectNothing();
                return;
            }

            TargetSerializedObject = GetSerializedObj();

            bool success = BuildInspectorProperties(TargetSerializedObject, m_contentWindow);
            if (success) m_contentWindow.Bind(TargetSerializedObject); // TODO BUG
        }

        public void InspectNothing()
        {
            m_contentWindow.Clear();
            m_contentWindow.Add(new Label {text = " ⓘ Asset Inspector"});
            m_contentWindow.Add(new Label("\n\n    ⚠ Please select an asset from the column to the left."));
        }

        private static SerializedObject GetSerializedObj()
        {
            if (DataLabsDashboard.CurrentSelectedAsset == null)
                return null;

            // Create a new OdinEditor instance each time
            var editor = OdinEditor.CreateEditor(DataLabsDashboard.CurrentSelectedAsset);
            return editor.serializedObject;
        }

        private static bool BuildInspectorProperties(SerializedObject obj, VisualElement wrapper)
        {
            if (obj == null || wrapper == null) return false;
            wrapper.Add(new Label {text = " ⓘ Asset Inspector"});
            // build the focus script button
            Button focusButton = new Button(() => EditorGUIUtility.PingObject(obj.FindProperty("m_Script").objectReferenceValue));
            focusButton.text = "☲";
            focusButton.style.minWidth = 20;
            focusButton.style.maxWidth = 20;
            focusButton.tooltip = "Ping this Script";                    
                    
            // build the focus object button
            Button focusAsset = new Button(() => EditorGUIUtility.PingObject(obj.targetObject));
            focusAsset.text = "☑";
            focusAsset.style.minWidth = 20;
            focusAsset.style.maxWidth = 20;
            focusAsset.tooltip = "Ping this Asset";
            
            VisualElement buttonContainer = new VisualElement();
            buttonContainer.style.flexDirection = FlexDirection.Row;

            buttonContainer.Add(focusButton);
            buttonContainer.Add(focusAsset);
            
            wrapper.Add(buttonContainer);

            // Create an OdinEditor for the target object
            OdinEditor editor = (OdinEditor) OdinEditor.CreateEditor(obj.targetObject);
            //UnityEditor.Editor editor = UnityEditor.Editor.CreateEditor(obj.targetObject);

            // Create an InspectorElement for the OdinEditor
            InspectorElement inspector = new InspectorElement(editor);

            // Add the InspectorElement to the wrapper
            wrapper.Add(inspector);

            return true;
        }
    }
}