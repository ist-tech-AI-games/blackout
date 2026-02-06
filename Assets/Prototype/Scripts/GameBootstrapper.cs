using UnityEngine;

public class GameBootstrapper : MonoBehaviour
{
    [SerializeField] private GameScenario scenario;
    [SerializeField] private PlayerUnitController playerController;

    private void Awake()
    {
        scenario.Initialize();
        playerController?.Initialize(scenario);
        scenario.EpisodeBegin();
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        playerController?.OnUpdate(dt);
        scenario.EpisodeUpdate(dt);
    }
}