using UnityEngine;

public enum ObjectInteractionOption
{
    None,

    // These two are considered as `All` when the team is not specified or is neutral.
    IgnoreFriend,
    IgnoreEnemy,
    All,
}

[CreateAssetMenu(menuName = "Project/Item Data")]
public class ItemData : ScriptableObject
{
    [field: SerializeField]
    public Sprite Sprite { get; private set; }

    [field: SerializeField]
    public ItemEffect Effect { get; private set; }

    [field: SerializeField]
    public int MaxItemAmount { get; private set; } = 1;

    [field: SerializeField]
    public CollisionBound CollisionBound { get; private set; }

    [field: SerializeField]
    public ObjectInteractionOption InteractionOption { get; private set; } =
        ObjectInteractionOption.IgnoreFriend;
}
