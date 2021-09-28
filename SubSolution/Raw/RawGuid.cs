using System;

namespace SubSolution.Raw
{
    static public class RawGuid
    {
        static public readonly Guid Folder = Guid.Parse("2150E333-8FDC-42A3-9474-1A3956D46DE8");
        static public readonly Guid CSharp = Guid.Parse("FAE04EC0-301F-11D3-BF4B-00C04F79EFBC");

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