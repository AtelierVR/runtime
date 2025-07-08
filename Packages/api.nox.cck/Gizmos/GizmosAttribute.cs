using System.Collections.Generic;

namespace Nox.CCK.Development
{
    public class GizmosAttribute : System.Attribute
    {
        public string[] Tags { get; private set; }
        public GizmosAttribute(params string[] tags)
        {
            List<string> Tags = new();
            foreach (var tag in tags)
                if (string.IsNullOrEmpty(tag) && !Tags.Contains(tag))
                    Tags.Add(tag);
            this.Tags = Tags.ToArray();
        }
    }
}