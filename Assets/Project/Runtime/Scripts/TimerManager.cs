using System;
using System.Collections.Generic;
using UnityEngine;

public class GameTimer
{
    public float Duration { get; private set; }
    public bool IsLoop { get; private set; }
    public Action OnComplete { get; private set; }
    
    public float CurrentTime { get; private set; }
    public float Ratio => Mathf.Clamp01(CurrentTime / Duration);
    public float RemainingTime => Mathf.Max(0, Duration - CurrentTime);
    private bool isFinished;

    public GameTimer(float duration, bool isLoop, Action onComplete)
    {
        Duration = duration;
        IsLoop = isLoop;
        OnComplete = onComplete;
        CurrentTime = 0f;
        isFinished = false;
    }

    public bool Tick(float dt)
    {
        if (isFinished) return true;

        CurrentTime += dt;
        if (CurrentTime >= Duration)
        {
            UnityEngine.Debug.Log("Timer completed");
            OnComplete?.Invoke();
            if (IsLoop) CurrentTime -= Duration;
            else { isFinished = true; return true; }
        }
        return false;
    }
}

public class TimerManager
{
    private List<GameTimer> timers = new List<GameTimer>();
    // Incremented by Clear(). A timer callback can synchronously restart the episode
    // (time expired → game ended → EpisodeBegin → Clear + AddTimer) while Tick() is still
    // iterating; Tick() uses this to stop instead of touching the new episode's timers.
    private int generation;

    public GameTimer AddTimer(float duration, bool isLoop, Action callback)
    {
        var timer = new GameTimer(duration, isLoop, callback);
        timers.Add(timer);
        return timer;
    }

    public void Tick(float dt)
    {
        int gen = generation;
        for (int i = timers.Count - 1; i >= 0; i--)
        {
            if (i >= timers.Count) continue;
            GameTimer timer = timers[i];
            bool finished = timer.Tick(dt);
            if (gen != generation) return; // list was cleared inside the callback
            // Remove by reference: a callback may have added or removed other timers.
            if (finished) timers.Remove(timer);
        }
    }

    public void Clear()
    {
        timers.Clear();
        generation++;
    }
}