using System;

namespace Nox.CCK
{
    public class VersionMatching
    {

        private Version _version;
        private Type _matchingType;

        public VersionMatching(string version)
        {
            // This is a placeholder for the version matching system
            // [>=|<=|==|!=|>|<][version]

            var type = Type.EqualTo;
            var versionpart = version;
            if (version.StartsWith(">="))
            {
                type = Type.GreaterThanOrEqualTo;
                versionpart = version.Substring(2);
            }
            else if (version.StartsWith("<="))
            {
                type = Type.LessThanOrEqualTo;
                versionpart = version.Substring(2);
            }
            else if (version.StartsWith("=="))
            {
                type = Type.EqualTo;
                versionpart = version.Substring(2);
            }
            else if (version.StartsWith("!="))
            {
                type = Type.NotEqualTo;
                versionpart = version.Substring(2);
            }
            else if (version.StartsWith(">"))
            {
                type = Type.GreaterThan;
                versionpart = version.Substring(1);
            }
            else if (version.StartsWith("<"))
            {
                type = Type.LessThan;
                versionpart = version.Substring(1);
            }

            _version = new Version(versionpart);
            _matchingType = type;
        }

        public bool Matches(Version version) => _matchingType switch
        {
            Type.GreaterThan => version > _version,
            Type.LessThan => version < _version,
            Type.EqualTo => version == _version,
            Type.NotEqualTo => version != _version,
            Type.GreaterThanOrEqualTo => version >= _version,
            Type.LessThanOrEqualTo => version <= _version,
            _ => false,
        };

        public Type GetMatchingType() => _matchingType;
        public Version GetVersion() => _version;

        public enum Type
        {
            GreaterThan,
            LessThan,
            EqualTo,
            NotEqualTo,
            GreaterThanOrEqualTo,
            LessThanOrEqualTo
        }
    }
}