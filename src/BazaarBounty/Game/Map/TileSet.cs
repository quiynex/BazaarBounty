using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.IO;
using MonoGame.Extended;
using MonoGame.Extended.Collisions;
using System;
using System.Diagnostics;

namespace BazaarBounty;

public class Tile
{
    public Rectangle SourceRect { get; private set; }
    public Texture2D Texture { get; private set; }

    public Tile(Texture2D texture, Rectangle sourceRect)
    {
        Texture = texture;
        SourceRect = sourceRect;
    }
}

public class Tileset
{
    private List<Tile> tiles = new List<Tile>();
    public int FirstGid { get; private set; }
    public string Path;
    public int TileCount;

    public Tileset(string texturePath, int tileWidth, int tileHeight, int firstGid, int tileCount)
    {
        Path = texturePath;
        Texture2D texture = BazaarBountyGame.GetGameInstance().Content.Load<Texture2D>(texturePath);

        FirstGid = firstGid;

        int tileCountX = texture.Width / tileWidth;
        int tileCountY = texture.Height / tileHeight;
        TileCount = tileCountX * tileCountY;
        if(tileCount != TileCount){
            Console.WriteLine("Mismatch in Tilecount calculation!");
        }

        for (int y = 0; y < tileCountY; y++)
        {
            for (int x = 0; x < tileCountX; x++)
            {
                Rectangle sourceRect = new Rectangle(x * tileWidth, y * tileHeight, tileWidth, tileHeight);
                tiles.Add(new Tile(texture, sourceRect));
            }
        }
    }

    public Tile GetTile(int id)
    {
        return tiles[id - FirstGid];
    }

    public IEnumerable<int> GetTileIdRange()
    {
        // This assumes the last tile ID is FirstGid + number of tiles - 1
        return Enumerable.Range(FirstGid, TileCount);
    }
}

public class TilesetManager
{
    private List<Tileset> tilesets;
    private Dictionary<int, Tileset> tileIdToTilesetMapping;

    public TilesetManager()
    {
        tilesets = new List<Tileset>();
        tileIdToTilesetMapping = new Dictionary<int, Tileset>();
    }

    public void LoadTilesetFromTsx(string tsxFilePath, int firstGid)
    {
        // Parse the XML
        XDocument tsxDoc = XDocument.Load(tsxFilePath);
        XElement tilesetElement = tsxDoc.Element("tileset");
        int tileWidth = int.Parse(tilesetElement.Attribute("tilewidth").Value);
        int tileHeight = int.Parse(tilesetElement.Attribute("tileheight").Value);
        // Optional
        //string name = tilesetElement.Attribute("name").Value;
        int tileCount = int.Parse(tilesetElement.Attribute("tilecount").Value);
        //int columns = int.Parse(tilesetElement.Attribute("columns").Value);

        XElement imageElement = tilesetElement.Element("image");
        string imagePath =
            "Maps\\TileSets\\" + Path.GetFileNameWithoutExtension(imageElement.Attribute("source").Value);

        // Create the Tileset object.
        Tileset tileset = new Tileset(imagePath, tileWidth, tileHeight, firstGid, tileCount);
        AddTileset(tileset);
    }

    public void AddTileset(Tileset tileset)
    {
        tilesets.Add(tileset);
        // Each tileset knows its own range and the number of tiles
        foreach (int tileId in tileset.GetTileIdRange())
        {
            tileIdToTilesetMapping[tileId] = tileset;
        }
    }
    public int LocalToGlobalID(int localTileId, string tilesetName)
    {
        var tileset = tilesets.FirstOrDefault(t => t.Path.EndsWith(tilesetName));
        if (tileset == null)
            throw new Exception($"Tileset with name {tilesetName} not found.");

        return tileset.FirstGid + localTileId - 1;
    }

    public (int, string) GlobalToLocalID(int globalTileId)
    {
        if (tileIdToTilesetMapping.TryGetValue(globalTileId, out Tileset tileset))
        {
            return (globalTileId - tileset.FirstGid + 1, tileset.Path);
        }

        throw new Exception($"Global tile ID {globalTileId} is not within the range of any tileset.");
    }

    public Tile GetTile(int globalTileId)
    {
        if (globalTileId == 0) return null; // Reserved for 'no tile'

        if (tileIdToTilesetMapping.TryGetValue(globalTileId, out Tileset tileset))
        {
            return tileset.GetTile(globalTileId);
        }

        return null; // Tile ID not found in any tileset
    }
}

// Used for loading Gid and TileSet name from map XML
public struct TilesetDescriptor
{
    public int FirstGid;
    public string TextureName;

    public TilesetDescriptor(int firstGid, string textureName)
    {
        FirstGid = firstGid;
        TextureName = textureName;
    }
}

