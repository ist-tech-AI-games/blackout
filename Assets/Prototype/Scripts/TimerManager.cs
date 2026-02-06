using System;
using System.Collections.Generic;

public class GameTimer
{
    public float Duration { get; private set; }
    public bool IsLoop { get; private set; }
    public Action OnComplete { get; private set; }
    
    private float currentTime;
    private bool isFinished;

    public GameTimer(float duration, bool isLoop, Action onComplete)
    {
        Duration = duration;
        IsLoop = isLoop;
        OnComplete = onComplete;
        currentTime = 0f;
        isFinished = false;
    }

    public bool Tick(float dt)
    {
        if (isFinished) return true;

        currentTime += dt;
        if (currentTime >= Duration)
        {
            UnityEngine.Debug.Log("Timer completed");
            OnComplete?.Invoke();
            if (IsLoop) currentTime -= Duration;
            else { isFinished = true; return true; }
        }
        return false;
    }
}

public class TimerManager
{
    private List<GameTimer> timers = new List<GameTimer>();

    public GameTimer AddTimer(float duration, bool isLoop, Action callback)
    {
        var timer = new GameTimer(duration, isLoop, callback);
        timers.Add(timer);
        return timer;
    }

    public void Tick(float dt)
    {
        for (int i = timers.Count - 1; i >= 0; i--)
        {
            if (timers[i].Tick(dt)) timers.RemoveAt(i);
        }
    }

    public void Clear() => timers.Clear();
}