using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.Xml.Linq;
using System.IO;
using System;
using MonoGame.Extended;
using MonoGame.Extended.Collisions;

namespace BazaarBounty
{
    // Map Layer definition used to store a single XML layer.
    public class MapLayer
    {
        public static float currentDepth = 0;

        public string Name;

        // Store 2D array of integers referring to tile IDs for each layer.
        public int[,] Tiles;
        public float Depth;
        public bool IsVisible;

        public MapLayer(string name, int width, int height, bool isVisible)
        {
            Name = name;
            Tiles = new int[width, height];
            Depth = currentDepth;
            currentDepth += 0.01f;
            IsVisible = isVisible;
            // Console.WriteLine("Current Depth is " + currentDepth);
        }

        // Static method to reset the depth counter
        public static void ResetDepth()
        {
            currentDepth = 0;
        }
    }

    // Store the map information indicated by the "Placeholder" assets. Ex: Wall locations as Rectangles.
    public class TiledMapInformation
    {
        private Dictionary<string, List<Rectangle>> tilesByType;

        public TiledMapInformation()
        {
            tilesByType = new Dictionary<string, List<Rectangle>>();
        }

        // Add tile to TiledMapInformation
        public void AddTile(string type, Rectangle rectangle)
        {
            if (!tilesByType.ContainsKey(type))
            {
                tilesByType[type] = new List<Rectangle>();
            }

            tilesByType[type].Add(rectangle);
        }

        // Get tile rectangles of a certain type defined by tileTypeMap dictionary.
        public List<Rectangle> GetTilesOfType(string type)
        {
            if (tilesByType.TryGetValue(type, out var list))
            {
                return list;
            }

            return new List<Rectangle>(); // Return an empty list if no tiles found
        }
    }

    // The actual map class.
    public class Map
    {
        string name;
        TilesetManager tilesetManager;
        List<TilesetDescriptor> tilesetDescriptors;
        Dictionary<string, MapLayer> mapLayers;
        public List<CollidableTiles> collidableTiles;
        TilePositionCache positionCache;
        TiledMapInformation mapInfo;

        // int tileWidth;      // num of tiles wide in the map
        // int tileHeight;     // num of tiles tall in the map
        int mapRows; // num of rows of tiles in the map
        int mapCols; // num of columns of tiles in the map
        int mapWidth; // num of pixels wide in the map
        int mapHeight; // num of pixels tall in the map
        Vector2 offset;
        float scale;
        int informationLayerGid;

        public TiledMapInformation MapInfo => mapInfo;
        public int[,] InfoMapGrid => mapLayers[informationLayerName].Tiles;
        public TilePositionCache PositionCache => positionCache;

        public int MapRows => mapRows;
        public int MapCols => mapCols;
        public int MapWidth => mapWidth;
        public int MapHeight => mapHeight;
        public Vector2 Offset => offset;
        public int InformationLayerGid => informationLayerGid;

        // Hard-coded dictionary linking tile ID to tile type.
        public static readonly Dictionary<int, string> tileTypeMap = new Dictionary<int, string>()
        {
            { 1, "Floor" },
            { 2, "Door" },
            { 3, "Wall" },
            { 4, "Half Wall" },
            { 5, "Player Spawn" },
            { 7, "Border Floor" },
            { 9, "Enemy Spawn 1" },
            { 10, "Enemy Spawn 2" },
            { 11, "Enemy Spawn 3" },
            { 12, "Enemy Spawn 4" },
            { 13, "Enemy Spawn 5" },
            { 14, "Enemy Spawn 6" },
            { 15, "Enemy Spawn 7" },
            { 16, "Enemy Spawn 8" }
        };

        public bool IsWalkable(int tileId)
        {   // Floor, Player Spawn, Enemy Spawns
            tileId = tileId - informationLayerGid + 1;
            return tileId == 1 || tileId == 5 || tileId == 7 ||  (9 <= tileId && tileId <= 16);
        }

        public bool IsFlyable(int tileId)
        {   // Floor, Half-Wall, Player Spawn, Enemy Spawns
            tileId = tileId - informationLayerGid + 1;
            return tileId == 1 || tileId == 4 || tileId == 7 ||  tileId == 5 || (9 <= tileId && tileId <= 16);
        }

        public static readonly List<string> collidableTileType = new List<string>()
        {
            "Door", "Wall", "Half Wall"
        };

        // Hard-coded name used to define the "placeholder" information layer for Tiled maps.
        static readonly string informationLayerName = "Information Layer";

        public Map(string mapName, Vector2 mapOffset, float mapScale)
        {
            name = mapName;
            offset = mapOffset;
            scale = mapScale;

            tilesetManager = new TilesetManager();
            tilesetDescriptors = new List<TilesetDescriptor>();
            mapLayers = new Dictionary<string, MapLayer>();
            mapInfo = new TiledMapInformation();
            collidableTiles = new List<CollidableTiles>();

            MapLayer.ResetDepth();
        }

