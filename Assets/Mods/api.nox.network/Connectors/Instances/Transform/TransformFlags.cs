namespace api.nox.network.Instances.Transform
{
    [System.Flags]
    public enum TransformFlags : byte
    {
        None = 0,
        Position = 1,
        Rotation = 2,
        Scale = 4,
        Velocity = 8,
        AngularVelocity = 16,
        All = 31,
        Rigidbody = 24,
        Transform = 7
    }
}