using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Nox.CCK.Utils;

namespace api.nox.search
{
    public class Result
    {
        public string Error;
        public ResultData[] Data;
        public Func<UniTask<Result>> Next;
        public float Ratio = 1f;

        public static Result From(Dictionary<string, object> data)
        {
            var result = new Result();
            
            if (data.TryGetValue("error", out var error) && error is string err)
                result.Error = err;

            if (data.TryGetValue("data", out var dataArray) && dataArray is Dictionary<string, object>[] dataList)
            {
                result.Data = new ResultData[dataList.Length];
                for (var i = 0; i < dataList.Length; i++)
                    result.Data[i] = ResultData.From(dataList[i]);
            }

            if (data.TryGetValue("next", out var next) && next is Func<UniTask<Result>> nextFunc)
                result.Next = nextFunc;

            if (data.TryGetValue("ratio", out var ratio) && ratio is float r)
                result.Ratio = r;

            return result;
        }
    }
}