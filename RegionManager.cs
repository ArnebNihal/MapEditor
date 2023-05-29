using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using DaggerfallConnect;
using DaggerfallConnect.Arena2;
using DaggerfallConnect.InternalTypes;
using DaggerfallConnect.Utility;
using UnityEditor.Localization.Plugins.XLIFF.V12;
using Unity.Mathematics.Editor;
using DaggerfallWorkshop.Game.Items;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Game.UserInterfaceWindows;
using Microsoft.Unity.VisualStudio.Editor;
using UnityEditor.Build.Pipeline.Tasks;
using UnityEngine.EventSystems;
using Newtonsoft.Json;
using System.Linq;

namespace MapEditor
{
    public class RegionManager : EditorWindow
    {
        static RegionManager regionManagerWindow;
        const string windowTitle = "Region Manager";
        public static int currentRegionIndex = 0;
        public string[] regionNames;
        public RegionData currentRegionData;

        public class RegionData
        {
            public (int, int) capitalPosition;
            public ushort governType;
            public ushort deity;
            public ushort surface;
            public int elevationSum;
            public int highestPoint;
            public int lowestPoint = 4;
            public ulong highestLocationId = 0;
            public ulong lowestLocationId = 999999;
            public ushort[] locations; // contains count of each different kind of location, ordered as LocationTypes
            public ushort[] climates; // same as locations, but for climates
        }

        void Update()
        {

        }

        void Awake()
        {
            SetRegionNames();
            AnalyzeSelectedRegion();
        }

