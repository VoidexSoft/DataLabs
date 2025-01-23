using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Voidex.DataLabs.GoogleSheets.Editor
{
    [CreateAssetMenu(fileName = "DataLabsSheetsProfile", menuName = "Voidex/DataLabs/GoogleSheets/Profile")]
    public class DataLabsSheetsProfile : ScriptableObject, IReadOnlyCollection<Profile>
    {
        public Profile this[int index]
        {
            get
            {
                if (index < 0 || index >= Count)
                {
                    throw new IndexOutOfRangeException();
                }

                return m_profiles[index];
            }
        }

        public int Count
        {
            get { return m_profiles.Count; }
        }

        [SerializeField] private List<Profile> m_profiles = new List<Profile>();
        [HideInInspector, SerializeField] private int m_nextProfileIndex = 0;

        public List<Profile>? Profiles => m_profiles;

        public void AddNewProfile()
        {
            m_profiles.Add(new Profile()
            {
                ProfileName = $"Profile{m_nextProfileIndex++}"
            });
        }

        public void RemoveProfile(int index)
        {
            m_profiles.RemoveAt(index);
        }

        public IEnumerator<Profile> GetEnumerator()
        {
            return ((IEnumerable<Profile>) m_profiles).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable) m_profiles).GetEnumerator();
        }
    }
    
    [Serializable]
    public partial class Profile
    {
        public string ProfileName;
        public string SheetID;
        public string AssetType = "None";
        public string AssetDirectory = "Assets/Data";
        [ListDrawerSettings(ShowIndexLabels = true, DraggableItems = false)]
        public string[] WorksheetNames = new string[1] {"Sheet0"};


        [Button]
        [GUIColor(0.8f, 0.8f, 1)]
        private void OpenGoogleSheet()
        {
            if (!string.IsNullOrEmpty(SheetID))
            {
                Application.OpenURL("https://docs.google.com/spreadsheets/d/" + SheetID);
            }
        }
    }
}