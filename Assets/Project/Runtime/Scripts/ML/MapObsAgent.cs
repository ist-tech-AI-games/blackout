using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

/// <summary>
/// Broadcasts the TeamA semantic map as a visual observation each step.
/// Uses DynamicRTSensorComponent so the RenderTexture can be assigned after
/// the agent's sensors are initialized (avoiding the timing race between
/// BlackOutEpisodeCoordinator.Awake and Agent.OnEnable→InitializeSensors).
///
/// Setup (Unity Editor):
///   1. Attach this component to a GameObject in the ML training scene.
///   2. Do NOT attach a RenderTextureSensorComponent — this agent creates its own sensor.
///   3. Behavior Parameters: Name = "BlackOutMap", Continuous Actions = 0,
///      Discrete Branches = 0, Vector Observations = 0.
///   4. Assign this agent in BlackOutEpisodeCoordinator inspector.
/// </summary>
public class MapObsAgent : Agent
{
    private DynamicRTSensorComponent _sensorComp;

    private void Awake()
    {
        // Remove any stale RenderTextureSensorComponent to prevent duplicate sensors.
        var legacy = GetComponent<RenderTextureSensorComponent>();
        if (legacy != null)
        {
            Debug.LogWarning("[MapObsAgent] Removing RenderTextureSensorComponent — sensor is now created by DynamicRTSensorComponent.", this);
            DestroyImmediate(legacy);
        }

        // Ensure DynamicRTSensorComponent exists before OnEnable → InitializeSensors runs.
        _sensorComp = GetComponent<DynamicRTSensorComponent>();
        if (_sensorComp == null)
            _sensorComp = gameObject.AddComponent<DynamicRTSensorComponent>();
    }

    /// <summary>
    /// Assigns the TeamA RenderTexture. Safe to call before or after InitializeSensors —
    /// DynamicRTSensorComponent handles both orderings internally.
    /// </summary>
    public void Setup(RenderTexture teamATexture)
    {
        _sensorComp.SetRenderTexture(teamATexture);
    }

    private void FixedUpdate() => RequestDecision();

    public override void OnActionReceived(ActionBuffers actions) { }
}

/// <summary>
/// SensorComponent that creates a DynamicRTSensor at InitializeSensors time.
/// The RenderTexture can be assigned before or after CreateSensors() is called;
/// _pendingTexture covers the case where Setup hasn't run yet.
/// </summary>
public class DynamicRTSensorComponent : SensorComponent
{
    private RenderTexture _pendingTexture;
    private DynamicRTSensor _sensor;

    public void SetRenderTexture(RenderTexture rt)
    {
        _pendingTexture = rt;
        _sensor?.SetRenderTexture(rt);
    }

    public override ISensor[] CreateSensors()
    {
        _sensor = new DynamicRTSensor("TeamAMap");
        if (_pendingTexture != null)
            _sensor.SetRenderTexture(_pendingTexture);
        return new ISensor[] { _sensor };
    }
}

/// <summary>
/// Visual sensor that reads from a RenderTexture assigned at runtime.
/// The RT reference can be updated after sensor creation via SetRenderTexture().
/// Reads pixel bytes directly (GetPixels32) to avoid any color-space conversion.
/// </summary>
internal class DynamicRTSensor : ISensor
{
    private RenderTexture _rt;
    private Texture2D _readBuffer;
    private readonly string _name;

    // Dimensions used for the ObservationSpec — updated when RT is first assigned.
    private int _width;
    private int _height;
    private ObservationSpec _spec;

    internal DynamicRTSensor(string name)
    {
        _name = name;
        // Placeholder spec; real dimensions are set when SetRenderTexture is called.
        _width = 1;
        _height = 1;
        _spec = ObservationSpec.Visual(1, _height, _width);
    }

    internal void SetRenderTexture(RenderTexture rt)
    {
        _rt = rt;
        if (rt == null) return;

        if (_width != rt.width || _height != rt.height)
        {
            _width = rt.width;
            _height = rt.height;
            _spec = ObservationSpec.Visual(1, _height, _width);

            if (_readBuffer != null)
                Object.Destroy(_readBuffer);
            // Non-mipmap, non-linear (sRGB): ReadPixels from sRGB RT preserves raw bytes.
            _readBuffer = new Texture2D(_width, _height, TextureFormat.RGB24, false, false);
        }
    }

    public string GetName() => _name;
    public ObservationSpec GetObservationSpec() => _spec;
    public byte[] GetCompressedObservation() => null;
    public CompressionSpec GetCompressionSpec() => CompressionSpec.Default();
    public void Update() { }
    public void Reset() { }

    public int Write(ObservationWriter writer)
    {
        if (_rt == null || _readBuffer == null) return 0;

        // Read the RT into a CPU-side Texture2D.
        // sRGB source + sRGB Texture2D → GetPixels32 returns raw sRGB bytes (no gamma applied).
        var prev = RenderTexture.active;
        RenderTexture.active = _rt;
        _readBuffer.ReadPixels(new Rect(0, 0, _width, _height), 0, 0, false);
        _readBuffer.Apply(false);
        RenderTexture.active = prev;

        var pixels = _readBuffer.GetPixels32();

        // Texture2D stores rows bottom-up; ObservationWriter expects top-down.
        // R=G=B=semantic_id, so channel 0 (R) alone is sufficient.
        for (int y = 0; y < _height; y++)
        {
            int srcY = _height - 1 - y;
            for (int x = 0; x < _width; x++)
                writer[0, y, x] = pixels[srcY * _width + x].r / 255f;
        }
        return _width * _height;
    }
}