        void OnGUI()
        {
            int oldRegionIndex = currentRegionIndex;
            currentRegionIndex = EditorGUILayout.Popup("Region: ", currentRegionIndex, regionNames, GUILayout.MaxWidth(300.0f));
            if (currentRegionIndex != oldRegionIndex)
            {
                AnalyzeSelectedRegion();
            }

            GUILayout.Label(regionNames[currentRegionIndex], EditorStyles.boldLabel);
            GUILayout.Space(50.0f);
            EditorGUILayout.LabelField("Capital city: ", "");
            EditorGUILayout.LabelField("Govern Type: ", "");
            EditorGUILayout.LabelField("Deity: ", "");
            GUILayout.Space(20.0f);
            EditorGUILayout.LabelField("Surface: ", currentRegionData.surface + " units");
            GUILayout.Space(20.0f);
            EditorGUILayout.LabelField("Average Elevation: ", (currentRegionData.elevationSum / currentRegionData.surface).ToString());
            EditorGUILayout.LabelField("Highest Point: ", (currentRegionData.highestPoint).ToString());
            EditorGUILayout.LabelField("Lowest Point: ", (currentRegionData.lowestPoint).ToString());
            GUILayout.Space(30.0f);

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Cities: ", currentRegionData.locations[(int)DFRegion.LocationTypes.TownCity].ToString());
            EditorGUILayout.LabelField(((float)currentRegionData.locations[(int)DFRegion.LocationTypes.TownCity] * 100.0f / (float)currentRegionData.surface).ToString(), "%");
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Hamlets: ", currentRegionData.locations[(int)DFRegion.LocationTypes.TownHamlet].ToString());
            EditorGUILayout.LabelField(((float)currentRegionData.locations[(int)DFRegion.LocationTypes.TownHamlet] * 100.0f / (float)currentRegionData.surface).ToString(), "%");
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Villages: ", currentRegionData.locations[(int)DFRegion.LocationTypes.TownVillage].ToString());
            EditorGUILayout.LabelField(((float)currentRegionData.locations[(int)DFRegion.LocationTypes.TownVillage] * 100.0f / (float)currentRegionData.surface).ToString(), "%");
            GUILayout.EndHorizontal();
            GUILayout.Space(20.0f);
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Wealthy Homes: ", currentRegionData.locations[(int)DFRegion.LocationTypes.HomeWealthy].ToString());
            EditorGUILayout.LabelField(((float)currentRegionData.locations[(int)DFRegion.LocationTypes.HomeWealthy] * 100.0f / (float)currentRegionData.surface).ToString(), "%");
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Hovels: ", currentRegionData.locations[(int)DFRegion.LocationTypes.HomePoor].ToString());
            EditorGUILayout.LabelField(((float)currentRegionData.locations[(int)DFRegion.LocationTypes.HomePoor] * 100.0f / (float)currentRegionData.surface).ToString(), "%");
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Taverns: ", currentRegionData.locations[(int)DFRegion.LocationTypes.Tavern].ToString());
            EditorGUILayout.LabelField(((float)currentRegionData.locations[(int)DFRegion.LocationTypes.Tavern] * 100.0f / (float)currentRegionData.surface).ToString(), "%");
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Farms: ", currentRegionData.locations[(int)DFRegion.LocationTypes.HomeFarms].ToString());
            EditorGUILayout.LabelField(((float)currentRegionData.locations[(int)DFRegion.LocationTypes.HomeFarms] * 100.0f / (float)currentRegionData.surface).ToString(), "%");
            GUILayout.EndHorizontal();
            GUILayout.Space(20.0f);
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Temples: ", currentRegionData.locations[(int)DFRegion.LocationTypes.ReligionTemple].ToString());
            EditorGUILayout.LabelField(((float)currentRegionData.locations[(int)DFRegion.LocationTypes.ReligionTemple] * 100.0f / (float)currentRegionData.surface).ToString(), "%");
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Cult Sites: ", currentRegionData.locations[(int)DFRegion.LocationTypes.ReligionCult].ToString());
            EditorGUILayout.LabelField(((float)currentRegionData.locations[(int)DFRegion.LocationTypes.ReligionCult] * 100.0f / (float)currentRegionData.surface).ToString(), "%");
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Witch Covens: ", currentRegionData.locations[(int)DFRegion.LocationTypes.Coven].ToString());
            EditorGUILayout.LabelField(((float)currentRegionData.locations[(int)DFRegion.LocationTypes.Coven] * 100.0f / (float)currentRegionData.surface).ToString(), "%");
            GUILayout.EndHorizontal();
            GUILayout.Space(20.0f);
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Large Dungeons: ", currentRegionData.locations[(int)DFRegion.LocationTypes.DungeonLabyrinth].ToString());
            EditorGUILayout.LabelField(((float)currentRegionData.locations[(int)DFRegion.LocationTypes.DungeonLabyrinth] * 100.0f / (float)currentRegionData.surface).ToString(), "%");
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Medium Dungeons: ", currentRegionData.locations[(int)DFRegion.LocationTypes.DungeonKeep].ToString());
            EditorGUILayout.LabelField(((float)currentRegionData.locations[(int)DFRegion.LocationTypes.DungeonKeep] * 100.0f / (float)currentRegionData.surface).ToString(), "%");
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Small Dungeons: ", currentRegionData.locations[(int)DFRegion.LocationTypes.DungeonRuin].ToString());
            EditorGUILayout.LabelField(((float)currentRegionData.locations[(int)DFRegion.LocationTypes.DungeonRuin] * 100.0f / (float)currentRegionData.surface).ToString(), "%");
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Graveyards: ", currentRegionData.locations[(int)DFRegion.LocationTypes.Graveyard].ToString());
            EditorGUILayout.LabelField(((float)currentRegionData.locations[(int)DFRegion.LocationTypes.Graveyard] * 100.0f / (float)currentRegionData.surface).ToString(), "%");
            GUILayout.EndHorizontal();
            GUILayout.Space(20.0f);

            int allLocationsCount = 0;
            for (int i = 0; i < Enum.GetNames(typeof(DFRegion.LocationTypes)).Length; i++)
            {
                allLocationsCount += currentRegionData.locations[i];
            }
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("All Locations: ", allLocationsCount.ToString());
            EditorGUILayout.LabelField(((float)allLocationsCount * 100.0f / (float)currentRegionData.surface).ToString(), "%");
            GUILayout.EndHorizontal();
            EditorGUILayout.LabelField("Location Id range: ", currentRegionData.lowestLocationId + " - " + currentRegionData.highestLocationId);
            GUILayout.Space(30.0f);

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Ocean: ", "");
            EditorGUILayout.LabelField(((float)currentRegionData.climates[0] * 100.0f / (float)currentRegionData.surface).ToString(), "%");
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Desert: ", "");
            EditorGUILayout.LabelField(((float)currentRegionData.climates[(int)MapsFile.Climates.Desert - (int)MapsFile.Climates.Ocean] * 100.0f / (float)currentRegionData.surface).ToString(), "%");
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Desert2: ", "");
            EditorGUILayout.LabelField(((float)currentRegionData.climates[(int)MapsFile.Climates.Desert2 - (int)MapsFile.Climates.Ocean] * 100.0f / (float)currentRegionData.surface).ToString(), "%");
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Mountain: ", "");
            EditorGUILayout.LabelField(((float)currentRegionData.climates[(int)MapsFile.Climates.Mountain - (int)MapsFile.Climates.Ocean] * 100.0f / (float)currentRegionData.surface).ToString(), "%");
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Rainforest: ", "");
            EditorGUILayout.LabelField(((float)currentRegionData.climates[(int)MapsFile.Climates.Rainforest - (int)MapsFile.Climates.Ocean] * 100.0f / (float)currentRegionData.surface).ToString(), "%");
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Swamp: ", "");
            EditorGUILayout.LabelField(((float)currentRegionData.climates[(int)MapsFile.Climates.Swamp - (int)MapsFile.Climates.Ocean] * 100.0f / (float)currentRegionData.surface).ToString(), "%");
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Subtropical: ", "");
            EditorGUILayout.LabelField(((float)currentRegionData.climates[(int)MapsFile.Climates.Subtropical - (int)MapsFile.Climates.Ocean] * 100.0f / (float)currentRegionData.surface).ToString(), "%");
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Mountain Woods: ", "");
            EditorGUILayout.LabelField(((float)currentRegionData.climates[(int)MapsFile.Climates.MountainWoods - (int)MapsFile.Climates.Ocean] * 100.0f / (float)currentRegionData.surface).ToString(), "%");
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Woodlands: ", "");
            EditorGUILayout.LabelField(((float)currentRegionData.climates[(int)MapsFile.Climates.Woodlands - (int)MapsFile.Climates.Ocean] * 100.0f / (float)currentRegionData.surface).ToString(), "%");
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Haunted Woodlands: ", "");
            EditorGUILayout.LabelField(((float)currentRegionData.climates[(int)MapsFile.Climates.HauntedWoodlands - (int)MapsFile.Climates.Ocean] * 100.0f / (float)currentRegionData.surface).ToString(), "%");
            GUILayout.EndHorizontal();
        }