        public bool IsTileWalkable(int x, int y, CharacterType type, bool preventBorder = false)
        {
            var infoLayer = mapLayers[informationLayerName];
            if (x < 0 || x >= mapCols || y < 0 || y >= mapRows) return false;
            int tileId = infoLayer.Tiles[x, y];
            if (type == CharacterType.Flying)
                return IsFlyable(tileId) && (!preventBorder || tileId != 7);
            return IsWalkable(tileId) && (!preventBorder || tileId != 7);
        }
        
        public int GetTileWalkCost(int x, int y, CharacterType type)
        {
            var infoLayer = mapLayers[informationLayerName];
            if (x < 0 || x >= mapCols || y < 0 || y >= mapRows) return 0;
            int tileId = infoLayer.Tiles[x, y];
            if (tileId - informationLayerGid + 1 == 7) return 100000000;  // try not to walk on border floor
            if (type == CharacterType.Flying)
                return IsFlyable(tileId) ? 1 : 0;
            return IsWalkable(tileId) ? 1 : 0;
        }

        // Returns a random floor position on the map.
        public Vector2 SampleFloorPosition()
        {
            Random rng = RandomProvider.GetRandom();
            int x = rng.Next(0, mapCols);
            int y = rng.Next(0, mapRows);
            // exclude enemy spawn tiles and player spawn tiles
            var infoLayer = mapLayers[informationLayerName];
            int tileId = infoLayer.Tiles[x, y] - informationLayerGid + 1;

            while (tileId != 1)
            {
                x = rng.Next(0, mapCols);
                y = rng.Next(0, mapRows);
                tileId = infoLayer.Tiles[x, y] - informationLayerGid + 1;
            }

            return positionCache.GetWorldPosition(x, y);
        }

        // Debug tool used to reload content.
        public void Unload(CollisionComponent collisionComponent)
        {
            UnloadCollidableTiles(collisionComponent);
            // Other forms of unloading ??
        }

        // Debug tool for testing map changes.
        public void EditMap() // Use key "M"
        {
            // Check if InformationLayerName exists in the map layers, if so invert visibility
            if (mapLayers.TryGetValue(informationLayerName, out MapLayer layer))
            {
                layer.IsVisible = !layer.IsVisible;
            }

            //     // Example of processing based on tile types stored in TiledMapInformation
            //     var doorTiles = mapInfo.GetTilesOfType("Door");
            //     foreach (var rectangle in doorTiles)
            //     {
            //         // Example manipulation, e.g., changing properties associated with door tiles
            //         Console.WriteLine($"Processing door at {rectangle}");
            //     }
        }

        // Randomly chooses a map and calls LoadMap.
        public void LoadContent()
        {
            string mapPath = Path.Combine("Content", "Maps", name);
            Console.WriteLine($"Map name: {name}");

            // Load the map
            LoadMap(mapPath);
        }

        // Main map loading function.
        public void LoadMap(string xmlFilePath)
        {
            Random rng = RandomProvider.GetRandom();
            XDocument doc = XDocument.Load(xmlFilePath);
            XElement mapElement = doc.Element("map");
            int tileWidth = int.Parse(mapElement.Attribute("tilewidth").Value);
            int tileHeight = int.Parse(mapElement.Attribute("tileheight").Value);
            mapRows = int.Parse(mapElement.Attribute("height").Value);
            mapCols = int.Parse(mapElement.Attribute("width").Value);
            mapWidth = tileWidth * mapCols;
            mapHeight = tileHeight * mapRows;
            informationLayerGid = 1; //default
            // mapWidth = tileWidth * int.Parse(mapElement.Attribute("width").Value);
            // mapHeight = tileHeight * int.Parse(mapElement.Attribute("height").Value);

            // The only constructor that needs XML information
            positionCache = new TilePositionCache(tileWidth, tileHeight, offset, scale);

            // Extract tileset information
            foreach (XElement tilesetElement in doc.Descendants("tileset"))
            {
                int firstGid = int.Parse(tilesetElement.Attribute("firstgid").Value);
                string source = tilesetElement.Attribute("source").Value;
                string textureName = Path.Combine("Content/Maps/TileSets", Path.GetFileName(source));

                tilesetDescriptors.Add(new TilesetDescriptor(firstGid, textureName));
                if (source == "TileSets/Placeholders.tsx")
                {
                    informationLayerGid = firstGid;
                }
            }

            // Load corresponding textures
            foreach (TilesetDescriptor descriptor in tilesetDescriptors)
            {
                tilesetManager.LoadTilesetFromTsx(descriptor.TextureName, descriptor.FirstGid);
            }


            // Now parse the layer data
            foreach (XElement layerElement in doc.Descendants("layer"))
            {
                XElement dataElement = layerElement.Element("data");
                string[] rows = dataElement.Value.Trim().Split('\n');
                string name = layerElement.Attribute("name").Value;
                int width = int.Parse(layerElement.Attribute("width").Value);
                int height = int.Parse(layerElement.Attribute("height").Value);

                // Cache each position
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        positionCache.CachePosition(x, y);
                    }
                }

