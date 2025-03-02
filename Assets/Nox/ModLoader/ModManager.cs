using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Nox.ModLoader.Discovers;
using Nox.ModLoader.Mods;

namespace Nox.ModLoader
{
    public class ModManager
    {
        public static List<Mod> Mods { get; private set; } = new();
        public static Mod[] GetMods() => Mods.ToArray();
        public static Mod GetMod(string id) => Mods.Find(x => x.GetMetadata().Match(id));

        public static UniTask<ResultLoadInfos> LoadMods(string[] ids) => LoadMods(ids, GlobalDiscover.Instance);
        public static UniTask<ResultLoadInfos> LoadMods() => LoadMods(GlobalDiscover.Instance);

        public static async UniTask<ResultLoadInfos> LoadMods(IDiscover discover)
        {
            var mods = new List<Mod>();
            var packages = discover.FindAllPackages();

            foreach (var package in packages)
                mods.Add(package.InternalDDiscover.CreateMod(package));

            return await PrepareMods(mods.ToArray());
        }

        public static async UniTask<ResultLoadInfos> LoadMods(string[] ids, IDiscover discover)
        {
            var mods = new List<Mod>();

            foreach (var id in ids)
            {
                var package = discover.FindPackage(id);
                if (package == null) continue;
                mods.Add(package.InternalDDiscover.CreateMod(package));
            }

            return await PrepareMods(mods.ToArray());
        }

        private static ResultLoad[] CheckModHaveMissingDependencies(Mod mod, Mod[] mods)
        {
            var metadata = mod.GetMetadata();
            if (metadata == null)
                return new[] { new ResultLoad {
                    Type = ResultLoad.ResultType.NoMetadata,
                    ForMod = metadata.GetId()
                } };

            List<ResultLoad> results = new();
            var CurrentMods = Mods;

            List<Mod> AllMods = new();
            AllMods.AddRange(CurrentMods);
            AllMods.AddRange(mods);

            foreach (var dependency in metadata.GetDepends())
            {
                var isDepend = false;
                foreach (var currentMod in AllMods)
                    if (currentMod.GetMetadata().Match(dependency))
                    {
                        isDepend = true;
                        break;
                    }

                if (!isDepend)
                    results.Add(new ResultLoad
                    {
                        Type = ResultLoad.ResultType.MissingDependency,
                        Message = $"Missing dependency {dependency.GetId()}({dependency.GetVersion()})",
                        CausedBy = metadata.GetId(),
                        ForMod = dependency.GetId()
                    });
            }

            return results.ToArray();
        }

        // Check if mod breaks other mods, if have any mod that breaks, don't load the mod
        private static ResultLoad[] CheckModBreakOtherMod(Mod mod, Mod[] mods)
        {
            var metadata = mod.GetMetadata();
            if (metadata == null)
                return new[] { new ResultLoad {
                    Type = ResultLoad.ResultType.NoMetadata,
                    ForMod = metadata.GetId()
                } };

            List<ResultLoad> results = new();
            var CurrentMods = Mods;

            List<Mod> AllMods = new();
            AllMods.AddRange(CurrentMods);
            AllMods.AddRange(mods);

            foreach (var dependency in metadata.GetBreaks())
                foreach (var currentMod in AllMods)
                    if (currentMod.GetMetadata().Match(dependency))
                        results.Add(new ResultLoad
                        {
                            Type = ResultLoad.ResultType.MissingDependency,
                            Message = $"Mod {metadata.GetId()}({metadata.GetVersion()}) breaks {dependency.GetId()}({dependency.GetVersion()})",
                            CausedBy = metadata.GetId(),
                            ForMod = dependency.GetId()
                        });

            return results.ToArray();
        }

        // Check if mod conflicts with other mods, if have any mod that conflicts, you can load the mod but show a warning
        private static ResultLoad[] CheckModConflictOtherMod(Mod mod, Mod[] mods)
        {
            var metadata = mod.GetMetadata();
            if (metadata == null)
                return new[] { new ResultLoad {
                    Type = ResultLoad.ResultType.NoMetadata,
                    ForMod = metadata.GetId()
                } };

            List<ResultLoad> results = new();
            var CurrentMods = Mods;

            List<Mod> AllMods = new();
            AllMods.AddRange(CurrentMods);
            AllMods.AddRange(mods);

            foreach (var dependency in metadata.GetConflicts())
                foreach (var currentMod in AllMods)
                    if (currentMod.GetMetadata().Match(dependency))
                        results.Add(new ResultLoad
                        {
                            Type = ResultLoad.ResultType.MissingDependency,
                            Message = $"Mod {metadata.GetId()}({metadata.GetVersion()}) conflicts with {dependency.GetId()}({dependency.GetVersion()})",
                            CausedBy = metadata.GetId(),
                            ForMod = dependency.GetId()
                        });

            return results.ToArray();
        }

