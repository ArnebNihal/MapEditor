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
using Unity.PlasticSCM.Editor.UI;
using DaggerfallWorkshop.Utility;

namespace MapEditor
{
    //[CustomEditor(typeof(MapEditor))]
    public class MapEditor : EditorWindow
    {
        static MapEditor window;
        const string windowTitle = "Map Editor";
        const string menuPath = "Daggerfall Tools/Map Editor";
        public static Vector2 mousePosition;

        // Map alpha channel variables and const
        public static float mapAlphaChannel = 255.0f;
        const float level1 = 255.0f;
        const float level2 = 200.0f;
        const float level3 = 180.0f;
        const float level4 = 160.0f;

        string worldName = "Tamriel";
        bool groupEnabled;

        //Levels and tools
        static bool heightmap = false;
        static bool climate = false;
        static bool politics = false;
        static bool locations = false;
        static bool mapImage = true;
        static bool grid = false;

        //Rects and Rect constants
        static Rect mapView;
        static Rect heightmapRect;
        static Rect townBlocksRect;
        const int heightmapOriginX = 0;
        const int heightmapOriginY = 0;
        const int heightmapRectWidth = MapsFile.WorldWidth;
        const int heightmapRectHeight = MapsFile.WorldHeight;
        const float setMovement = 0.01f;
        const float dataField = 600.0f;
        const float dataFieldBig = 1000.0f;
        const float dataFieldSmall = 200.0f;
        const string noBlock = "[__________]";

        const string buttonUp = "UP";
        const string buttonDown = "DOWN";
        const string buttonLeft = "LEFT";
        const string buttonRight = "RIGHT";
        const string buttonReset = "RESET";
        const string buttonZoomIn = "ZOOM IN";
        const string buttonZoomOut = "ZOOM OUT";
        static Rect layerPosition = new Rect(-500, -500, 1, 1);
        Texture2D referenceMap;
        Texture2D heightMap;
        Texture2D locationsMap;
        Texture2D climateMap;
        Texture2D politicMap;
        static GUIStyle guiStyle = new GUIStyle();
        static float zoomLevel = 1.0f;

        // mapReference constants
        const int mapReferenceWidth = 3440;
        const int mapReferenceHeight = 2400;
        const int mapReferenceMCD = 80;
        const int mapRefWidthUnit = mapReferenceWidth / mapReferenceMCD;
        const int mapRefHeightUnit = mapReferenceHeight / mapReferenceMCD;
        const int mapReferenceMultiplier = 25;
        const float layerOriginX = 0.2639999f;
        const float layerOriginY = 0.5899986f;
        const float startingWidth = 0.1622246f;
        const float startingHeight = 0.1943558f;
        const float widthProportion = 0.2575f;
        const float heightProportion = 0.3085f;
        const float proportionMultiplier = 1.5f;

        public static PixelData pixelData;
        public PixelData modifiedPixelData;
        public Vector2 pixelCoordinates;
        public static bool pixelSelected = false;
        public bool exteriorContent = false;
        public bool dungeonContent = false;
        public bool buildingList = false;
        public bool blockList = false;
        public bool widthModified = false;
        public bool heightModified = false;
        public int width = 0;
        public int height = 0;
        public Vector2 buildingScroll;
        public Vector2 townBlocksScroll;
        public Vector2 dungeonScroll;
        public string[] regionNames;
        public string[] climateNames;
        public static readonly string[] locationTypes = {
            "City", "Hamlet", "Village", "Farm", "Dungeon Labyrinth", "Temple", "Tavern", "Dungeon Keep", "Wealthy Home", "Cult", "Dungeon Ruin", "Home Poor", "Graveyard", "Coven"
        };

        public static readonly string[] dungeonTypes = {
            "Crypt", "Orc Stronghold", "Human Stronghold", "Prison", "Desecrated Temple", "Mine", "Natural Cave", "Coven", "Vampire Haunt", "Laboratory", "Harpy Nest", "Ruined Castle", "Spider Nest", "Giant Stronghold", "Dragon's Den", "Barbarian Stronghold", "Volcanic Caves", "Scorpion Nest", "Cemetery", "No Dungeon"
        };

        public static readonly string[] buildingTypes = {
            "Alchemist", "House for Sale", "Armorer", "Bank", "Town4", "Bookseller", "Clothing Store", "Furniture Store", "Gem Store", "General Store", "Library", "Guild Hall", "Pawn Shop", "Weapon Smith", "Temple", "Tavern", "Palace", "House1", "House2", "House3", "House4", "House5", "House6"
        };

        BlocksFile blockFileReader;
        public string[] RMBBlocks;

        public string[] townBlocks;

        public string worldSavePath;
        public string sourceFilesPath;
        public const string testPath = "/home/arneb/Games/daggerfall/DaggerfallGameFiles/arena2/Maps/Tamriel/";
        public const string arena2Path = "/home/arneb/Games/daggerfall/DaggerfallGameFiles/arena2";
        public int availableBlocks = 0;
        public int selectedX = 0;
        public int selectedY = 0;
        public int selectedCoordinates = 0;
        public Dictionary<int,string> modifiedTownBlocks = new Dictionary<int,string>();
        
        public static void ShowWindow() 
        {
            EditorWindow.GetWindow(typeof(MapEditor));
        }

        void OnEnable()
        {

        }

        void OnDisable()
        {

        }

        void OnValidate()
        {
            Debug.Log("Setting map alpha channel");
        }

        void OnBackingScaleFactorChanged()
        {
            layerPosition = UpdateLayerPosition();
            Graphics.DrawTexture(mapView, referenceMap, layerPosition, 0, 0, 0, 0);
        }

        [MenuItem(menuPath)]
        static void Init()
        {
            window = (MapEditor)EditorWindow.GetWindow(typeof(MapEditor));
            window.titleContent = new GUIContent(windowTitle);
        }

        void Update()
        {

        }

        public void LoadMapFile(string path)
        {
            
        }

        void Awake()
        {
            guiStyle = SetGUIStyle();

            SetHeightmapRect();

            SetMaps();

            SetLocationsMap();

            SetRegionNames();
            SetClimateNames();
            ResetSelectedCoordinates();

            if (blockFileReader == null)
                blockFileReader = new BlocksFile(Path.Combine(arena2Path, BlocksFile.Filename), FileUsage.UseMemory, true);

            RMBBlocks = SetRMBBlocks();

            pixelData = new PixelData();
            modifiedPixelData = new PixelData();

            string path = "Assets/Scripts/Editor/3170483-1327182302.jpg";
            referenceMap = new Texture2D(3440, 2400);
            referenceMap = new Texture2D(referenceMap.width, referenceMap.height, TextureFormat.ARGB32, false, true);
            ImageConversion.LoadImage(referenceMap, File.ReadAllBytes(path));
            layerPosition = new Rect(layerOriginX, layerOriginY, startingWidth, startingHeight);
        }

