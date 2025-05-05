using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace api.nox.search
{
    public class Worker
    {
        public string ServerAddress;
        public string ServerTitle;
        public Func<Dictionary<string, object>, UniTask<Result>> Fetch;

        public static Worker From(Dictionary<string, object> workers)
        {
            var worker = new Worker();

            if (workers.TryGetValue("server_address", out var serverAddress) && serverAddress is string sa)
                worker.ServerAddress = sa;

            if (workers.TryGetValue("server_title", out var serverTitle) && serverTitle is string st)
                worker.ServerTitle = st;

            if (workers.TryGetValue("fetch", out var fetch)
                && fetch is Func<Dictionary<string, object>, UniTask<Dictionary<string, object>>> f)
                worker.Fetch = async data => Result.From(await f(data));

            return worker;
        }
    }
}