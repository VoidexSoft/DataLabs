using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;
using Voidex.DataLabs.GoogleSheets.Runtime;

namespace Voidex.DataLabs
{
    public abstract class DataEntity : ScriptableObject
    {
        [SerializeField, ReadOnly] private int m_dbKey = int.MinValue;

        /// <summary>
        /// Raw access available, but it is recommended to use GetDbKey() or SetDbKey() instead for derived class override safety.
        /// </summary>
        [DataLabsFilterable]
        public int DbKey => m_dbKey;

        [FormerlySerializedAs("m_title")] [FormerlySerializedAs("Title")] [SerializeField]
        [Content]
        public string title = "Blank";

        [DataLabsFilterable]
        public string Title
        {
            get => title;
            set => title = value;
        }
        
        [FormerlySerializedAs("m_description")]
        [TextArea(3, 6)] [FormerlySerializedAs("Description")] [SerializeField]
        [Content]
        public string description;

        public string Description
        {
            get => description;
            set => description = value;
        }

        [SerializeField] public Sprite GetDataIcon => GetDataIconInternal();

        // [ValueDropdown("GetSheetProfiles")] [ShowInInspector]
        // public Voidex.DataLabs.GoogleSheets.Editor.Profile SheetProfile;
        //
        // private IEnumerable<ValueDropdownItem> GetSheetProfiles()
        // {
        //     if (Voidex.DataLabs.GoogleSheets.Editor.GoogleSheetSettings.instance != null)
        //     {
        //         return Voidex.DataLabs.GoogleSheets.Editor.GoogleSheetSettings.instance.Profile.Profiles.Select(p => new ValueDropdownItem(p.ProfileName, p));
        //     }
        //
        //     return new List<ValueDropdownItem>();
        // }

        //TODO: Add open google sheet button
        //TODO: Button to add new profile
        //TODO: Button to fetch data from google sheet
        //TODO: Button to push data to google sheet: Create an Exporter class to do this
        
        //[Button(ButtonSizes.Medium)][GUIColor(0.8f, 0.8f, 1f)]
        // private void OpenGoogleSheet()
        // {
        //     if (SheetProfile == null)
        //     {
        //         Debug.LogWarning("No Sheet Profile selected.");
        //         return;
        //     }
        //     Application.OpenURL("https://docs.google.com/spreadsheets/d/" + SheetProfile.SheetID);
        // }
        
        protected virtual void Reset()
        {
            Title = $"UNASSIGNED.{System.DateTime.Now.TimeOfDay.TotalMilliseconds}";
            Description = "";
        }


        /// <summary>
        /// Get the Database Key for this Entity.
        /// </summary>
        public int GetDbKey()
        {
            return m_dbKey;
        }

        /// <summary>
        /// Set the Database Key for this Entity.
        /// </summary>
        public void SetDbKey(int id)
        {
            m_dbKey = id;
        }

        /// <summary>
        /// Typically used in the Editor to display an icon for the Asset List. Can be used for other things at runtime if desired.
        /// </summary>
        protected virtual Sprite GetDataIconInternal()
        {
            return null;
        }

        [Button]
        private void SetDbKeyManually(int id)
        {
            int newId = GenerateUniqueId(id);
            m_dbKey = newId;
        }
        
        public int GenerateUniqueId(int m_uniqueIdIterator)
        {
            int result = 0;
            bool done = false;
            while (!done)
            {
                result = m_uniqueIdIterator++;
                if (!DataLab.Db.Data.ContainsKey(result)) done = true;
            }

            return result;
        }
    }
}