        void OnGUI()
        {
            GUILayout.Label("Base Settings", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            worldName = EditorGUILayout.TextField("World Name", worldName, GUILayout.MaxWidth(dataFieldBig));
            if (GUILayout.Button("Open Region Manager", GUILayout.MaxWidth(dataFieldSmall)))
            {
                OpenRegionManager();
            }

            if (GUILayout.Button("Open Block Inspector", GUILayout.MaxWidth(dataFieldSmall)))
            {
                OpenBlockInspector();
            }
            EditorGUILayout.EndHorizontal();
            // if (GUILayout.Button("Set as current world", GUILayout.MaxWidth(200)))
            // {
            //     SetCurrentWorld();
            // }

            groupEnabled = EditorGUILayout.BeginToggleGroup("Unlock map editing", groupEnabled);

            Rect r = EditorGUILayout.BeginHorizontal(GUILayout.Height(30));
            heightmap = EditorGUILayout.ToggleLeft("Heightmap", heightmap);
            climate = EditorGUILayout.ToggleLeft("Climate", climate);
            politics = EditorGUILayout.ToggleLeft("Politics", politics);
            locations = EditorGUILayout.ToggleLeft("Locations", locations);
            mapImage = EditorGUILayout.ToggleLeft("Map Reference", mapImage);
            EditorGUILayout.EndHorizontal();

            r = EditorGUILayout.BeginHorizontal(GUILayout.Height(MapsFile.WorldHeight / 4 * 3));
            mapView = EditorGUILayout.GetControlRect(false, 0.0f, GUILayout.Width(MapsFile.WorldWidth / 4 * 3), GUILayout.Height(MapsFile.WorldHeight / 4 * 3));
            mapView.x += 5.0f;

            if (mapImage)
            {
                Graphics.DrawTexture(mapView, referenceMap, layerPosition, 0, 0, 0, 0);
            }

            if (heightmap)
            {
                Graphics.DrawTexture(mapView, heightMap, heightmapRect, 0, 0, 0, 0);
            }

            if (climate)
            {
                Graphics.DrawTexture(mapView, climateMap, heightmapRect, 0, 0, 0, 0);
            }

            if (politics)
            {
                Graphics.DrawTexture(mapView, politicMap, heightmapRect, 0, 0, 0, 0);
            }

            if (locations)
            {
                Graphics.DrawTexture(mapView, locationsMap, heightmapRect, 0, 0, 0, 0);
            }

            if (grid)
            {

            }

            if (mousePosition == null)
                mousePosition = Vector2.zero;

            mousePosition = GetMouseCoordinates();

            float tempMapAlphaChannel = SetMapAlphaChannel();
            if (mapAlphaChannel != tempMapAlphaChannel)
            {
                mapAlphaChannel = tempMapAlphaChannel;
                SetMaps();
            }

            EditorGUILayout.BeginVertical();

            if (!pixelSelected)
            {
                string mousePos = ((int)mousePosition.x).ToString() + ", " + ((int)mousePosition.y).ToString();
                EditorGUILayout.LabelField("Coordinates: ", mousePos);

                if (pixelData.hasLocation)
                {
                    EditorGUILayout.LabelField("Location Name: ", pixelData.Name);
                    EditorGUILayout.LabelField("Region Name: ", pixelData.RegionName);
                    EditorGUILayout.LabelField("Map ID: ", pixelData.MapId.ToString());
                    EditorGUILayout.LabelField("Latitude: ", pixelData.Latitude.ToString());
                    EditorGUILayout.LabelField("Longitude", pixelData.Longitude.ToString());
                    EditorGUILayout.LabelField("Location Type: ", ((DFRegion.LocationTypes)pixelData.LocationType).ToString());
                    EditorGUILayout.LabelField("Dungeon Type: ", ((DFRegion.DungeonTypes)pixelData.DungeonType).ToString());
                    EditorGUILayout.LabelField("Key: ", pixelData.Key.ToString());
                    EditorGUILayout.LabelField("Politic: ", pixelData.Politic.ToString());
                    EditorGUILayout.LabelField("Region Index: ", pixelData.RegionIndex.ToString());
                    EditorGUILayout.LabelField("Location Index: ", pixelData.LocationIndex.ToString());
                }

                EditorGUILayout.LabelField("Elevation: ", pixelData.Elevation.ToString());

                if (pixelData.Region == 64)
                    EditorGUILayout.LabelField("Region: ", "Water body");
                else EditorGUILayout.LabelField("Region: ", regionNames[pixelData.Region]);
                
                EditorGUILayout.LabelField("Climate: ", climateNames[pixelData.Climate]);
            }

            if (pixelSelected)
            {
                string mousePos = ((int)pixelCoordinates.x).ToString() + ", " + ((int)pixelCoordinates.y).ToString();
                EditorGUILayout.LabelField("Coordinates: ", mousePos.ToString());

                if (modifiedPixelData.hasLocation)
                {
                    modifiedPixelData.Name = EditorGUILayout.TextField("Location Name: ", modifiedPixelData.Name, GUILayout.MaxWidth(dataField));
                    EditorGUILayout.LabelField("Region Name: ", modifiedPixelData.RegionName);
                    EditorGUILayout.LabelField("Map ID: ", modifiedPixelData.MapId.ToString());
                    EditorGUILayout.LabelField("Latitude: ", modifiedPixelData.Latitude.ToString());
                    EditorGUILayout.LabelField("Longitude", modifiedPixelData.Longitude.ToString());
                    modifiedPixelData.LocationType = EditorGUILayout.Popup("Location Type: ", modifiedPixelData.LocationType, locationTypes, GUILayout.MaxWidth(dataField));
                    modifiedPixelData.DungeonType = EditorGUILayout.Popup("Dungeon Type: ", modifiedPixelData.DungeonType, dungeonTypes, GUILayout.MaxWidth(dataField));
                    EditorGUILayout.LabelField("Key: ", modifiedPixelData.Key.ToString());

                    exteriorContent = EditorGUILayout.Foldout(exteriorContent, "Exterior Data");
                    if (exteriorContent)
                    {
                        EditorGUILayout.LabelField("X: ", modifiedPixelData.exterior.X.ToString());
                        EditorGUILayout.LabelField("Y: ", modifiedPixelData.exterior.Y.ToString());
                        EditorGUILayout.LabelField("Location ID: ", modifiedPixelData.exterior.LocationId.ToString());
                        EditorGUILayout.LabelField("Exterior Location ID: ", modifiedPixelData.exterior.ExteriorLocationId.ToString());
                        EditorGUILayout.LabelField("Building Count: ", modifiedPixelData.exterior.BuildingCount.ToString());

                        if (modifiedPixelData.exterior.BuildingCount > 0)
                        {
                            buildingList = EditorGUILayout.Foldout(buildingList, "Building List");
                            if (buildingList)
                            {
                                buildingScroll = EditorGUILayout.BeginScrollView(buildingScroll);
                                for (int building = 0; building < modifiedPixelData.exterior.BuildingCount; building++)
                                {
                                    modifiedPixelData.exterior.buildings[building].NameSeed = EditorGUILayout.IntField("Name Seed: ", modifiedPixelData.exterior.buildings[building].NameSeed, GUILayout.MaxWidth(dataField));

                                    EditorGUILayout.BeginHorizontal();
                                    modifiedPixelData.exterior.buildings[building].FactionId = EditorGUILayout.IntField("Faction ID: ", modifiedPixelData.exterior.buildings[building].FactionId, GUILayout.MaxWidth(dataField));
                                    EditorGUILayout.LabelField(" ", ((FactionFile.FactionIDs)modifiedPixelData.exterior.buildings[building].FactionId).ToString());
                                    EditorGUILayout.EndHorizontal();
                                    EditorGUILayout.LabelField("Sector: ", modifiedPixelData.exterior.buildings[building].Sector.ToString());
                                    modifiedPixelData.exterior.buildings[building].BuildingType = EditorGUILayout.Popup("Building Type: ", modifiedPixelData.exterior.buildings[building].BuildingType, buildingTypes, GUILayout.MaxWidth(dataField));
                                    modifiedPixelData.exterior.buildings[building].Quality = EditorGUILayout.IntSlider("Quality: ", modifiedPixelData.exterior.buildings[building].Quality, 1, 20, GUILayout.MaxWidth(dataField));
                                    EditorGUILayout.Space();
                                }
                                EditorGUILayout.EndScrollView();
                            }
                        }

                        if (!widthModified)
                        {
                            width = EditorGUILayout.IntSlider("Width: ", modifiedPixelData.exterior.Width, 1, 8, GUILayout.MaxWidth(dataField));
                            widthModified = true;
                        }
                        width = EditorGUILayout.IntSlider("Width: ", width, 1, 8, GUILayout.MaxWidth(dataField));

                        if (!heightModified)
                        {
                            height = EditorGUILayout.IntSlider("Height: ", modifiedPixelData.exterior.Height, 1, 8, GUILayout.MaxWidth(dataField));
                            heightModified = true;
                        }
                        height = EditorGUILayout.IntSlider("Height: ", height, 1, 8, GUILayout.MaxWidth(dataField));

                        modifiedPixelData.exterior.PortTown = EditorGUILayout.Toggle("Port Town", modifiedPixelData.exterior.PortTown);

                        townBlocks = new string[width * height];

                        int counter = 0;
                        int offset = 0;
                        for (int i = 0; i < townBlocks.Length; i++)
                        {
                            if (counter >= modifiedPixelData.exterior.Width && width > modifiedPixelData.exterior.Width)
                            {
                                // ResetSelectedCoordinates();
                                counter = width - modifiedPixelData.exterior.Width;
                                do
                                {
                                    townBlocks[i + offset] = noBlock;
                                    counter--;
                                    offset++;
                                }
                                while (counter > 0);
                            }
                            counter++;

                            if (i < modifiedPixelData.exterior.BlockNames.Length && townBlocks.Length > (i + offset))
                            {
                                if (modifiedTownBlocks.ContainsKey(i + offset))
                                {
                                    string modTownBlock;
                                    modifiedTownBlocks.TryGetValue((i + offset), out modTownBlock);
                                    townBlocks[i + offset] = modTownBlock;
                                }
                                else townBlocks[i + offset] = modifiedPixelData.exterior.BlockNames[i];
                            }
                            else if (townBlocks.Length <= (i + offset))
                                break;
                            else
                            {
                                for (int j = (i + offset); j < townBlocks.Length; j++)
                                {
                                    townBlocks[j] = noBlock;
                                }
                                break;
                            }
                        }

                        EditorGUILayout.LabelField("Block Names: ");

                        townBlocksScroll = EditorGUILayout.BeginScrollView(townBlocksScroll);

                        int column = 0;
                        int index = 0;
                        int row = (height - 1);

                        EditorGUILayout.BeginHorizontal(GUILayout.MaxWidth(dataField));
                        for (int i = 0; i < townBlocks.Length; i++)
                        {
                            if (i != 0 && (i % width) == 0)
                            {
                                EditorGUILayout.EndHorizontal();
                                row--;
                                column = 0;
                                EditorGUILayout.BeginHorizontal(GUILayout.MaxWidth(dataField));
                            }

                            index = column + (width * row);
                            if (index == selectedCoordinates)
                                EditorGUILayout.LabelField(townBlocks[index], EditorStyles.whiteBoldLabel);
                            else EditorGUILayout.LabelField(townBlocks[index]);
                            column++;
                        }

                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.EndScrollView();

                        EditorGUILayout.BeginHorizontal();
                        availableBlocks = EditorGUILayout.Popup("RMB: ", availableBlocks, RMBBlocks, GUILayout.MaxWidth(dataField));
                        selectedX = EditorGUILayout.IntSlider("Set to block X:", selectedX, 1, width, GUILayout.MaxWidth(dataField));
                        selectedY = EditorGUILayout.IntSlider(" Y:", selectedY, 1, height, GUILayout.MaxWidth(dataField));
                        selectedCoordinates = (selectedX - 1) + (height - selectedY) * width;

                        if (GUILayout.Button("Set Block", GUILayout.MaxWidth(dataFieldSmall)))
                        {
                            if (modifiedTownBlocks.ContainsKey(selectedCoordinates))
                                modifiedTownBlocks.Remove(selectedCoordinates);

                            modifiedTownBlocks.Add(selectedCoordinates, RMBBlocks[availableBlocks]);
                        }
                        EditorGUILayout.EndHorizontal();
                    }

                    if (modifiedPixelData.dungeon.BlockCount > 0)
                    {
                        dungeonContent = EditorGUILayout.Foldout(dungeonContent, "DungeonData");
                        if (dungeonContent)
                        {
                            EditorGUILayout.LabelField("X: ", modifiedPixelData.dungeon.X.ToString());
                            EditorGUILayout.LabelField("Y: ", modifiedPixelData.dungeon.Y.ToString());
                            EditorGUILayout.LabelField("Location ID: ", modifiedPixelData.dungeon.LocationId.ToString());
                            EditorGUILayout.LabelField("Block Count: ", modifiedPixelData.dungeon.BlockCount.ToString());


                            (int, int) x = (0, 0);
                            (int, int) z = (0, 0);
                            for (int block = 0; block < modifiedPixelData.dungeon.BlockCount; block++)
                            {
                                if (modifiedPixelData.dungeon.blocks[block].X < x.Item1)
                                    x.Item1 = modifiedPixelData.dungeon.blocks[block].X;
                                if (modifiedPixelData.dungeon.blocks[block].X > x.Item2)
                                    x.Item2 = modifiedPixelData.dungeon.blocks[block].X;
                                if (modifiedPixelData.dungeon.blocks[block].Z < z.Item1)
                                    z.Item1 = modifiedPixelData.dungeon.blocks[block].Z;
                                if (modifiedPixelData.dungeon.blocks[block].Z > z.Item2)
                                    z.Item2 = modifiedPixelData.dungeon.blocks[block].Z;
                            }
                            EditorGUILayout.LabelField("Block Names: ");

                            dungeonScroll = EditorGUILayout.BeginScrollView(dungeonScroll);
                            EditorGUILayout.BeginHorizontal(GUILayout.MaxWidth(dataField));

                            for (int Z = z.Item1; Z <= z.Item2; Z++)
                            {
                                for (int X = x.Item1; X <= x.Item2; X++)
                                {
                                    bool found = false;
                                    for (int block = 0; block < modifiedPixelData.dungeon.BlockCount; block++)
                                    {
                                        if (modifiedPixelData.dungeon.blocks[block].X == X && modifiedPixelData.dungeon.blocks[block].Z == Z)
                                        {
                                            EditorGUILayout.LabelField(modifiedPixelData.dungeon.blocks[block].BlockName);
                                            found = true;
                                        }
                                    }

                                    if (!found)
                                        EditorGUILayout.LabelField(noBlock);

                                    if (X == x.Item2)
                                    {
                                        EditorGUILayout.EndHorizontal();
                                        EditorGUILayout.BeginHorizontal(GUILayout.MaxWidth(dataField));
                                    }
                                }
                            }
                            EditorGUILayout.EndHorizontal();
                            EditorGUILayout.EndScrollView();
                        }
                    }
                }

                if (!modifiedPixelData.hasLocation)
                    if (GUILayout.Button("Create location", GUILayout.MaxWidth(100)))
                    {
                        SetNewLocation();
                    }

                modifiedPixelData.Elevation = EditorGUILayout.IntField("Elevation: ", modifiedPixelData.Elevation, GUILayout.MaxWidth(dataField));
                modifiedPixelData.Region = EditorGUILayout.Popup("Region: ", modifiedPixelData.Region, regionNames, GUILayout.MaxWidth(dataField));

                modifiedPixelData.Climate = EditorGUILayout.Popup("Climate: ", modifiedPixelData.Climate, climateNames, GUILayout.MaxWidth(dataField));

                EditorGUILayout.BeginHorizontal(GUILayout.Height(30));
                if (GUILayout.Button("Apply Changes", GUILayout.MaxWidth(100)))
                {
                    ApplyChanges();
                    SetMaps();
                }

                if (GUILayout.Button("Revert Changes", GUILayout.MaxWidth(100)))
                {
                    pixelData.GetPixelData((int)pixelCoordinates.x, (int)pixelCoordinates.y);
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();

            r = EditorGUILayout.BeginHorizontal(GUILayout.Height(30));
            grid = EditorGUILayout.ToggleLeft("Grid", grid);
            EditorGUILayout.EndHorizontal();

            r = EditorGUILayout.BeginHorizontal(GUILayout.Height(20));
            if (GUILayout.Button(buttonZoomOut, GUILayout.MaxWidth(100)))
            {
                zoomLevel /= 2.0f;
                layerPosition = DirectionButton(buttonZoomOut, layerPosition, true);
                heightmapRect = DirectionButton(buttonZoomOut, heightmapRect);
            }

            if (GUILayout.Button(buttonUp, GUILayout.MaxWidth(50)))
            {
                layerPosition = DirectionButton(buttonUp, layerPosition, true);
                heightmapRect = DirectionButton(buttonUp, heightmapRect);
            }

            if (GUILayout.Button(buttonZoomIn, GUILayout.MaxWidth(100)))
            {
                zoomLevel *= 2.0f;
                layerPosition = DirectionButton(buttonZoomIn, layerPosition, true);
                heightmapRect = DirectionButton(buttonZoomIn, heightmapRect);
            }
            EditorGUILayout.EndHorizontal();

            r = EditorGUILayout.BeginHorizontal(GUILayout.Height(20));
            if (GUILayout.Button(buttonLeft, GUILayout.MaxWidth(50)))
            {
                layerPosition = DirectionButton(buttonLeft, layerPosition, true);
                heightmapRect = DirectionButton(buttonLeft, heightmapRect);
            }

            GUILayout.Space(25);

            if (GUILayout.Button(buttonReset, GUILayout.MaxWidth(100)))
            {
                zoomLevel = 1.0f;
                layerPosition = new Rect(layerOriginX, layerOriginY, startingWidth, startingHeight);
                heightmapRect = DirectionButton(buttonReset, heightmapRect);
            }

            GUILayout.Space(25);

            if (GUILayout.Button(buttonRight, GUILayout.MaxWidth(50)))
            {
                layerPosition = DirectionButton(buttonRight, layerPosition, true);
                heightmapRect = DirectionButton(buttonRight, heightmapRect);
            }
            EditorGUILayout.EndHorizontal();

            r = EditorGUILayout.BeginHorizontal(GUILayout.Height(20));
            GUILayout.Space(100);
            if (GUILayout.Button(buttonDown, GUILayout.MaxWidth(50)))
            {
                layerPosition = DirectionButton(buttonDown, layerPosition, true);
                heightmapRect = DirectionButton(buttonDown, heightmapRect);
            }
            GUILayout.Space(100);
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("SAVE CURRENT WORLD", GUILayout.MaxWidth(200)))
            {
                SaveCurrentWorld();
            }

            EditorGUILayout.EndToggleGroup();

            Event mouse = Event.current;
            if (groupEnabled && !pixelSelected && mapView.Contains(mouse.mousePosition))
            {
                Vector2 pixel = GetMouseCoordinates();
                if ((int)pixel.x > 0 && (int)pixel.x < MapsFile.WorldWidth && (int)pixel.y > 0 && (int)pixel.y < MapsFile.WorldHeight)
                    pixelData.GetPixelData((int)pixel.x, (int)pixel.y);
            }

            if (groupEnabled && mapView.Contains(mouse.mousePosition) && mouse.button == 0 && mouse.type == EventType.MouseUp)
            {
                modifiedPixelData = new PixelData();
                modifiedPixelData = pixelData;
                pixelCoordinates = GetMouseCoordinates();
                pixelSelected = true;
                widthModified = false;
                heightModified = false;
            }

            if (mapView.Contains(mouse.mousePosition) && mouse.button == 1 && mouse.type == EventType.MouseUp)
            {
                modifiedTownBlocks = new Dictionary<int, string>();
                modifiedPixelData = new PixelData();
                pixelSelected = false;
            }

            window.Repaint();
        }

        protected Vector2 GetMouseCoordinates()
        {
            Vector2 coordinates = Vector2.zero;
            Event mouse = Event.current;
            if (mapView.Contains(mouse.mousePosition))
            {
                coordinates.x = mouse.mousePosition.x - mapView.x;
                coordinates.x = (coordinates.x * ((float)MapsFile.WorldWidth / zoomLevel)) / mapView.width;
                coordinates.x += heightmapRect.x * (float)MapsFile.WorldWidth;

                coordinates.y = mouse.mousePosition.y - mapView.y;
                coordinates.y = (coordinates.y * (float)MapsFile.WorldHeight / zoomLevel) / mapView.height;
                coordinates.y += ((1.0f - heightmapRect.y) * (float)MapsFile.WorldHeight)- (float)MapsFile.WorldHeight / zoomLevel;

                return coordinates;
            }

            else
            {
                coordinates = new Vector2(-1.0f, -1.0f);
                return coordinates;
            };
        }

        protected void SetNewLocation()
        {
            modifiedPixelData.hasLocation = true;
            modifiedPixelData.Name = "";
            modifiedPixelData.MapId = ((ulong)pixelCoordinates.y * MapsFile.OneBillion + (ulong)pixelCoordinates.x);
            modifiedPixelData.Latitude = 0;
            modifiedPixelData.Longitude = 0;
            modifiedPixelData.LocationType = -1;
            modifiedPixelData.DungeonType = 255;
            modifiedPixelData.Key = 0;
            modifiedPixelData.exterior = new Exterior();
            modifiedPixelData.exterior.X = (int)pixelCoordinates.x * MapsFile.WorldMapTerrainDim + (int)modifiedPixelData.Longitude;
            modifiedPixelData.exterior.Y = (int)pixelCoordinates.y * MapsFile.WorldMapTerrainDim + (int)modifiedPixelData.Latitude;
            modifiedPixelData.exterior.LocationId = 0;
            modifiedPixelData.exterior.ExteriorLocationId = 0;
            modifiedPixelData.exterior.BuildingCount = 0;
            modifiedPixelData.exterior.buildings = new Buildings[0];
            modifiedPixelData.exterior.Width = 1;
            modifiedPixelData.exterior.Height = 1;
            modifiedPixelData.exterior.PortTown = false;
            modifiedPixelData.exterior.BlockNames = new string[1];
            modifiedPixelData.exterior.BlockNames[0] = "CUSTAA01.RMB";
            modifiedPixelData.dungeon.X = 0;
            modifiedPixelData.dungeon.Y = 0;
            modifiedPixelData.dungeon.LocationId = 0;
            modifiedPixelData.dungeon.BlockCount = 5;
            modifiedPixelData.dungeon.blocks = new Blocks[5];
            
            (short, short)[] blockPosition = {(0, -1), (-1, 0), (0, 0), (1, 0), (1, 1)};
            for (int i = 0; i < modifiedPixelData.dungeon.BlockCount; i++)
            {
                modifiedPixelData.dungeon.blocks[i] = new Blocks();
                modifiedPixelData.dungeon.blocks[i].X = blockPosition[i].Item1;
                modifiedPixelData.dungeon.blocks[i].Z = blockPosition[i].Item2;

                if (i == 2)
                    modifiedPixelData.dungeon.blocks[i].IsStartingBlock = true;
                else modifiedPixelData.dungeon.blocks[i].IsStartingBlock = false;

                modifiedPixelData.dungeon.blocks[i].WaterLevel = 0;
                modifiedPixelData.dungeon.blocks[i].CastleBlock = false;
            }

            modifiedPixelData.Politic = modifiedPixelData.Region;
            modifiedPixelData.RegionIndex = modifiedPixelData.Politic + 128;
            modifiedPixelData.LocationIndex = 0;
        }

        protected void ApplyChanges()
        {
            if (modifiedPixelData.hasLocation)
            {
                // Converting DFRegion.RegionMapTable
                DFRegion.RegionMapTable modifiedLocation = new DFRegion.RegionMapTable();
                List<DFRegion.RegionMapTable> mapTableList = new List<DFRegion.RegionMapTable>();
                mapTableList = Worldmaps.Worldmap[modifiedPixelData.Region].MapTable.ToList();

                modifiedLocation.MapId = modifiedPixelData.MapId;
                modifiedLocation.Latitude = modifiedPixelData.Latitude;
                modifiedLocation.Longitude = modifiedPixelData.Longitude;
                modifiedLocation.LocationType = (DFRegion.LocationTypes)modifiedPixelData.LocationType;
                modifiedLocation.DungeonType = (DFRegion.DungeonTypes)modifiedPixelData.DungeonType;
                modifiedLocation.Discovered = false;
                modifiedLocation.Key = (uint)modifiedPixelData.Key;

                if (mapTableList.Exists(x => x.MapId == modifiedLocation.MapId))
                    mapTableList.RemoveAll(x => x.MapId == modifiedLocation.MapId);
                mapTableList.Add(modifiedLocation);
                mapTableList.Sort();

                Worldmaps.Worldmap[modifiedPixelData.Region].LocationCount = mapTableList.Count();
                DFLocation[] newLocations = new DFLocation[Worldmaps.Worldmap[modifiedPixelData.Region].LocationCount];

                // Recreating new MapNames, MapIdLookup and MapName Lookup for the region
                bool newLocationAdded = false;
                int counter = 0;
                string[] newMapNames = new string[Worldmaps.Worldmap[modifiedPixelData.Region].LocationCount];
                Dictionary<ulong, int> newMapIdLookup = new Dictionary<ulong, int>();
                Dictionary<string, int> newMapNameLookup = new Dictionary<string, int>();

                foreach (DFRegion.RegionMapTable mapTable in mapTableList)
                {
                    if (newMapIdLookup.ContainsKey(mapTable.MapId))
                        newMapIdLookup.Remove(mapTable.MapId);
                    newMapIdLookup.Add(mapTable.MapId, counter);

                    DFLocation location = new DFLocation();

                    if (!newLocationAdded)
                    {
                        Worldmaps.GetLocation(modifiedPixelData.Region, counter, out location);
                    }
                    else Worldmaps.GetLocation(modifiedPixelData.Region, (counter - 1), out location);

                    if (modifiedLocation.MapId == mapTable.MapId)
                    {
                        DFLocation createdLocation = new DFLocation();
                        createdLocation = GetDFLocationFromPixelData(modifiedPixelData);
                        newLocations[counter] = createdLocation;
                    }
                    if (location.MapTableData.MapId == mapTable.MapId)
                    {
                        if (!newMapNameLookup.ContainsKey(Worldmaps.Worldmap[modifiedPixelData.Region].MapNames[counter]))
                            newMapNameLookup.Add(Worldmaps.Worldmap[modifiedPixelData.Region].MapNames[counter], counter);
                        newMapNames[counter] = Worldmaps.Worldmap[modifiedPixelData.Region].MapNames[counter];
                        newLocations[counter] = location;                        
                    }
                    else {
                        if (!newMapNameLookup.ContainsKey(modifiedPixelData.Name))
                            newMapNameLookup.Add(modifiedPixelData.Name, counter);
                        newMapNames[counter] = modifiedPixelData.Name;
                        newLocationAdded = true;
                        Debug.Log("New location added");
                        DFLocation createdLocation = new DFLocation();
                        createdLocation = GetDFLocationFromPixelData(modifiedPixelData);
                        newLocations[counter] = createdLocation;
                    }

                    if (newLocationAdded && (counter + 1) < mapTableList.Count)
                    {
                        if (!newMapNameLookup.ContainsKey(Worldmaps.Worldmap[modifiedPixelData.Region].MapNames[counter]))
                            newMapNameLookup.Add(Worldmaps.Worldmap[modifiedPixelData.Region].MapNames[counter], (counter + 1));
                        newMapNames[counter + 1] = Worldmaps.Worldmap[modifiedPixelData.Region].MapNames[counter];
                    }

                    Debug.Log("Region: " + modifiedPixelData.Region + ", location: " + counter);

                    if ((counter + 1) == mapTableList.Count)
                    {
                        break;
                    }

                    counter++;
                }

                Worldmaps.Worldmap[modifiedPixelData.Region].MapNames = new string[Worldmaps.Worldmap[modifiedPixelData.Region].LocationCount];
                Worldmaps.Worldmap[modifiedPixelData.Region].MapNames = newMapNames;

                Worldmaps.Worldmap[modifiedPixelData.Region].MapTable = mapTableList.ToArray();

                Worldmaps.Worldmap[modifiedPixelData.Region].MapIdLookup = new Dictionary<ulong, int>();
                Worldmaps.Worldmap[modifiedPixelData.Region].MapIdLookup = newMapIdLookup;

                Worldmaps.Worldmap[modifiedPixelData.Region].MapNameLookup = new Dictionary<string, int>();
                Worldmaps.Worldmap[modifiedPixelData.Region].MapNameLookup = newMapNameLookup;

                Worldmaps.Worldmap[modifiedPixelData.Region].Locations = newLocations;

                Worldmaps.mapDict = Worldmaps.EnumerateMaps();
            }

            SmallHeightmap.Woods[(int)pixelCoordinates.x, (int)pixelCoordinates.y] = (byte)modifiedPixelData.Elevation;
            PoliticInfo.Politic[(int)pixelCoordinates.x, (int)pixelCoordinates.y] = ConvertRegionIndexToPoliticIndex(modifiedPixelData.Region);
            ClimateInfo.Climate[(int)pixelCoordinates.x, (int)pixelCoordinates.y] = (modifiedPixelData.Climate + (int)MapsFile.Climates.Ocean);
        }

        protected void OpenRegionManager()
        {
            RegionManager regionManager = (RegionManager) EditorWindow.GetWindow(typeof(RegionManager), false, "Region Manager");
            regionManager.Show();
        }

        protected void OpenBlockInspector()
        {
            BlockInspector blockInspector = (BlockInspector) EditorWindow.GetWindow(typeof(BlockInspector), false, "Block Inspector");
            blockInspector.Show();
        }

        protected void SaveCurrentWorld()
        {
            string fileDataPath = Path.Combine(testPath, "Maps.json");
            var json = JsonConvert.SerializeObject(Worldmaps.Worldmap, new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore });
            File.WriteAllText(fileDataPath, json);

            fileDataPath = Path.Combine(testPath, "mapDict.json");
            json = JsonConvert.SerializeObject(Worldmaps.mapDict, new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore });
            File.WriteAllText(fileDataPath, json);

            fileDataPath = Path.Combine(testPath, "Climate.json");
            json = JsonConvert.SerializeObject(ClimateInfo.Climate, new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore });
            File.WriteAllText(fileDataPath, json);