                // Parse the visibility attribute
                bool isVisible = true; // Default visibility is true
                XAttribute visibilityAttribute = layerElement.Attribute("visible");
                if (visibilityAttribute != null) isVisible = visibilityAttribute.Value == "1";

                // Create new layer and populate grid
                MapLayer layer = new MapLayer(name, width, height, isVisible);
                for (int y = 0; y < height; y++)
                {
                    string[] tiles = rows[y].Split(',');
                    for (int x = 0; x < width; x++)
                    {
                        int tileId = int.Parse(tiles[x].Trim());
                        if(tileId != 0){
                            TileSwapDict tileSwapDict = BazaarBountyGame.levelManager.MapManager.CurrentMapProperties.TileSwapDictionary;
                            // Get local ID + tileset name
                            (int localId, string localTilesetPath) = tilesetManager.GlobalToLocalID(tileId);
                            string tileSetName = Path.GetFileNameWithoutExtension(localTilesetPath);
                            string directoryPath = Path.GetDirectoryName(localTilesetPath);

                            (int swappedLocalId, string swappedTileSetName) = tileSwapDict.GetSwappedTile(localId, tileSetName, rng); // Add logic here for tile swapping.

                            layer.Tiles[x, y] = tilesetManager.LocalToGlobalID(swappedLocalId, Path.Combine(directoryPath, swappedTileSetName));
                        }


                        // Specific processing for the InformationLayerName
                        if (name == informationLayerName)
                        {
                            if (tileTypeMap.TryGetValue(tileId - informationLayerGid + 1, out var tileType))
                            {
                                Vector2 position = positionCache.GetDrawPosition(x, y);
                                Rectangle rectangle = new Rectangle((int)position.X, (int)position.Y, tileWidth,
                                    tileHeight);
                                mapInfo.AddTile(tileType, rectangle);
                            }
                        }
                    }
                }

                mapLayers[name] = layer;
            }
        }

        public void LoadCollidableTiles(CollisionComponent collisionComponent)
        {
            // clear collidable tiles from last map (if exists)
            UnloadCollidableTiles(collisionComponent);
            foreach (string tileType in collidableTileType)
            {
                var tiles = mapInfo.GetTilesOfType(tileType);
                foreach (var rectangle in tiles)
                {
                    var newTile = new CollidableTiles(tileType, new Vector2(rectangle.X, rectangle.Y), rectangle.Width,
                        rectangle.Height);
                    collidableTiles.Add(newTile);
                    collisionComponent.Insert(newTile);
                }
            }
        }

        public void UnloadCollidableTiles(CollisionComponent collisionComponent)
        {
            foreach (var tile in collidableTiles)
            {
                collisionComponent.Remove(tile);
            }

            collidableTiles.Clear();
        }

        public void ClampCameraToMap(PlayerCharacter player, OrthographicCamera camera, float widthPercentage,
            float heightPercentage)
        {
            var viewWidth = camera.BoundingRectangle.Width * widthPercentage / 2;
            var viewHeight = camera.BoundingRectangle.Height * heightPercentage / 2;

            // Calculate the margins to keep the camera within
            var leftBound = viewWidth / 2 + Offset.X;
            var rightBound = MapWidth - viewWidth / 2 + Offset.X;
            var topBound = viewHeight / 2 + Offset.Y;
            var bottomBound = MapHeight - viewHeight / 2 + Offset.Y;

            // Get the desired camera focus based on the player's position
            var focusPosition = player.Position;

            // Clamp the camera position to stay within the bounds
            focusPosition.X = MathHelper.Clamp(focusPosition.X, leftBound, rightBound);
            focusPosition.Y = MathHelper.Clamp(focusPosition.Y, topBound, bottomBound);

            // Set the camera to look at the clamped position
            camera.LookAt(focusPosition);
        }

        public void Update(GameTime gameTime)
        {
            // Nothing for now. (Potentially animated tiles)
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            foreach (var layer in mapLayers.Values)
            {
                if (!layer.IsVisible) continue; // Check visibility

                for (int y = 0; y < layer.Tiles.GetLength(1); y++)
                {
                    for (int x = 0; x < layer.Tiles.GetLength(0); x++)
                    {
                        int tileId = layer.Tiles[x, y];
                        if (tileId == 0) continue; // Skip empty tiles

                        Tile tile = tilesetManager.GetTile(tileId);
                        if (tile != null)
                        {
                            Vector2 drawPosition = positionCache.GetDrawPosition(x, y);
                            spriteBatch.Draw(tile.Texture, drawPosition, tile.SourceRect, Color.White, 0f, Vector2.Zero,
                                scale, SpriteEffects.None, layer.Depth);
                        }
                        else
                        {
                            Console.Write("null return from GetTile");
                        }
                    }
                }
            }

            foreach (var tiles in collidableTiles)
            {
                tiles.Draw(spriteBatch);
            }
        }
    }
}