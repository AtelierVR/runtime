using System;
using System.Collections.Generic;

namespace api.nox.search
{

    public class ResultData
    {
        public int Id = Guid.NewGuid().GetHashCode();
        public string Title;
        public string ImageUrl;
        public string GotoId;
        public object[] GotoData;

        public static ResultData From(Dictionary<string, object> data)
        {
            var resultData = new ResultData();
            
            if (data.TryGetValue("id", out var index) && index is int i)
                resultData.Id = i;

            if (data.TryGetValue("title", out var title) && title is string t)
                resultData.Title = t;

            if (data.TryGetValue("image_url", out var imageUrl) && imageUrl is string imgUrl)
                resultData.ImageUrl = imgUrl;

            if (data.TryGetValue("goto_id", out var gotoId) && gotoId is string gId)
                resultData.GotoId = gId;

            if (data.TryGetValue("goto_data", out var gotoData) && gotoData is object[] gData)
                resultData.GotoData = gData;

            return resultData;
        }
    }
}