namespace Nox.Mods
{
    public class ModLoadResult
    {
        public ModLoadResultType Success { get; }
        public string Message { get; }
        public string Path { get; set; }

        public ModLoadResult(string path, ModLoadResultType success, string message)
        {
            Path = path;
            Success = success;
            Message = message;
        }

        public bool IsError => Success == ModLoadResultType.Error;
    }

    public enum ModLoadResultType
    {
        Success,
        Suggestion,
        Warning,
        Error
    }
}