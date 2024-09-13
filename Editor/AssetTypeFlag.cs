namespace Editor.Tools
{
    public enum AssetTypeFlag
    {
        None = 0,
        Prefab = 1 << 0,
        Material = 1 << 2,
        Texture = 1 << 3,

        // Sprite = 1 << 4,
        AudioClip = 1 << 4,
        AnimationClip = 1 << 5,
        Mesh = 1 << 6,
        Model = 1 << 7,
    }
}