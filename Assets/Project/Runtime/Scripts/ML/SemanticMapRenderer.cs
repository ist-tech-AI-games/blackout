using UnityEngine;

/// <summary>
/// Renders a semantic ID map of the game state to two RenderTextures (one per team perspective).
/// Each pixel stores an entity type ID (wall, storage, unit, item) as a grayscale value.
/// Team A and Team B see ally/enemy IDs flipped relative to each other.
///
/// ID encoding (must match StreamingAssets/semantic_map_config.json and Python preprocessor):
///   0=empty, 1=wall, 2=ally_storage, 3=enemy_storage, 4=ally_unit, 5=enemy_unit, 6+=item
///
/// Usage:
///   1. Attach to a GameObject in the ML training scene.
///   2. Set references in inspector (gameScenario, coordinator, defaultMapSize, resolutionScale).
///   3. BlackOutEpisodeCoordinator calls CreateTextures() in Awake, then assigns RTs to agents.
///   4. BlackOutEpisodeCoordinator calls Render() each FixedUpdate.
/// </summary>
public class SemanticMapRenderer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameScenario gameScenario;
    [SerializeField] private ItemData[] knownItems;

    [Header("Config")]
    [Tooltip("Expected map size in tiles. Used to pre-create RenderTextures before map loads.")]
    [SerializeField] private Vector2Int defaultMapSize = new Vector2Int(24, 24);
    [Tooltip("Pixels per tile. resolutionScale=4 gives 4×4 pixels per tile for sub-tile precision.")]
    [SerializeField] private int resolutionScale = 4;
    [Tooltip("First item ID in the semantic map. Must match item_id_offset in semantic_map_config.json.")]
    [SerializeField] private int itemIdOffset = 6;

    // Semantic IDs — must match semantic_map_config.json
    private const byte ID_EMPTY = 0;
    private const byte ID_WALL = 1;
    private const byte ID_ALLY_STORAGE = 2;
    private const byte ID_ENEMY_STORAGE = 3;
    private const byte ID_ALLY_UNIT = 4;
    private const byte ID_ENEMY_UNIT = 5;

    /// <summary>RenderTexture for Team A agents (Team A = ally).</summary>
    public RenderTexture RenderTextureTeamA { get; private set; }
    /// <summary>RenderTexture for Team B agents (Team B = ally).</summary>
    public RenderTexture RenderTextureTeamB { get; private set; }

    private Texture2D textureA;
    private Texture2D textureB;
    private byte[] pixelsA;
    private byte[] pixelsB;
    private byte[] backgroundA;
    private byte[] backgroundB;
    private int texWidth;
    private int texHeight;
    private bool isInitialized;

    // ===== Setup (called from BlackOutEpisodeCoordinator.Awake) =====

    /// <summary>
    /// Creates RenderTexture objects using defaultMapSize * resolutionScale dimensions.
    /// Must be called BEFORE agent.Setup() so RenderTextureSensorComponent can reference the RTs.
    /// </summary>
    public void CreateTextures()
    {
        texWidth = defaultMapSize.x * resolutionScale;
        texHeight = defaultMapSize.y * resolutionScale;
        AllocateAll();
    }

    /// <summary>
    /// Subscribes to OnEpisodeStarted to re-render the static tile background when the map reloads.
    /// Must be called AFTER gameScenario.Initialize() (EventBus must exist).
    /// </summary>
    public void SubscribeEvents(GameEventBus eventBus)
    {
        eventBus.Flow.OnEpisodeStarted += OnEpisodeStarted;
    }

    // ===== Per-step render (called from BlackOutEpisodeCoordinator.FixedUpdate) =====

    /// <summary>
    /// Renders the current game state into both team RenderTextures.
    /// Call once per FixedUpdate, after game logic has run.
    /// </summary>
    public void Render()
    {
        if (!isInitialized) return;

        MapManager mapManager = gameScenario.MapManager;
        MatchManager matchManager = gameScenario.MatchManager;
        TeamData teamA = matchManager.TeamA;
        Vector2 mapOrigin = mapManager.MapOriginWorld;

        // Start from pre-computed tile background
        System.Array.Copy(backgroundA, pixelsA, pixelsA.Length);
        System.Array.Copy(backgroundB, pixelsB, pixelsB.Length);

        // Overlay ground items
        foreach (ItemObject item in gameScenario.LevelDirector.ActiveItems)
        {
            if (item.State != ItemObject.ItemState.OnGround) continue;
            int idx = GetItemIndex(item.ItemData);
            if (idx < 0) continue;
            byte id = (byte)(itemIdOffset + idx);
            WritePixel(pixelsA, item.GlobalPos, mapOrigin, id);
            WritePixel(pixelsB, item.GlobalPos, mapOrigin, id);
        }

        // Overlay units (written last → highest priority)
        foreach (Unit unit in matchManager.Units)
        {
            if (!unit.gameObject.activeInHierarchy) continue;
            bool isTeamAUnit = unit.Team == teamA;
            WritePixel(pixelsA, unit.GlobalPos, mapOrigin, isTeamAUnit ? ID_ALLY_UNIT : ID_ENEMY_UNIT);
            WritePixel(pixelsB, unit.GlobalPos, mapOrigin, isTeamAUnit ? ID_ENEMY_UNIT : ID_ALLY_UNIT);
        }

        textureA.LoadRawTextureData(pixelsA);
        textureA.Apply(false);
        Graphics.Blit(textureA, RenderTextureTeamA);

        textureB.LoadRawTextureData(pixelsB);
        textureB.Apply(false);
        Graphics.Blit(textureB, RenderTextureTeamB);
    }

    // ===== Private helpers =====

    private void OnEpisodeStarted(MatchManager mm, GameScenario scenario)
    {
        // Resize if map dimensions changed (e.g. different map configs)
        int newW = gameScenario.MapManager.MapWidth * resolutionScale;
        int newH = gameScenario.MapManager.MapHeight * resolutionScale;
        if (newW != texWidth || newH != texHeight)
        {
            texWidth = newW;
            texHeight = newH;
            AllocateAll();
        }

        RenderBackground();
        isInitialized = true;
        // Pre-render immediately so MapObsAgent's first RequestDecision in the next
        // FixedUpdate sees a valid RenderTexture, not an empty/stale one.
        Render();
    }

    private void AllocateAll()
    {
        RenderTextureTeamA?.Release();
        RenderTextureTeamB?.Release();

        // sRGB RT: ML-Agents' RenderTextureSensor reads via ReadPixels into an sRGB Texture2D.
        // Using a linear RT causes the read to apply linear→sRGB gamma encoding, which corrupts
        // small semantic ID values (e.g. wall id=1 → linear 0.004 → sRGB byte 46 → wrong channel).
        // With sRGB RT + sRGB source texture, the blit is a round-trip (sRGB→linear→sRGB = identity)
        // and ML-Agents receives id/255 directly, which round-trips correctly in preprocess_graphic.
        RenderTextureTeamA = new RenderTexture(texWidth, texHeight, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        RenderTextureTeamB = new RenderTexture(texWidth, texHeight, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        RenderTextureTeamA.Create();
        RenderTextureTeamB.Create();
        ClearRT(RenderTextureTeamA);
        ClearRT(RenderTextureTeamB);

        // sRGB source: R=G=B=semantic_id. Blit to sRGB RT is a lossless round-trip.
        // Grayscale formula: 0.299*R + 0.587*G + 0.114*B = id/255 when R=G=B.
        textureA = new Texture2D(texWidth, texHeight, TextureFormat.RGB24, false, false);
        textureB = new Texture2D(texWidth, texHeight, TextureFormat.RGB24, false, false);
        pixelsA = new byte[texWidth * texHeight * 3];
        pixelsB = new byte[texWidth * texHeight * 3];
        backgroundA = new byte[texWidth * texHeight * 3];
        backgroundB = new byte[texWidth * texHeight * 3];
    }

    private static void ClearRT(RenderTexture rt)
    {
        var prev = RenderTexture.active;
        RenderTexture.active = rt;
        GL.Clear(false, true, Color.clear);
        RenderTexture.active = prev;
    }

    /// <summary>
    /// Pre-renders the static tile layer (wall / storage / empty) for both team perspectives.
    /// Called once per episode after the map is generated.
    /// </summary>
    private void RenderBackground()
    {
        MapManager mapManager = gameScenario.MapManager;
        MatchManager matchManager = gameScenario.MatchManager;
        TeamData teamA = matchManager.TeamA;
        Vector2 mapOrigin = mapManager.MapOriginWorld;

        for (int py = 0; py < texHeight; py++)
        {
            for (int px = 0; px < texWidth; px++)
            {
                // Center of this pixel in world space
                Vector2 worldPos = mapOrigin + new Vector2(px + 0.5f, py + 0.5f) / resolutionScale;
                Vector2Int cell = mapManager.WorldToCell(worldPos);
                MapTile tile = mapManager.GetTile(cell);

                byte idA, idB;
                if (tile == null || tile.TileData.TileCollisionOption == TileCollisionOption.BlockAll)
                {
                    idA = idB = ID_WALL;
                }
                else if (tile.OwnedRegion is Storage)
                {
                    bool isTeamAStorage = tile.OwnedRegion.OwnedTeam == teamA;
                    idA = isTeamAStorage ? ID_ALLY_STORAGE : ID_ENEMY_STORAGE;
                    idB = isTeamAStorage ? ID_ENEMY_STORAGE : ID_ALLY_STORAGE;
                }
                else
                {
                    idA = idB = ID_EMPTY;
                }

                int byteIdx = (py * texWidth + px) * 3;
                backgroundA[byteIdx]     = idA; // R
                backgroundA[byteIdx + 1] = idA; // G
                backgroundA[byteIdx + 2] = idA; // B
                backgroundB[byteIdx]     = idB;
                backgroundB[byteIdx + 1] = idB;
                backgroundB[byteIdx + 2] = idB;
            }
        }
    }

    private void WritePixel(byte[] pixels, Vector2 worldPos, Vector2 mapOrigin, byte id)
    {
        int px = Mathf.FloorToInt((worldPos.x - mapOrigin.x) * resolutionScale);
        int py = Mathf.FloorToInt((worldPos.y - mapOrigin.y) * resolutionScale);
        if (px < 0 || px >= texWidth || py < 0 || py >= texHeight) return;
        int byteIdx = (py * texWidth + px) * 3;
        pixels[byteIdx]     = id; // R
        pixels[byteIdx + 1] = id; // G
        pixels[byteIdx + 2] = id; // B
    }

    private int GetItemIndex(ItemData itemData)
    {
        if (knownItems == null) return -1;
        for (int i = 0; i < knownItems.Length; i++)
            if (knownItems[i] == itemData) return i;
        return -1;
    }

    private void OnDestroy()
    {
        if (gameScenario?.EventBus != null)
            gameScenario.EventBus.Flow.OnEpisodeStarted -= OnEpisodeStarted;
        RenderTextureTeamA?.Release();
        RenderTextureTeamB?.Release();
    }
}