        protected void SetRegionNames()
        {
            regionNames = new string[MapsFile.TempRegionCount];

            for (int i = 0; i < MapsFile.TempRegionCount; i++)
            {
                regionNames[i] = MapsFile.RegionNames[i];
            }
        }

        protected void AnalyzeSelectedRegion()
        {
            currentRegionData = new RegionData();
            currentRegionData.locations = new ushort[Enum.GetNames(typeof(DFRegion.LocationTypes)).Length];
            currentRegionData.climates = new ushort[Enum.GetNames(typeof(MapsFile.Climates)).Length];

            for (int x = 0; x < MapsFile.WorldWidth; x++)
            {
                for (int y = 0; y < MapsFile.WorldHeight; y++)
                {
                    int index = PoliticInfo.ConvertMapPixelToRegionIndex(x, y);

                    if (index == currentRegionIndex)
                    {
                        currentRegionData.surface++;
                        currentRegionData.elevationSum += (int)SmallHeightmap.GetHeightMapValue(x, y);
                        if ((int)SmallHeightmap.GetHeightMapValue(x, y) > currentRegionData.highestPoint)
                            currentRegionData.highestPoint = (int)SmallHeightmap.GetHeightMapValue(x, y);
                        if ((int)SmallHeightmap.GetHeightMapValue(x, y) < currentRegionData.lowestPoint)
                            currentRegionData.lowestPoint = (int)SmallHeightmap.GetHeightMapValue(x, y);

                        if (Worldmaps.HasLocation(x, y))
                        {
                            MapSummary summary = new MapSummary();
                            Worldmaps.HasLocation(x, y, out summary);

                            currentRegionData.locations[(int)summary.LocationType]++;

                            if (summary.MapID > currentRegionData.highestLocationId)
                                currentRegionData.highestLocationId = summary.MapID;
                            if (summary.MapID < currentRegionData.lowestLocationId)
                                currentRegionData.lowestLocationId = summary.MapID;
                        }
                        currentRegionData.climates[(ClimateInfo.Climate[x, y] - (int)MapsFile.Climates.Ocean)]++;
                    }
                }
            }
        }
    }
}