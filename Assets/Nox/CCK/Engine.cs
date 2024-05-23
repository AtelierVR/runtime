namespace Nox.CCK
{
    public enum Engine : byte
    {
        None = 0,
        Unity = 1,
        Unreal = 2,
        Godot = 3,
        Source = 4
    }

    public static class EngineExtensions
    {
        public static string GetEngineName(this Engine engine) => engine switch
        {
            Engine.Unity => "unity",
            Engine.Unreal => "unreal",
            Engine.Godot => "godot",
            Engine.Source => "source",
            _ => null,
        };

        public static Engine GetEngineFromName(string name) => name switch
        {
            "unity" => Engine.Unity,
            "unreal" => Engine.Unreal,
            "godot" => Engine.Godot,
            "source" => Engine.Source,
            _ => Engine.None,
        };
    }
}