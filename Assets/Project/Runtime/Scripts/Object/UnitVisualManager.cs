using UnityEngine;

[System.Serializable]
public class TeamArt {
    public Sprite Carrier;
    public Sprite Collector;
    public Sprite Hunter;
}

public class UnitVisualManager : MonoBehaviour
{
    [Header("Teams")]
    [field: SerializeField]
    public TeamData TeamA { get; private set; }

    [field: SerializeField]
    public TeamData TeamB { get; private set; }

    [Header("Unit Data")]
    [field: SerializeField]
    public UnitData CarrierData { get; private set; }

    [field: SerializeField]
    public UnitData CollectorData { get; private set; }

    [field: SerializeField]
    public UnitData HunterData { get; private set; }

    [Header("Unit Art")]
    [field: SerializeField]
    public TeamArt TeamAArt { get; private set; }

    [field: SerializeField]
    public TeamArt TeamBArt { get; private set; }

    public static UnitVisualManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null) {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else {
            Destroy(gameObject);
        }
    }

    public Sprite GetArt(UnitData unitData, TeamData teamData)
    {
        if (teamData == TeamA) {
            if (unitData == CarrierData) {
                return TeamAArt.Carrier;
            }
            else if (unitData == CollectorData) {
                return TeamAArt.Collector;
            }
            else if (unitData == HunterData) {
                return TeamAArt.Hunter;
            }
        }
        else if (teamData == TeamB) {
            if (unitData == CarrierData) {
                return TeamBArt.Carrier;
            }
            else if (unitData == CollectorData) {
                return TeamBArt.Collector;
            }
            else if (unitData == HunterData) {
                return TeamBArt.Hunter;
            }
        }

        return null;
    }
}
