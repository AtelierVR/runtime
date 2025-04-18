using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace api.nox.search
{
    public class Result
    {
        public string Error;
        public ResultData[] Data;
        public Func<UniTask<Result>> Next;

        public static Result From(Dictionary<string, object> data)
        {
            var result = new Result();

            if (data.TryGetValue("error", out var error) && error is string err)
                result.Error = err;

            if (data.TryGetValue("data", out var dataArray) && dataArray is List<object> dataList)
            {
                result.Data = new ResultData[dataList.Count];
                for (var i = 0; i < dataList.Count; i++)
                    if (dataList[i] is Dictionary<string, object> itemData)
                        result.Data[i] = ResultData.From(itemData);
            }

            if (data.TryGetValue("next", out var next) && next is Func<UniTask<Result>> nextFunc)
                result.Next = nextFunc;

            return result;
        }
    }
}