using System;

public class GameEventBus
{
    public readonly GameFlowEvents Flow = new GameFlowEvents();
    public readonly WorldEvents World = new WorldEvents();
    public readonly UnitEvents Unit = new UnitEvents();

    public void Reset()
    {
        Flow.Clear();
        World.Clear();
        Unit.Clear();
    }

    public class GameFlowEvents
    {
        public event Action OnTimeExpired;
        public event Action<TeamData> OnGameEnded;
        public event Action<MatchManager, GameScenario> OnEpisodeStarted;

        public void PublishTimeExpired() => OnTimeExpired?.Invoke();
        public void PublishGameEnded(TeamData winner) => OnGameEnded?.Invoke(winner);
        public void PublishEpisodeStarted(MatchManager matchManager, GameScenario gameScenario) => OnEpisodeStarted?.Invoke(matchManager, gameScenario);
        public void Clear() { OnTimeExpired = null; OnGameEnded = null; OnEpisodeStarted = null; }
    }

    public class WorldEvents
    {
        public event Action OnAbsorptionInterval;
        public void PublishAbsorption() => OnAbsorptionInterval?.Invoke();
        public void Clear() { OnAbsorptionInterval = null; }
    }

    public class UnitEvents
    {
        public event Action<Unit, ItemObject> OnItemPickedUp;
        /// <summary>killer is null when both units die simultaneously.</summary>
        public event Action<Unit, Unit> OnUnitKilled;
        public event Action<Unit, ItemData> OnItemDeposited;
        public event Action<ItemObject> OnItemAbsorbed;

        public void PublishItemPickedUp(Unit u, ItemObject i) => OnItemPickedUp?.Invoke(u, i);
        public void PublishUnitKilled(Unit killer, Unit victim) => OnUnitKilled?.Invoke(killer, victim);
        public void PublishItemDeposited(Unit u, ItemData d) => OnItemDeposited?.Invoke(u, d);
        public void PublishItemAbsorbed(ItemObject i) => OnItemAbsorbed?.Invoke(i);
        public void Clear() { OnItemPickedUp = null; OnUnitKilled = null; OnItemDeposited = null; OnItemAbsorbed = null; }
    }
}