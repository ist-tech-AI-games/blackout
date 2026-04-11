using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using SensorCompressionType = Unity.MLAgents.Sensors.SensorCompressionType;

/// <summary>
/// Broadcasts both team semantic maps (TeamA, TeamB) as two separate visual observations
/// per step. This agent exists solely to reduce gRPC graphic transmissions from 10 to 2:
/// instead of each of the 10 BlackOutAgents sending a copy of their team RenderTexture,
/// only this agent sends both textures once.
///
/// Setup (Unity Editor):
///   1. Add this component to a GameObject in the ML training scene.
///   2. Add exactly one RenderTextureSensorComponent to the same GameObject (TeamA texture).
///      Python derives the TeamB graphic by flipping ally/enemy channels — no second texture needed.
///   3. Behavior Parameters: Name = "BlackOutMap", Continuous Actions = 0,
///      Discrete Branches = 0, Vector Observations = 0.
///   4. Assign this agent in BlackOutEpisodeCoordinator inspector.
/// </summary>
public class MapObsAgent : Agent
{
    private void FixedUpdate() => RequestDecision();

    public override void OnActionReceived(ActionBuffers actions) { }

    /// <summary>
    /// Assigns the TeamA RenderTexture to the RenderTextureSensorComponent.
    /// Python derives the TeamB graphic by flipping ally/enemy channels.
    /// Must be called in the coordinator's Awake phase, before Agent.OnEnable →
    /// InitializeSensors runs.
    /// </summary>
    public void Setup(RenderTexture teamATexture)
    {
        var rtSensor = GetComponent<RenderTextureSensorComponent>();
        if (rtSensor == null)
        {
            Debug.LogError("[MapObsAgent] Needs a RenderTextureSensorComponent.", this);
            return;
        }

        rtSensor.RenderTexture = teamATexture;
        rtSensor.Grayscale = true;
        rtSensor.CompressionType = SensorCompressionType.None;
    }
}
