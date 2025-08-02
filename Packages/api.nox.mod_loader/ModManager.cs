using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Nox.ModLoader.Discovers;
using Nox.ModLoader.Mods;
using Logger = Nox.CCK.Utils.Logger;

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
            => await PrepareMods((from id in ids
                    select discover.FindPackage(id)
                    into package
                    where package != null
                    select package.InternalDDiscover.CreateMod(package))
                .ToArray());

        private static ResultLoad[] CheckModHaveMissingDependencies(Mod mod, Mod[] mods)
        {
            var metadata = mod.GetMetadata();
            if (metadata == null)
                return new[]
                {
                    new ResultLoad
                    {
                        Type = ResultLoad.ResultType.NoMetadata,
                        ForMod = "<unknown>"
                    }
                };

            List<ResultLoad> results = new();
            List<Mod> allMods = new();
            allMods.AddRange(Mods);
            allMods.AddRange(mods);

            foreach (var dependency in metadata.GetDepends())
            {
                var isDepend = allMods.Any(currentMod => currentMod.GetMetadata().Match(dependency));

                if (!isDepend)
                    results.Add(new ResultLoad
                    {
                        Type = ResultLoad.ResultType.MissingDependency,
                        Message = $"Missing dependency {dependency.GetId()}@{dependency.GetVersion()} for {metadata.GetId()}@{metadata.GetVersion()}",
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
                return new[]
                {
                    new ResultLoad
                    {
                        Type = ResultLoad.ResultType.NoMetadata,
                        ForMod = "<unknown>"
                    }
                };

            List<Mod> allMods = new();
            allMods.AddRange(Mods);
            allMods.AddRange(mods);

            return (from dependency in metadata.GetBreaks()
                    from currentMod in allMods
                    where currentMod.GetMetadata().Match(dependency)
                    select new ResultLoad
                    {
                        Type = ResultLoad.ResultType.MissingDependency,
                        Message =
                            $"Mod {metadata.GetId()}@{metadata.GetVersion()} breaks {dependency.GetId()}@{dependency.GetVersion()}",
                        CausedBy = metadata.GetId(),
                        ForMod = dependency.GetId()
                    })
                .ToArray();
        }

        // Check if mod conflicts with other mods, if have any mod that conflicts, you can load the mod but show a warning
        private static ResultLoad[] CheckModConflictOtherMod(Mod mod, Mod[] mods)
        {
            var metadata = mod.GetMetadata();
            if (metadata == null)
                return new[]
                {
                    new ResultLoad
                    {
                        Type = ResultLoad.ResultType.NoMetadata,
                        ForMod = "<unknown>"
                    }
                };

            List<Mod> allMods = new();
            allMods.AddRange(Mods);
            allMods.AddRange(mods);

            return (from dependency in metadata.GetConflicts()
                    from currentMod in allMods
                    where currentMod.GetMetadata().Match(dependency)
                    select new ResultLoad
                    {
                        Type = ResultLoad.ResultType.MissingDependency,
                        Message =
                            $"Mod {metadata.GetId()}@{metadata.GetVersion()} conflicts with {dependency.GetId()}@{dependency.GetVersion()}",
                        CausedBy = metadata.GetId(),
                        ForMod = dependency.GetId()
                    })
                .ToArray();
        }

        private static ResultLoad CheckModIsAlreadyLoaded(Mod mod)
        {
            var metadata = mod.GetMetadata();
            if (metadata == null)
                return new ResultLoad
                {
                    Type = ResultLoad.ResultType.NoMetadata,
                    ForMod = "<unknown>"
                };

            if (Mods.Any(currentMod => currentMod.GetMetadata().Match(metadata)))
                return new ResultLoad
                {
                    Type = ResultLoad.ResultType.AlreadyLoaded,
                    Message = $"Mod {metadata.GetId()}@{metadata.GetVersion()} is already loaded",
                    ForMod = metadata.GetId()
                };

            return null;
        }

        private static void GetRelations(Mod mod, Mod[] inLoading, ref List<Mod> dependencies)
        {
            var metadata = mod.GetMetadata();
            if (metadata == null)
                return;

            List<Mod> allMods = new();
            allMods.AddRange(Mods);
            allMods.AddRange(inLoading);

            foreach (var dependency in metadata.GetRelations())
            {
                var depend = allMods.FirstOrDefault(currentMod => currentMod.GetMetadata().Match(dependency));
                if (depend == null)
                    continue;

                if (dependencies.Contains(depend))
                    continue;

                dependencies.Add(depend);
                GetRelations(depend, inLoading, ref dependencies);
            }
        }

        private static async UniTask<ResultLoadInfos> PrepareMods(Mod[] mods)
        {
            var results = new List<ResultLoad>();

            // Checking mods
            List<Mod> verifiedMods = new();

            foreach (var mod in mods)
            {
                var alreadyLoaded = CheckModIsAlreadyLoaded(mod);
                var missingDependencies = CheckModHaveMissingDependencies(mod, mods);
                var breaks = CheckModBreakOtherMod(mod, mods);
                var conflicts = CheckModConflictOtherMod(mod, mods);

                List<ResultLoad> allResults = new();
                if (alreadyLoaded != null)
                    allResults.Add(alreadyLoaded);
                allResults.AddRange(missingDependencies);
                allResults.AddRange(breaks);
                allResults.AddRange(conflicts);

                if (allResults.Count > 0)
                {
                    results.AddRange(allResults);

                    if (allResults.Exists(x => x.IsError))
                        continue;
                }

                verifiedMods.Add(mod);
            }

            var errorResults = results.FindAll(x => x.IsError);
            if (errorResults.Count > 0)
                return new ResultLoadInfos { Mods = Array.Empty<Mod>(), Results = results.ToArray() };

            List<Mod> sortedMods = new();
            foreach (var result in verifiedMods)
            {
                var i = 0;
                var relations = new List<Mod>();
                GetRelations(result, sortedMods.ToArray(), ref relations);
                for (; i < sortedMods.Count; i++)
                    if (relations.Contains(sortedMods[i]))
                        break;
                sortedMods.Insert(i, result);
            }

            sortedMods.Reverse();
            verifiedMods = sortedMods;

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

            errorResults = results.FindAll(x => x.IsError);
            if (errorResults.Count > 0)
                return new ResultLoadInfos { Mods = Array.Empty<Mod>(), Results = results.ToArray() };

            Mods.AddRange(loadedMods);

            results.AddRange(loadedMods.Select(mod => new ResultLoad
            {
                Type = ResultLoad.ResultType.Success,
                Message = $"Mod {mod.GetMetadata().GetId()}({mod.GetMetadata().GetVersion()}) loaded successfully",
                ForMod = mod.GetMetadata().GetId()
            }));


            return new ResultLoadInfos { Mods = loadedMods.ToArray(), Results = results.ToArray() };
        }
    }

    public class ResultLoadInfos
    {
        public Mod[] Mods;
        public ResultLoad[] Results;

        public ResultLoad[] GetResults(ResultLoad.ResultType flags) =>
            Array.FindAll(Results, x => x.Type.HasFlag(flags));
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
            IsConflict = 4,
            IsBreak = 8,
            NoMetadata = 16,
            AlreadyLoaded = 32,
            LoadError = 64,

            IsSuccess = Success,
            IsWarning = IsConflict,
            IsError = MissingDependency | IsBreak | NoMetadata | AlreadyLoaded | LoadError
        }
    }
}