            fileDataPath = Path.Combine(testPath, "Politic.json");
            json = JsonConvert.SerializeObject(PoliticInfo.Politic, new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore });
            File.WriteAllText(fileDataPath, json);

            fileDataPath = Path.Combine(testPath, "Woods.json");
            json = JsonConvert.SerializeObject(SmallHeightmap.Woods, new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore });
            File.WriteAllText(fileDataPath, json);            
        }

        protected void SetCurrentWorld()
        {
            // worldSavePath = EditorUtility.SaveFolderPanel("Select a path", "", "");

            // if (!Directory.Exists(Path.Combine(worldSavePath, worldName)))
            // {
            //     Directory.CreateDirectory(Path.Combine(path, worldName));
            //     string path2 = EditorGUIUtilityBridge.OpenFolderPanel("Select source files path", "", "");

            //     File.Copy(Path.Combine(path2, "Maps.json"), Path.Combine(worldSavePath, "Maps.json"));
            //     File.Copy(Path.Combine(path2, "Climate.json"), Path.Combine(worldSavePath, "Climate.json"));
            //     File.Copy(Path.Combine(path2, "Politic.json"), Path.Combine(worldSavePath, "Politic.json"));
            //     File.Copy(Path.Combine(path2, "Woods.json"), Path.Combine(worldSavePath, "Woods.json"));
            // }
        }

        protected string[] SetRMBBlocks()
        {
            string[] blockNames = new string[499];
            DFBlock block = new DFBlock();
            string rmbPath = Path.Combine(WorldMaps.mapPath, "RMB");

            for (int i = 0; i < blockNames.Length; i++)
            {
                block = blockFileReader.GetBlock(i);
            
                if (block.Name == null || block.Name == "")
                    break;

                if (block.Name.EndsWith("RMB"))
                    blockNames[i] = block.Name;
            }

            if (!Directory.Exists(rmbPath))
            {
                Debug.Log("invalid RMB directory: " + rmbPath);
            }
            else
            {
                var rmbFiles = Directory.GetFiles(rmbPath, "*" + "RMB", SearchOption.AllDirectories);
                // var rmbFileNames = new string[rmbFiles.Length];
                // var loadedRMBNames = GetAllRMBFileNames();

                for (int i = 0; i < rmbFiles.Length; i++)
                {
                    blockNames.Append<string>(rmbFiles[i]);
                }
            }

            List<string> blockNamesList = new List<string>();
            blockNamesList = blockNames.ToList();
            blockNamesList.Sort();
            blockNames = new string[blockNamesList.Count];
            blockNames = blockNamesList.ToArray();

            return blockNames;
        }

        protected void ResetSelectedCoordinates()
        {
            selectedX = selectedY = 1;
        }

        protected void SetRegionNames()
        {
            regionNames = new string[MapsFile.TempRegionCount];

            for (int i = 0; i < MapsFile.TempRegionCount; i++)
            {
                regionNames[i] = MapsFile.RegionNames[i];
            }
        }

        protected void SetClimateNames()
        {
            climateNames = new string[Enum.GetNames(typeof(MapsFile.Climates)).Length];

            for (int i = 0; i < Enum.GetNames(typeof(MapsFile.Climates)).Length; i++)
            {
                climateNames[i] = ((MapsFile.Climates)(i + (int)MapsFile.Climates.Ocean)).ToString();
            }
        }

        protected void SetHeightmap()
        {
            Color32[] heightmapBuffer = new Color32[MapsFile.WorldWidth * MapsFile.WorldHeight];
            heightMap = new Texture2D(MapsFile.WorldWidth, MapsFile.WorldHeight, TextureFormat.ARGB32, false);
            heightMap.filterMode = FilterMode.Point;
            heightmapBuffer = CreateHeightmap();
            heightMap.SetPixels32(heightmapBuffer);
            heightMap.Apply();
        }

        protected void SetLocationsMap()
        {
            Color32[] locationsMapBuffer = new Color32[MapsFile.WorldWidth * MapsFile.WorldHeight];
            locationsMap = new Texture2D(MapsFile.WorldWidth, MapsFile.WorldHeight, TextureFormat.ARGB32, false);
            locationsMap.filterMode = FilterMode.Point;
            locationsMapBuffer = CreateLocationsMap();
            locationsMap.SetPixels32(locationsMapBuffer);
            locationsMap.Apply();
        }

        protected void SetClimateMap()
        {
            Color32[] climateMapBuffer = new Color32[MapsFile.WorldWidth * MapsFile.WorldHeight];
            climateMap = new Texture2D(MapsFile.WorldWidth, MapsFile.WorldHeight, TextureFormat.ARGB32, false);
            climateMap.filterMode = FilterMode.Point;
            climateMapBuffer = CreateClimateMap();
            climateMap.SetPixels32(climateMapBuffer);
            climateMap.Apply();
        }

        protected void SetPoliticMap()
        {
            Color32[] politicMapBuffer = new Color32[MapsFile.WorldWidth * MapsFile.WorldHeight];
            politicMap = new Texture2D(MapsFile.WorldWidth, MapsFile.WorldHeight, TextureFormat.ARGB32, false);
            politicMap.filterMode = FilterMode.Point;
            politicMapBuffer = CreatePoliticMap();
            politicMap.SetPixels32(politicMapBuffer);
            politicMap.Apply();
        }

        protected void SetHeightmapRect()
        {
            heightmapRect = new Rect(heightmapOriginX, heightmapOriginY, 1, 1);
        }

        protected void SetMaps()
        {
            SetHeightmap();
            SetClimateMap();
            SetPoliticMap();
        }

        static Color32[] CreateHeightmap()
        {
            Color32[] colours = new Color32[MapsFile.WorldWidth * MapsFile.WorldHeight];

            for (int x = 0; x < MapsFile.WorldWidth; x++)
            {
                for (int y = 0; y < MapsFile.WorldHeight; y++)
                {
                    byte value = SmallHeightmap.GetHeightMapValue(x, y);
                    int terrain;
                    Color32 colour;

                    if (value < 3)
                        terrain = -1;

                    else
                        terrain = (value / 10);

                    switch (terrain)
                    {
                        case -1:
                            colour = new Color32(40, 71, 166, (byte)mapAlphaChannel);
                            break;

                        case 0:
                            colour = new Color32(175, 200, 168, (byte)mapAlphaChannel);
                            break;

                        case 1:
                            colour = new Color32(148, 176, 141, (byte)mapAlphaChannel);
                            break;

                        case 2:
                            colour = new Color32(123, 156, 118, (byte)mapAlphaChannel);
                            break;

                        case 3:
                            colour = new Color32(107, 144, 109, (byte)mapAlphaChannel);
                            break;

                        case 4:
                            colour = new Color32(93, 130, 94, (byte)mapAlphaChannel);
                            break;

                        case 5:
                            colour = new Color32(82, 116, 86, (byte)mapAlphaChannel);
                            break;

                        case 6:
                            colour = new Color32(77, 110, 78, (byte)mapAlphaChannel);
                            break;

                        case 7:
                            colour = new Color32(68, 99, 67, (byte)mapAlphaChannel);
                            break;

                        case 8:
                            colour = new Color32(61, 89, 53, (byte)mapAlphaChannel);
                            break;

                        case 9:
                            colour = new Color32(52, 77, 45, (byte)mapAlphaChannel);
                            break;

                        case 10:
                            colour = new Color32(34, 51, 34, (byte)mapAlphaChannel);
                            break;

                        default:
                            colour = new Color32(40, 47, 40, (byte)mapAlphaChannel);
                            break;
                    }
                    //}

                    colours[(MapsFile.WorldHeight - 1 - y) * MapsFile.WorldWidth + x] = colour;
                }
            }

            return colours;
        }

        static Color32[] CreateLocationsMap()
        {
            Color32[] colours = new Color32[MapsFile.WorldWidth * MapsFile.WorldHeight];

            for (int x = 0; x < MapsFile.WorldWidth; x++)
            {
                for (int y = 0; y < MapsFile.WorldHeight; y++)
                {
                    int offset = (((MapsFile.WorldHeight - y - 1) * MapsFile.WorldWidth) + x);
                    int sampleRegion = PoliticInfo.ConvertMapPixelToRegionIndex(x, y);

                    MapSummary summary;
                    if (Worldmaps.HasLocation(x, y, out summary))
                    {
                        Color32 colour = new Color32();
                        int index = (int)summary.LocationType;
                        if (index == -1)
                            continue;
                        else
                        {
                            switch (index)
                            {
                                case 0:
                                    colour = new Color32(220, 177, 177, 255);
                                    break;

                                case 1:
                                    colour = new Color32(188, 138, 138, 255);
                                    break;

                                case 2:
                                    colour = new Color32(155, 105, 106, 255);
                                    break;

                                case 3:
                                    colour = new Color32(165, 100, 70, 255);
                                    break;

                                case 4:
                                    colour = new Color32(215, 119, 39, 255);
                                    break;

                                case 5:
                                    colour = new Color32(176, 205, 255, 255);
                                    break;

                                case 6:
                                    colour = new Color32(126, 81, 89, 255);
                                    break;

                                case 7:
                                    colour = new Color32(191, 87, 27, 255);
                                    break;

                                case 8:
                                    colour = new Color32(193, 133, 100, 255);
                                    break;

                                case 9:
                                    colour = new Color32(68, 124, 192, 255);
                                    break;

                                case 10:
                                    colour = new Color32(171, 51, 15, 255);
                                    break;

                                case 11:
                                    colour = new Color32(140, 86, 55, 255);
                                    break;

                                case 12:
                                    colour = new Color32(147, 15, 7, 255);
                                    break;

                                case 13:
                                    colour = new Color32(15, 15, 15, 255);
                                    break;

                                default:
                                    colour = new Color32(40, 47, 40, 255);
                                    break;
                            }
                        }
                        colours[offset] = colour;
                    }
                    else colours[offset] = new Color32(0, 0, 0, 0);
                }
            }

            return colours;
        }

        static Color32[] CreateClimateMap()
        {
            Color32[] colours = new Color32[MapsFile.WorldWidth * MapsFile.WorldHeight];
            Color32 colour = new Color32();

            for (int x = 0; x < MapsFile.WorldWidth; x++)
            {
                for (int y = 0; y < MapsFile.WorldHeight; y++)
                {
                    int offset = (((MapsFile.WorldHeight - y - 1) * MapsFile.WorldWidth) + x);

                    int value = ClimateInfo.Climate[x, y];

                    switch (value)
                    {
                        case 223:   // Ocean 
                            colour = new Color32(0, 0, 0, 0);
                            break;

                        case 224:   // Desert
                            colour = new Color32(217, 217, 217, (byte)mapAlphaChannel);
                            break;

                        case 225:   // Desert2
                            colour = new Color32(255, 255, 255, (byte)mapAlphaChannel);
                            break;

                        case 226:   // Mountains
                            colour = new Color32(230, 196, 230, (byte)mapAlphaChannel);
                            break;

                        case 227:   // RainForest
                            colour = new Color32(0, 152, 25, (byte)mapAlphaChannel);
                            break;

                        case 228:   // Swamp
                            colour = new Color32(115, 153, 141, (byte)mapAlphaChannel);
                            break;

                        case 229:   // Sub tropical
                            colour = new Color32(180, 180, 179, (byte)mapAlphaChannel);
                            break;

                        case 230:   // Woodland hills (aka Mountain Woods)
                            colour = new Color32(191, 143, 191, (byte)mapAlphaChannel);
                            break;

                        case 231:   // TemperateWoodland (aka Woodlands)
                            colour = new Color32(0, 190, 0, (byte)mapAlphaChannel);
                            break;

                        case 232:   // Haunted woodland
                            colour = new Color32(190, 166, 143, (byte)mapAlphaChannel);
                            break;

                        default:
                            colour = new Color32(0, 0, 0, 0);
                            break;
                    }
                    colours[offset] = colour;
                }                
            }
            return colours;
        }

        static Color32[] CreatePoliticMap()
        {
            Color32[] colours = new Color32[MapsFile.WorldWidth * MapsFile.WorldHeight];
            Color32 colour = new Color32();

            for (int x = 0; x < MapsFile.WorldWidth; x++)
            {
                for (int y = 0; y < MapsFile.WorldHeight; y++)
                {
                    int offset = (((MapsFile.WorldHeight - y - 1) * MapsFile.WorldWidth) + x);

                    int value = PoliticInfo.ConvertMapPixelToRegionIndex(x, y);

                    switch (value)
                    {
                        case 64:    // Sea
                            colour = new Color32(0, 0, 0, 0);
                            break;

                        case 0:     // The Alik'r Desert
                            colour = new Color32(55, 170, 253, (byte)mapAlphaChannel);
                            break;

                        case 1:     // The Dragontail Mountains
                            colour = new Color32(149, 43, 29, (byte)mapAlphaChannel);
                            break;

                        case 2:     // Glenpoint Foothills - unused
                            colour = new Color32(123, 156, 118, (byte)mapAlphaChannel);
                            break;

                        case 3:     // Daggerfall Bluffs - unused
                            colour = new Color32(107, 144, 109, (byte)mapAlphaChannel);
                            break;

                        case 4:     // Yeorth Burrowland - unused
                            colour = new Color32(93, 130, 94, (byte)mapAlphaChannel);
                            break;

                        case 5:     // Dwynnen
                            colour = new Color32(212, 180, 105, (byte)mapAlphaChannel);
                            break;

                        case 6:     // Ravennian Forest - unused
                            colour = new Color32(77, 110, 78, (byte)mapAlphaChannel);
                            break;

                        case 7:     // Devilrock - unused
                            colour = new Color32(68, 99, 67, (byte)mapAlphaChannel);
                            break;

                        case 8:     // Malekna Forest - unused
                            colour = new Color32(61, 89, 53, (byte)mapAlphaChannel);
                            break;

                        case 9:     // The Isle of Balfiera
                            colour = new Color32(158, 0, 0, (byte)mapAlphaChannel);
                            break;

                        case 10:    // Bantha - unused
                            colour = new Color32(34, 51, 34, (byte)mapAlphaChannel);
                            break;

                        case 11:    // Dak'fron
                            colour = new Color32(36, 116, 84, (byte)mapAlphaChannel);
                            break;

                        case 12:    // The Islands in the Western Iliac Bay - unused
                            colour = new Color32(36, 116, 84, (byte)mapAlphaChannel);
                            break;

                        case 13:    // Tamarilyn Point - unused
                            colour = new Color32(36, 116, 84, (byte)mapAlphaChannel);
                            break;

                        case 14:    // Lainlyn Cliffs - unused
                            colour = new Color32(36, 116, 84, (byte)mapAlphaChannel);
                            break;

                        case 15:    // Bjoulsae River - unused
                            colour = new Color32(36, 116, 84, (byte)mapAlphaChannel);
                            break;

                        case 16:    // The Wrothgarian Mountains
                            colour = new Color32(250, 201, 11, (byte)mapAlphaChannel);
                            break;

                        case 17:    // Daggerfall
                            colour = new Color32(0, 126, 13, (byte)mapAlphaChannel);
                            break;

                        case 18:    // Glenpoint
                            colour = new Color32(152, 152, 152, (byte)mapAlphaChannel);
                            break;

                        case 19:    // Betony
                            colour = new Color32(31, 55, 132, (byte)mapAlphaChannel);
                            break;

                        case 20:    // Sentinel
                            colour = new Color32(158, 134, 17, (byte)mapAlphaChannel);
                            break;

                        case 21:    // Anticlere
                            colour = new Color32(30, 30, 30, (byte)mapAlphaChannel);
                            break;

                        case 22:    // Lainlyn
                            colour = new Color32(38, 127, 0, (byte)mapAlphaChannel);
                            break;

                        case 23:    // Wayrest
                            colour = new Color32(0, 248, 255, (byte)mapAlphaChannel);
                            break;

                        case 24:    // Gen Tem High Rock village - unused
                            colour = new Color32(158, 134, 17, (byte)mapAlphaChannel);
                            break;

                        case 25:    // Gen Rai Hammerfell village - unused
                            colour = new Color32(158, 134, 17, (byte)mapAlphaChannel);
                            break;

                        case 26:    // The Orsinium Area
                            colour = new Color32(0, 99, 46, (byte)mapAlphaChannel);
                            break;

                        case 27:    // Skeffington Wood - unused
                            colour = new Color32(0, 99, 46, (byte)mapAlphaChannel);
                            break;

                        case 28:    // Hammerfell bay coast - unused
                            colour = new Color32(0, 99, 46, (byte)mapAlphaChannel);
                            break;

                        case 29:    // Hammerfell sea coast - unused
                            colour = new Color32(0, 99, 46, (byte)mapAlphaChannel);
                            break;

                        case 30:    // High Rock bay coast - unused
                            colour = new Color32(0, 99, 46, (byte)mapAlphaChannel);
                            break;

                        case 31:    // High Rock sea coast
                            colour = new Color32(0, 0, 0, 0);
                            break;

                        case 32:    // Northmoor
                            colour = new Color32(127, 127, 127, (byte)mapAlphaChannel);
                            break;

                        case 33:    // Menevia
                            colour = new Color32(229, 115, 39, (byte)mapAlphaChannel);
                            break;

                        case 34:    // Alcaire
                            colour = new Color32(238, 90, 0, (byte)mapAlphaChannel);
                            break;

                        case 35:    // Koegria
                            colour = new Color32(0, 83, 165, (byte)mapAlphaChannel);
                            break;

                        case 36:    // Bhoriane
                            colour = new Color32(255, 124, 237, (byte)mapAlphaChannel);
                            break;

                        case 37:    // Kambria
                            colour = new Color32(0, 19, 127, (byte)mapAlphaChannel);
                            break;

                        case 38:    // Phrygias
                            colour = new Color32(81, 46, 26, (byte)mapAlphaChannel);
                            break;

                        case 39:    // Urvaius
                            colour = new Color32(12, 12, 12, (byte)mapAlphaChannel);
                            break;

                        case 40:    // Ykalon
                            colour = new Color32(87, 0, 127, (byte)mapAlphaChannel);
                            break;

                        case 41:    // Daenia
                            colour = new Color32(32, 142, 142, (byte)mapAlphaChannel);
                            break;

                        case 42:    // Shalgora
                            colour = new Color32(202, 0, 0, (byte)mapAlphaChannel);
                            break;

                        case 43:    // Abibon-Gora
                            colour = new Color32(142, 74, 173, (byte)mapAlphaChannel);
                            break;

                        case 44:    // Kairou
                            colour = new Color32(68, 27, 0, (byte)mapAlphaChannel);
                            break;

                        case 45:    // Pothago
                            colour = new Color32(207, 20, 43, (byte)mapAlphaChannel);
                            break;

                        case 46:    // Myrkwasa
                            colour = new Color32(119, 108, 59, (byte)mapAlphaChannel);
                            break;

                        case 47:    // Ayasofya
                            colour = new Color32(74, 35, 1, (byte)mapAlphaChannel);
                            break;

                        case 48:    // Tigonus
                            colour = new Color32(255, 127, 127, (byte)mapAlphaChannel);
                            break;

                        case 49:    // Kozanset
                            colour = new Color32(127, 127, 127, (byte)mapAlphaChannel);
                            break;

                        case 50:    // Satakalaam
                            colour = new Color32(255, 46, 0, (byte)mapAlphaChannel);
                            break;

                        case 51:    // Totambu
                            colour = new Color32(193, 77, 0, (byte)mapAlphaChannel);
                            break;

                        case 52:    // Mournoth
                            colour = new Color32(153, 28, 0, (byte)mapAlphaChannel);
                            break;

                        case 53:    // Ephesus
                            colour = new Color32(253, 103, 0, (byte)mapAlphaChannel);
                            break;

                        case 54:    // Santaki
                            colour = new Color32(1, 255, 144, (byte)mapAlphaChannel);
                            break;

                        case 55:    // Antiphyllos
                            colour = new Color32(229, 182, 64, (byte)mapAlphaChannel);
                            break;

                        case 56:    // Bergama
                            colour = new Color32(196, 169, 37, (byte)mapAlphaChannel);
                            break;

                        case 57:    // Gavaudon
                            colour = new Color32(240, 8, 47, (byte)mapAlphaChannel);
                            break;

                        case 58:    // Tulune
                            colour = new Color32(0, 73, 126, (byte)mapAlphaChannel);
                            break;

                        case 59:    // Glenumbra Moors
                            colour = new Color32(15, 0, 61, (byte)mapAlphaChannel);
                            break;

                        case 60:    // Ilessan Hills
                            colour = new Color32(236, 42, 50, (byte)mapAlphaChannel);
                            break;

                        case 61:    // Cybiades
                            colour = new Color32(255, 255, 255, (byte)mapAlphaChannel);
                            break;

                        case -1:
                        default:
                            colour = new Color32(0, 0, 0, 0);
                            break;
                    }
                    colours[offset] = colour;
                }
            }
            return colours;
        }

        public static int ConvertRegionIndexToPoliticIndex(int regionIndex)
        {
            if (regionIndex == 64)
                return regionIndex;

            regionIndex += 128;
            return regionIndex;
        }

        public static DFLocation GetDFLocationFromPixelData(PixelData sourcePixel)
        {
            DFLocation createdLocation = new DFLocation();
            createdLocation.Loaded = false;
            createdLocation.Name = sourcePixel.Name;
            createdLocation.RegionName = sourcePixel.RegionName;

            if (sourcePixel.DungeonType == 255)
                createdLocation.HasDungeon = false;
            else createdLocation.HasDungeon = true;

            createdLocation.MapTableData.MapId = sourcePixel.MapId;
            createdLocation.MapTableData.Latitude = sourcePixel.Latitude;
            createdLocation.MapTableData.Longitude = sourcePixel.Longitude;
            createdLocation.MapTableData.LocationType = (DFRegion.LocationTypes)sourcePixel.LocationType;
            createdLocation.MapTableData.DungeonType = (DFRegion.DungeonTypes)sourcePixel.DungeonType;
            createdLocation.MapTableData.Discovered = false;
            createdLocation.MapTableData.Key = 0;
            createdLocation.Exterior.RecordElement.Header.X = sourcePixel.exterior.X;
            createdLocation.Exterior.RecordElement.Header.Y = sourcePixel.exterior.Y;
            createdLocation.Exterior.RecordElement.Header.IsExterior = 32768; // TODO: must check what this does
            createdLocation.Exterior.RecordElement.Header.Unknown2 = 0; // TODO: must check what this does
            createdLocation.Exterior.RecordElement.Header.LocationId = (ushort)GenerateNewLocationId();
            createdLocation.Exterior.RecordElement.Header.IsInterior = 0; // TODO: must check what this does
            createdLocation.Exterior.RecordElement.Header.ExteriorLocationId = 0;
            createdLocation.Exterior.RecordElement.Header.LocationName = sourcePixel.Name;
            createdLocation.Exterior.BuildingCount = GetBuildingCount(sourcePixel);
            createdLocation.Exterior.Buildings = new DFLocation.BuildingData[createdLocation.Exterior.BuildingCount];
            // createdLocation.Exterior.Buildings[x].NameSeed...
            createdLocation.Exterior.ExteriorData.AnotherName = sourcePixel.Name;
            createdLocation.Exterior.ExteriorData.MapId = sourcePixel.MapId;
            createdLocation.Exterior.ExteriorData.LocationId = 0;
            createdLocation.Exterior.ExteriorData.Width = (byte)sourcePixel.exterior.Width;
            createdLocation.Exterior.ExteriorData.Height = (byte)sourcePixel.exterior.Height;

            if (sourcePixel.exterior.PortTown)
                createdLocation.Exterior.ExteriorData.PortTownAndUnknown = 1;
            else createdLocation.Exterior.ExteriorData.PortTownAndUnknown = 0;
            createdLocation.Exterior.ExteriorData.BlockNames = sourcePixel.exterior.BlockNames;

            if (sourcePixel.DungeonType != 255)
            {
                createdLocation.Dungeon.RecordElement.Header.X = sourcePixel.dungeon.X;
                createdLocation.Dungeon.RecordElement.Header.Y = sourcePixel.dungeon.Y;
                createdLocation.Dungeon.RecordElement.Header.IsExterior = 0;
                createdLocation.Dungeon.RecordElement.Header.Unknown2 = 0;
                createdLocation.Dungeon.RecordElement.Header.LocationId = (ushort)(createdLocation.Exterior.RecordElement.Header.LocationId + 1);
                createdLocation.Dungeon.RecordElement.Header.IsInterior = 1;
                createdLocation.Dungeon.RecordElement.Header.ExteriorLocationId = createdLocation.Exterior.RecordElement.Header.LocationId;
                createdLocation.Dungeon.RecordElement.Header.LocationName = sourcePixel.Name;
                createdLocation.Dungeon.Header.BlockCount = (ushort)sourcePixel.dungeon.BlockCount;

                for (int i = 0; i < createdLocation.Dungeon.Header.BlockCount; i++)
                {
                    createdLocation.Dungeon.Blocks[i].X = (sbyte)sourcePixel.dungeon.blocks[i].X;
                    createdLocation.Dungeon.Blocks[i].Z = (sbyte)sourcePixel.dungeon.blocks[i].Z;
                    createdLocation.Dungeon.Blocks[i].IsStartingBlock = sourcePixel.dungeon.blocks[i].IsStartingBlock;
                    createdLocation.Dungeon.Blocks[i].BlockName = sourcePixel.dungeon.blocks[i].BlockName;
                    createdLocation.Dungeon.Blocks[i].WaterLevel = (short)sourcePixel.dungeon.blocks[i].WaterLevel;
                    createdLocation.Dungeon.Blocks[i].CastleBlock = sourcePixel.dungeon.blocks[i].CastleBlock;
                }
            }
            else
            {
                createdLocation.Dungeon.RecordElement.Header.X = 0;
                createdLocation.Dungeon.RecordElement.Header.Y = 0;
                createdLocation.Dungeon.RecordElement.Header.IsExterior = 0;
                createdLocation.Dungeon.RecordElement.Header.Unknown2 = 0;
                createdLocation.Dungeon.RecordElement.Header.LocationId = 0;
                createdLocation.Dungeon.RecordElement.Header.IsInterior = 0;
                createdLocation.Dungeon.RecordElement.Header.ExteriorLocationId = 0;
                createdLocation.Dungeon.RecordElement.Header.LocationName = null;
                createdLocation.Dungeon.Header.BlockCount = 0;
                createdLocation.Dungeon.Blocks = new DFLocation.DungeonBlock[0];
            }

            return createdLocation;
        }

        public static ulong GenerateNewLocationId()
        {
            bool found = false;
            ulong counter = 0;
            ushort extValue;

            do{
                if (!Worldmaps.locationIdList.Contains(counter) && !Worldmaps.locationIdList.Contains(counter + 1))
                {
                    found = true;
                    return counter;
                }

                counter += 2;
            }
            while (!found);

            return 0;
        }

        public static ushort GetBuildingCount(PixelData pixel)
        {
            DFBlock block;
            int locDim = pixel.exterior.Width * pixel.exterior.Height;
            int buildCount = 0;
            for (int i = 0; i < locDim; i++)
            {
                block = new DFBlock();
                block = blockFileReader.GetBlock(pixel.exterior.BlockNames[i]);
                buildCount += block.RmbBlock.FldHeader.BuildingDataList.Length;
            }
            return buildCount;
        }

        static GUIStyle SetGUIStyle()
        {
            GUIStyle style = new GUIStyle();

            style.fixedHeight = 1200.0f;
            style.fixedWidth = 3440.0f;

            // style.stretchHeight = false;
            // style.stretchWidth = false;

            return style;
        }

        static Rect UpdateLayerPosition()
        {
            Rect position = new Rect(layerPosition.x, layerPosition.y, mapView.width, mapView.height);
            Debug.Log("Updating layer Position");
            return position;
        }

        static float SetMapAlphaChannel()
        {
            short numberOfChecks = CountCheckNumber();

            switch (numberOfChecks)
            {
                case 1:
                    return level1;

                case 2:
                    return level2;

                case 3:
                    return level3;

                case 4:
                    return level4;

                default:
                    return 0.0f;
            }
        }

        static short CountCheckNumber()
        {
            short numberOfChecks = 0;

            if (heightmap)
                numberOfChecks++;

            if (climate)
                numberOfChecks++;

            if (politics)
                numberOfChecks++;

            if (mapImage)
                numberOfChecks++;

            if (numberOfChecks < 0 || numberOfChecks > 4)
            {
                Debug.LogError("Invalid check count!");
                return 0;
            }
            else return numberOfChecks;
        }

        static Rect DirectionButton(string direction, Rect position, bool refMap = false)
        {
            int multiplier;
            Event key = Event.current;
            if (Input.GetKeyDown(KeyCode.LeftControl))
                multiplier = 10;
            else multiplier = 1;

            switch (direction)
            {
                case buttonUp:
                    if (refMap)
                        position.y += (position.height * (setMovement * multiplier * zoomLevel));
                    else position.y += setMovement * multiplier;
                    break;

                case buttonDown:
                    if (refMap)
                        position.y -= (position.height * (setMovement * multiplier * zoomLevel));
                    else position.y -= setMovement * multiplier;
                    break;

                case buttonLeft:
                    if (refMap)
                        position.x -= (position.width * (setMovement * multiplier * zoomLevel));
                    else position.x -= setMovement * multiplier;
                    break;

                case buttonRight:
                    if (refMap)
                        position.x += (position.width * (setMovement * multiplier * zoomLevel));
                    else position.x += setMovement * multiplier;
                    break;

                case buttonReset:
                    position = new Rect(0, 0, 1, 1);
                    break;

                case buttonZoomOut:
                    position = new Rect(position.x, position.y, position.width * 2, position.height * 2);
                    break;

                case buttonZoomIn:
                    position = new Rect(position.x, position.y, position.width / 2, position.height / 2);
                    break;

                default:
                    break;    
            }
            return position;
        }
    }    

    /// <summary>
    /// File containing regions and locations data
    /// </summary>
    public class Worldmap
    {
        #region Class Variables

        public string Name;
        public int LocationCount;
        public string[] MapNames;
        public DFRegion.RegionMapTable[] MapTable;
        public Dictionary<ulong, int> MapIdLookup;
        public Dictionary<string, int> MapNameLookup;
        public DFLocation[] Locations;

        #endregion
    }

    public static class Worldmaps
    {
        #region Class Fields

        public static Worldmap[] Worldmap;
        public static Dictionary<ulong, MapSummary> mapDict;
        public static List<ulong> locationIdList;

        static Worldmaps()
        {
            Worldmap = JsonConvert.DeserializeObject<Worldmap[]>(File.ReadAllText(Path.Combine(MapEditor.testPath, "Maps.json")));
            mapDict = EnumerateMaps();
        }

        #endregion

        #region Variables

        public static Dictionary<ulong, ulong> locationIdToMapIdDict;

        #endregion

        #region Public Methods

        public static ulong ReadLocationIdFast(int region, int location)
        {
            // Added new locations will put the LocationId in regions map table, since it doesn't exist in classic data
            if (Worldmaps.Worldmap[region].MapTable[location].LocationId != 0)
                return Worldmaps.Worldmap[region].MapTable[location].LocationId;

            // Get datafile location count (excluding added locations)
            int locationCount = Worldmaps.Worldmap[region].LocationCount;

            // Read the LocationId
            ulong locationId = Worldmaps.Worldmap[region].Locations[location].Exterior.RecordElement.Header.LocationId;

            return locationId;
        }

        public static DFRegion ConvertWorldMapsToDFRegion(int currentRegionIndex)
        {
            DFRegion dfRegion = new DFRegion();

            dfRegion.Name = Worldmaps.Worldmap[currentRegionIndex].Name;
            dfRegion.LocationCount = Worldmaps.Worldmap[currentRegionIndex].LocationCount;
            dfRegion.MapNames = Worldmaps.Worldmap[currentRegionIndex].MapNames;
            dfRegion.MapTable = Worldmaps.Worldmap[currentRegionIndex].MapTable;
            dfRegion.MapIdLookup = Worldmaps.Worldmap[currentRegionIndex].MapIdLookup;
            dfRegion.MapNameLookup = Worldmaps.Worldmap[currentRegionIndex].MapNameLookup;

            return dfRegion;
        }

        /// <summary>
        /// Gets a DFLocation representation of a location.
        /// </summary>
        /// <param name="region">Index of region.</param>
        /// <param name="location">Index of location.</param>
        /// <returns>DFLocation.</returns>
        public static DFLocation GetLocation(int region, int location)
        {
            // Read location
            DFLocation dfLocation = new DFLocation();
            dfLocation = Worldmaps.Worldmap[region].Locations[location];

            // Store indices
            dfLocation.RegionIndex = region;
            dfLocation.LocationIndex = location;

            // Generate smaller dungeon when possible
            // if (UseSmallerDungeon(dfLocation))
            //     GenerateSmallerDungeon(ref dfLocation);

            return dfLocation;
        }

        /// <summary>
        /// Attempts to get a Daggerfall location from MAPS.BSA.
        /// </summary>
        /// <param name="regionIndex">Index of region.</param>
        /// <param name="locationIndex">Index of location.</param>
        /// <param name="locationOut">DFLocation data out.</param>
        /// <returns>True if successful.</returns>
        public static bool GetLocation(int regionIndex, int locationIndex, out DFLocation locationOut)
        {
            locationOut = new DFLocation();

            // Get location data
            locationOut = Worldmaps.GetLocation(regionIndex, locationIndex);
            if (!locationOut.Loaded)
            {
                DaggerfallUnity.LogMessage(string.Format("Unknown location RegionIndex='{0}', LocationIndex='{1}'.", regionIndex, locationIndex), true);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Gets DFLocation representation of a location.
        /// </summary>
        /// <param name="regionName">Name of region.</param>
        /// <param name="locationName">Name of location.</param>
        /// <returns>DFLocation.</returns>
        public static DFLocation GetLocation(string regionName, string locationName)
        {
            // Load region
            int Region = GetRegionIndex(regionName);

            // Check location exists
            if (!Worldmaps.Worldmap[Region].MapNameLookup.ContainsKey(locationName))
                return new DFLocation();

            // Get location index
            int Location = Worldmaps.Worldmap[Region].MapNameLookup[locationName];

            return GetLocation(Region, Location);
        }

        /// <summary>
        /// Attempts to get a Daggerfall location from MAPS.BSA.
        /// </summary>
        /// <param name="regionName">Name of region.</param>
        /// <param name="locationName">Name of location.</param>
        /// <param name="locationOut">DFLocation data out.</param>
        /// <returns>True if successful.</returns>
        public static bool GetLocation(string regionName, string locationName, out DFLocation locationOut)
        {
            locationOut = new DFLocation();

            // Get location data
            locationOut = GetLocation(regionName, locationName);
            if (!locationOut.Loaded)
            {
                DaggerfallUnity.LogMessage(string.Format("Unknown location RegionName='{0}', LocationName='{1}'.", regionName, locationName), true);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Gets index of region with specified name. Does not change the currently loaded region.
        /// </summary>
        /// <param name="name">Name of region.</param>
        /// <returns>Index of found region, or -1 if not found.</returns>
        public static int GetRegionIndex(string name)
        {
            // Search for region name
            for (int i = 0; i < MapsFile.TempRegionCount; i++)
            {
                if (Worldmaps.Worldmap[i].Name == name)
                    return i;
            }

            return -1;
        }

        /// <summary>
        /// Determines if the current WorldCoord has a location.
        /// </summary>
        /// <param name="mapPixelX">Map pixel X.</param>
        /// <param name="mapPixelY">Map pixel Y.</param>
        /// <returns>True if there is a location at this map pixel.</returns>
        public static bool HasLocation(int mapPixelX, int mapPixelY, out MapSummary summaryOut)
        {
            // MapDictCheck();

            ulong id = MapsFile.GetMapPixelID(mapPixelX, mapPixelY);
            if (mapDict.ContainsKey(id))
            {
                summaryOut = mapDict[id];
                return true;
            }

            summaryOut = new MapSummary();
            return false;
        }

        /// <summary>
        /// Determines if the current WorldCoord has a location.
        /// </summary>
        /// <param name="mapPixelX">Map pixel X.</param>
        /// <param name="mapPixelY">Map pixel Y.</param>
        /// <returns>True if there is a location at this map pixel.</returns>
        public static bool HasLocation(int mapPixelX, int mapPixelY)
        {
            // MapDictCheck();

            ulong id = MapsFile.GetMapPixelID(mapPixelX, mapPixelY);
            if (mapDict.ContainsKey(id))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Attempts to get a Daggerfall location from MAPS.BSA using a locationId from quest system.
        /// The locationId is different to the mapId, which is derived from location coordinates in world.
        /// At this time, best known way to determine locationId is from LocationRecordElementHeader data.
        /// This is linked to mapId at in EnumerateMaps().
        /// Note: Not all locations have a locationId, only certain key locations
        /// </summary>
        /// <param name="locationId">LocationId of map from quest system.</param>
        /// <param name="locationOut">DFLocation data out.</param>
        /// <returns>True if successful.</returns>
        public static bool GetQuestLocation(ulong locationId, out DFLocation locationOut)
        {
            locationOut = new DFLocation();

            // MapDictCheck();

            // Get mapId from locationId
            ulong mapId = LocationIdToMapId(locationId);
            if (Worldmaps.mapDict.ContainsKey(mapId))
            {
                MapSummary summary = Worldmaps.mapDict[mapId];
                return GetLocation(summary.RegionIndex, summary.MapIndex, out locationOut);
            }

            return false;
        }

        /// <summary>
        /// Converts LocationId from quest system to a MapId for map lookups.
        /// </summary>
        /// <param name="locationId">LocationId from quest system.</param>
        /// <returns>MapId if present or -1.</returns>
        public static ulong LocationIdToMapId(ulong locationId)
        {
            if (locationIdToMapIdDict.ContainsKey(locationId))
            {
                return locationIdToMapIdDict[locationId];
            }

            return 0;
        }

        // private static void MapDictCheck()
        // {
        //     // Build map lookup dictionary
        //     if (mapDict == null)
        //         EnumerateMaps();
        // }

        /// <summary>
        /// Build dictionary of locations.
        /// </summary>
        public static Dictionary<ulong, MapSummary> EnumerateMaps()
        {
            //System.Diagnostics.Stopwatch s = System.Diagnostics.Stopwatch.StartNew();
            //long startTime = s.ElapsedMilliseconds;

            Dictionary<ulong, MapSummary> mapDictSwap = new Dictionary<ulong, MapSummary>();
            // locationIdToMapIdDict = new Dictionary<ulong, ulong>();
            locationIdList = new List<ulong>();

            for (int region = 0; region < MapsFile.TempRegionCount; region++)
            {
                DFRegion dfRegion = Worldmaps.ConvertWorldMapsToDFRegion(region);
                for (int location = 0; location < dfRegion.LocationCount; location++)
                {
                    MapSummary summary = new MapSummary();
                    // Get map summary
                    DFRegion.RegionMapTable mapTable = dfRegion.MapTable[location];
                    summary.ID = mapTable.MapId;
                    summary.MapID = Worldmaps.Worldmap[region].Locations[location].Exterior.RecordElement.Header.LocationId;
                    locationIdList.Add(summary.MapID);
                    summary.RegionIndex = region;
                    summary.MapIndex = Worldmaps.Worldmap[region].Locations[location].LocationIndex;

                    summary.LocationType = mapTable.LocationType;
                    summary.DungeonType = mapTable.DungeonType;

                    // TODO: This by itself doesn't account for DFRegion.LocationTypes.GraveyardForgotten locations that start the game discovered in classic
                    summary.Discovered = mapTable.Discovered;

                    mapDictSwap.Add(summary.ID, summary);

                    // Link locationId with mapId - adds ~25ms overhead
                    // ulong locationId = WorldMaps.ReadLocationIdFast(region, location);
                    // locationIdToMapIdDict.Add(locationId, summary.ID);
                }
            }

            locationIdList.Sort();

            string fileDataPath = Path.Combine(MapEditor.testPath, "mapDict.json");
            var json = JsonConvert.SerializeObject(mapDictSwap, new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore });
            File.WriteAllText(fileDataPath, json);

            return mapDictSwap;
        }

        /// <summary>
        /// Lookup block name for exterior block from location data provided.
        /// </summary>
        /// <param name="dfLocation">DFLocation to read block name.</param>
        /// <param name="x">Block X coordinate.</param>
        /// <param name="y">Block Y coordinate.</param>
        /// <returns>Block name.</returns>
        public static string GetRmbBlockName(in DFLocation dfLocation, int x, int y)
        {
            int index = y * dfLocation.Exterior.ExteriorData.Width + x;
            return dfLocation.Exterior.ExteriorData.BlockNames[index];
        }

        #endregion
    }

    public struct MapSummary
    {
        public ulong ID;                  // mapTable.MapId & 0x000fffff for dict key and matching with ExteriorData.MapId
        public ulong MapID;               // Full mapTable.MapId for matching with localization key
        public int RegionIndex;
        public int MapIndex;
        public DFRegion.LocationTypes LocationType;
        public DFRegion.DungeonTypes DungeonType;
        public bool Discovered;
    }

    public class ClimateInfo
    {
        #region Class Fields

        public static int[,] Climate;

        static ClimateInfo()
        {
            Climate = JsonConvert.DeserializeObject<int[,]>(File.ReadAllText(Path.Combine(MapEditor.testPath, "Climate.json")));
        }

        public static int[,] ClimateModified;

        #endregion
    }

    public static class PoliticInfo
    {
        #region Class Fields

        public static int[,] Politic;

        static PoliticInfo()
        {
            Politic = JsonConvert.DeserializeObject<int[,]>(File.ReadAllText(Path.Combine(MapEditor.testPath, "Politic.json")));
        }

        #endregion

        #region Public Methods

        public static bool IsBorderPixel(int x, int y, int actualPolitic)
        {
            int politicIndex = Politic[x, y];

            for (int i = -1; i < 2; i++)
            {
                for (int j = -1; j < 2; j++)
                {
                    int X = x + i;
                    int Y = y + j;

                    if ((X < 0 || X > MapsFile.WorldWidth || Y < 0 || Y > MapsFile.WorldHeight) ||
                        (i == 0 && j == 0))
                        continue;

                    if (politicIndex != Politic[X, Y] &&
                        politicIndex != actualPolitic &&
                        (SmallHeightmap.GetHeightMapValue(x, y) > 3) &&
                        (SmallHeightmap.GetHeightMapValue(X, Y) > 3))
                        return true;
                }
            }

            return false;
        }

        public static int ConvertMapPixelToRegionIndex(int x, int y)
        {
            int regionIndex = Politic[x, y];

            if (regionIndex == 64)
                return regionIndex;

            regionIndex -= 128;
            return regionIndex;
        }

        #endregion
    }

    public static class SmallHeightmap
    {
        #region Class Fields

        public static byte[,] Woods;

        static SmallHeightmap()
        {
            Woods = JsonConvert.DeserializeObject<byte[,]>(File.ReadAllText(Path.Combine(MapEditor.testPath, "Woods.json")));
        }

        #endregion

        #region Public Methods

        public static Byte[] GetHeightMapValuesRange1Dim(int mapPixelX, int mapPixelY, int dim)
        {
            Byte[] dstData = new Byte[dim * dim];
            for (int y = 0; y < dim; y++)
            {
                for (int x = 0; x < dim; x++)
                {
                    dstData[x + (y * dim)] = GetHeightMapValue(mapPixelX + x, mapPixelY + y);
                }
            }
            return dstData;
        }

        public static Byte GetHeightMapValue(int mapPixelX, int mapPixelY)
        {
            // Clamp X
            if (mapPixelX < 0) mapPixelX = 0;
            if (mapPixelX >= MapsFile.WorldWidth) mapPixelX = MapsFile.WorldWidth - 1;

            // Clamp Y
            if (mapPixelY < 0) mapPixelY = 0;
            if (mapPixelY >= MapsFile.WorldHeight) mapPixelY = MapsFile.WorldHeight - 1;

            return Woods[mapPixelX, mapPixelY];
        }

        #endregion
    }
}