        private static ResultLoad CheckModIsAllreadyLoaded(Mod mod)
        {
            var metadata = mod.GetMetadata();
            if (metadata == null)
                return new ResultLoad
                {
                    Type = ResultLoad.ResultType.NoMetadata,
                    ForMod = metadata.GetId()
                };

            foreach (var currentMod in Mods)
                if (currentMod.GetMetadata().Match(metadata))
                    return new ResultLoad
                    {
                        Type = ResultLoad.ResultType.AllreadyLoaded,
                        Message = $"Mod {metadata.GetId()}({metadata.GetVersion()}) is allready loaded",
                        ForMod = metadata.GetId()
                    };

            return null;
        }

        private static async UniTask<ResultLoadInfos> PrepareMods(Mod[] mods)
        {
            var results = new List<ResultLoad>();

            // Checking mods
            List<Mod> verifiedMods = new();

            foreach (var mod in mods)
            {
                var allreadyLoaded = CheckModIsAllreadyLoaded(mod);
                var missingDependencies = CheckModHaveMissingDependencies(mod, mods);
                var breaks = CheckModBreakOtherMod(mod, mods);
                var conflicts = CheckModConflictOtherMod(mod, mods);

                List<ResultLoad> AllResults = new();
                if (allreadyLoaded != null)
                    AllResults.Add(allreadyLoaded);
                AllResults.AddRange(missingDependencies);
                AllResults.AddRange(breaks);
                AllResults.AddRange(conflicts);

                if (AllResults.Count > 0)
                {
                    results.AddRange(AllResults);

                    if (AllResults.Exists(x => x.IsError))
                        continue;
                }

                verifiedMods.Add(mod);
            }

            var ErrorResults = results.FindAll(x => x.IsError);
            if (ErrorResults.Count > 0)
                return new() { Mods = new Mod[0], Results = results.ToArray() };

            verifiedMods.Reverse();
            verifiedMods.Sort((a, b) =>
            {
                var i = 0;
                foreach (var required in a.GetMetadata().GetDepends())
                    if (b.GetMetadata().Match(required.GetId())) i++;
                foreach (var required in b.GetMetadata().GetDepends())
                    if (a.GetMetadata().Match(required.GetId())) i--;
                return i;
            });

            // Loading mods
            List<Mod> loadedMods = new();

            foreach (var mod in verifiedMods)
            {
                if (mod.IsLoaded())
                {
                    loadedMods.Add(mod);
                    continue;
                }

                if (!await mod.Load())
                    results.Add(new ResultLoad
                    {
                        Type = ResultLoad.ResultType.LoadError,
                        Message = $"Failed to load mod {mod.GetMetadata().GetId()}({mod.GetMetadata().GetVersion()})",
                        ForMod = mod.GetMetadata().GetId()
                    });
                else loadedMods.Add(mod);

            }

            ErrorResults = results.FindAll(x => x.IsError);
            if (ErrorResults.Count > 0)
                return new() { Mods = new Mod[0], Results = results.ToArray() };

            Mods.AddRange(loadedMods);

            foreach (var mod in loadedMods)
                results.Add(new ResultLoad
                {
                    Type = ResultLoad.ResultType.Success,
                    Message = $"Mod {mod.GetMetadata().GetId()}({mod.GetMetadata().GetVersion()}) loaded successfully",
                    ForMod = mod.GetMetadata().GetId()
                });


            return new() { Mods = loadedMods.ToArray(), Results = results.ToArray() };
        }
    }

    public class ResultLoadInfos
    {
        public Mod[] Mods;
        public ResultLoad[] Results;

        public ResultLoad[] GetResults(ResultLoad.ResultType flags) => Array.FindAll(Results, x => x.Type.HasFlag(flags));
    }

    public class ResultLoad
    {
        public ResultType Type;
        public string Message;

        public string CausedBy;
        public string ForMod;

        public bool IsSuccess => Type.HasFlag(ResultType.IsSuccess);
        public bool IsWarning => Type.HasFlag(ResultType.IsWarning);
        public bool IsError => Type.HasFlag(ResultType.IsError);


        [Flags]
        public enum ResultType
        {
            Success = 1,
            MissingDependency = 2,
            IsConflit = 4,
            IsBreak = 8,
            NoMetadata = 16,
            AllreadyLoaded = 32,
            LoadError = 64,

            IsSuccess = Success,
            IsWarning = IsConflit,
            IsError = MissingDependency | IsBreak | NoMetadata | AllreadyLoaded | LoadError
        }
    }
}