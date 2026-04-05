using System;
using Unity.MLAgents.SideChannels;
using UnityEngine;

/// <summary>
/// One-way SideChannel that receives an integer seed from Python and applies it
/// to UnityEngine.Random before each episode begins.
/// </summary>
public class SeedChannel : SideChannel
{
    static readonly Guid kChannelId = new Guid("7a8b9c0d-1e2f-3a4b-5c6d-7e8f9a0b1c2d");

    public SeedChannel()
    {
        ChannelId = kChannelId;
    }

    protected override void OnMessageReceived(IncomingMessage msg)
    {
        int seed = msg.ReadInt32();
        UnityEngine.Random.InitState(seed);
    }
}
