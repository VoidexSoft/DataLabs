﻿using System;
using System.Collections.Generic;
using Google.Apis.Sheets.v4.Data;
using UnityEditor;
using UnityEngine;
using Voidex.DataLabs.GoogleSheets.Runtime;

namespace Voidex.DataLabs.GoogleSheets.Editor
{
    public interface IImportUtility : ILogger
    {
        /// <summary>
        /// Object containing imported data retrieved from a GoogleSheet
        /// </summary>
        List<DataSheet> DataSheets { get; }
        
        Spreadsheet Spreadsheet { get; }

        /// <summary>
        /// The PrimaryKey specified on the class' <see cref="ContentAssetAttribute"/>
        /// </summary>
        string PrimaryKey { get; }

        /// <summary>
        /// Directory relative to the project folder where any assets should be created
        /// </summary>
        string AssetDirectory { get; }

        string BuildAssetPath(string classTitle, string workSheetName, string extension = ".asset", string assetDirectory = "");
        
        

        /// <summary>
        /// Finds or Creates an Asset of the specified type at the specified path
        /// </summary>
        /// <typeparam name="T">type of asset to find</typeparam>
        /// <param name="path">path to find or create an asset at</param>
        /// <returns>the found or newly created asset</returns>
        T FindOrCreateAsset<T>(string path) where T : ScriptableObject;

        ScriptableObject FindOrCreateAsset(Type type, string path);

        /// <summary>
        /// Looks for an asset of the given type with the given name in the <see cref="AssetDatabase"/>
        /// </summary>
        /// <typeparam name="T">type of asset to find</typeparam>
        /// <param name="name">name of the asset to find</param>
        /// <param name="asset">the resulting asset if found, null if not found</param>
        /// <returns>TRUE if the asset was found, FALSE if the asset was not found</returns>
        bool FindAssetByName<T>(string name, out T asset) where T : UnityEngine.Object;

        bool FindAssetByName(Type type, string name, out UnityEngine.Object asset);

        void Reset(List<DataSheet> dataSheets, Spreadsheet spreadsheet, string primaryKey, string assetDirectory);
    }
}