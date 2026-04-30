using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Migration.Connectors.Sources.Aem.Files
{
    public static class TextQuality
    {
        public static bool ContainsBadCharacters(
            string? value,
            bool allowTabs = true,
            bool allowNewLines = true)
        {
            if (string.IsNullOrEmpty(value))
                return false;

            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];

                // 1) Unicode replacement char: �
                if (c == '\uFFFD')
                    return true;

                // 2) Unpaired surrogate (corrupt UTF-16)
                if (char.IsSurrogate(c))
                {
                    if (char.IsHighSurrogate(c))
                    {
                        // must be followed by a low surrogate
                        if (i + 1 >= value.Length || !char.IsLowSurrogate(value[i + 1]))
                            return true;

                        i++; // skip the low surrogate
                        continue;
                    }

                    // low surrogate without preceding high surrogate
                    return true;
                }

                // 3) Control characters (often break CSV/Excel/URLs)
                if (char.IsControl(c))
                {
                    if (allowTabs && c == '\t') continue;
                    if (allowNewLines && (c == '\r' || c == '\n')) continue;

                    return true;
                }
            }

            return false;
        }
    }

}