public class TilePositionCache
{
    private Dictionary<(int, int), Vector2> positionCache;
    private int tileWidth;
    private int tileHeight;
    private Vector2 offset;
    private float scale;

    public TilePositionCache(int tileWidth, int tileHeight, Vector2 offset, float scale)
    {
        this.tileWidth = tileWidth;
        this.tileHeight = tileHeight;
        this.offset = offset;
        this.scale = scale;
        positionCache = new Dictionary<(int, int), Vector2>();
    }

    public void CachePosition(int x, int y)
    {
        Vector2 drawPosition = new Vector2(x * tileWidth, y * tileHeight) * scale + offset;
        positionCache[(x, y)] = drawPosition;
    }

    public Vector2 GetDrawPosition(int x, int y)
    {
        if (positionCache.TryGetValue((x, y), out Vector2 position))
        {
            return position;
        }

        return Vector2.Zero; // Fallback position, in case of an error
    }

    public (int, int) GetGridPosition(Vector2 position)
    {
        int x = (int)((position.X - offset.X) / tileWidth / scale);
        int y = (int)((position.Y - offset.Y) / tileHeight / scale);
        return (x, y);
    }

    public Vector2 GetWorldPosition(int x, int y)
    {
        Vector2 worldPosition = new Vector2((x + 0.5f) * tileWidth, (y + 0.5f) * tileHeight) * scale + offset;
        return worldPosition;
    }
}

public class CollidableTiles : ICollisionActor
{
    protected string tileType;
    protected string tileCategory;
    public string TileType => tileType;
    public string TileCategory => tileCategory;
    protected int tileWidth;
    protected int tileHeight;
    protected Vector2 position;
    protected IShapeF bounds;

    public IShapeF Bounds => bounds;
    public Vector2 Position => position;

    private Dictionary<string, string> tileTypeToCategory = new Dictionary<string, string>()
    {
        { "Wall", "impenetrable" },
        { "Door", "impenetrable" },
        { "Half Wall", "unwalkable" }
    };

    public CollidableTiles(string type, Vector2 position, int tilewidth, int tileheight)
    {
        tileType = type;
        tileCategory = tileTypeToCategory.TryGetValue(tileType, out var result) ? result : "Unknown";
        this.position = position;
        tileHeight = tileheight;
        tileWidth = tilewidth;
        bounds = new RectangleF(position.X, position.Y, tilewidth, tileheight);
    }

    public virtual void Draw(SpriteBatch spriteBatch)
    {
        if (BazaarBountyGame.Settings.Debug.Mode)
            spriteBatch.DrawRectangle((RectangleF)bounds, new Color(Color.Red, 0.5f),
                layerDepth: BazaarBountyGame.Settings.Debug.Depth);
    }

    public virtual void OnCollision(CollisionEventArgs collisionInfo)
    {
        // Debug.WriteLine("Collision occured on walls!!!");    
    }
}

public class TileWithProb
{
    public int TileId { get; set; }
    public string TileSetName { get; set; }
    public float Probability { get; set; }

    public TileWithProb(int tileId, string tileSetName, float probability)
    {
        TileId = tileId;
        TileSetName = tileSetName;
        Probability = probability;
    }
}
public class TileSwap
{
    public List<TileWithProb> TilesToSwapTo { get; set; }

    public TileSwap(List<TileWithProb> tilesToSwapTo)
    {
        TilesToSwapTo = tilesToSwapTo;
    }

    public (int, string) SelectTile(Random rng)
    {
        float totalProbability = TilesToSwapTo.Sum(e => e.Probability);
        float randomValue = (float)rng.NextDouble() * totalProbability;

        float cumulativeProbability = 0;
        foreach (var tile in TilesToSwapTo)
        {
            cumulativeProbability += tile.Probability;
            if (randomValue <= cumulativeProbability)
            {
                return (tile.TileId, tile.TileSetName);
            }
        }

        // Fallback in case something goes wrong
        return (0, null);
    }
}

public class TileSwapDict
{
    private Dictionary<(int, string), TileSwap> tileSwapDictionary;

    public TileSwapDict()
    {
        tileSwapDictionary = new Dictionary<(int, string), TileSwap>();
    }

    public void AddTileSwap(int originalTileId, string originalTileSet, TileSwap tileSwap)
    {
        tileSwapDictionary[(originalTileId, originalTileSet)] = tileSwap;
    }

    public (int, string) GetSwappedTile(int originalTileId, string tileSetName, Random rng)
    {
        if (tileSwapDictionary.TryGetValue((originalTileId, tileSetName), out var tileSwap))
        {
            return tileSwap.SelectTile(rng);
        }

        // Return the original tile if no swap is defined
        return (originalTileId, tileSetName);
    }
}