using System;

namespace SubSolution.Raw
{
    static public class RawGuid
    {
        static public string ToRawFormat(this Guid guid) => '{' + guid.ToString().ToUpper() + '}';
        static public bool TryParse(string rawGuid, out Guid guid)
        {
            if (!rawGuid.StartsWith('{') || !rawGuid.EndsWith('}'))
            {
                guid = default(Guid);
                return false;
            }

            return Guid.TryParse(rawGuid[1..^1], out guid);
        }
    }
}