using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Migration.Connectors.Targets.Aprimo.Utilities
{
    public static class BytesConverter
    {
        public static double ConvertBytesToKB(long bytes)
        {
            return bytes / 1024f;
        }

        public static double ConvertBytesToMB(long bytes)
        {
            return ConvertBytesToKB(bytes) / 1024f;
        }

        public static double ConvertBytesToGB(long bytes)
        {
            return ConvertBytesToMB(bytes) / 1024f;
        }

        public static double ConvertBytesToTB(long bytes)
        {
            return ConvertBytesToGB(bytes) / 1024f;
        }

        public static double ConvertBytesToPB(long bytes)
        {
            return ConvertBytesToTB(bytes) / 1024f;
        }

        public static string BytesToHuman(long size)
        {
            long Kb = 1 * 1024;
            long Mb = Kb * 1024;
            long Gb = Mb * 1024;
            long Tb = Gb * 1024;
            long Pb = Tb * 1024;
            long Eb = Pb * 1024;

            if (size == 0) return "0 Mb";
            if (size < Kb) return FloatForm(size) + " byte";
            if (size >= Kb && size < Mb) return FloatForm((double)size / Kb) + " Kb";
            if (size >= Mb && size < Gb) return FloatForm((double)size / Mb) + " Mb";
            if (size >= Gb && size < Tb) return FloatForm((double)size / Gb) + " Gb";
            if (size >= Tb && size < Pb) return FloatForm((double)size / Tb) + " Tb";
            if (size >= Pb && size < Eb) return FloatForm((double)size / Pb) + " Pb";
            if (size >= Eb) return FloatForm((double)size / Eb) + " Eb";

            return size.ToString();
        }

        public static string FloatForm(double d)
        {
            return Math.Round(d, 2).ToString("##.##");
        }
    }
}
