using System;
using System.Collections.Generic;
using UnityEngine;

namespace Voidex.DataLabs
{
    public static class DataLab
    {
        /// <summary>
        /// All of the references to project assets. Not recommended to access directly. Use the DataLab class methods or create your own extension methods, such as non-linear search types.
        /// </summary>
        public static LabsDatabase Db
        {
            get
            {
                if (m_db == null) FindDb();
                return m_db;
            }
            set => m_db = value;
        }

        private static LabsDatabase m_db;

        /// <summary>
        /// Directly query the database for a specific key. This is the most efficient way to access data.
        /// </summary>
        /// <param name="key">The item ID</param>
        /// <returns>A reference to the <see cref="DataEntity"/>.</returns>
        public static DataEntity Get(int key)
        {
            return Db.Get(key);
        }
        public static DataEntity Get<T>(int key) where T:DataEntity
        {
            return Db.Get(key) as T;
        }

        /// <summary>
        /// Slow way to get every item of a specific type.
        /// </summary>
        /// <typeparam name="T">The type of Items you want.</typeparam>
        /// <returns>All of the items that are of the given type.</returns>
        public static List<T> GetAll<T>() where T : DataEntity
        {
            return Db.GetAll<T>();
        }

        private static void FindDb()
        {
            Db = (LabsDatabase) Resources.Load(LabsDatabase.DataLabsDatabaseName);
            //TODO: change to addressables
        }
    }
}