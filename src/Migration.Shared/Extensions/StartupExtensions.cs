using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OfficeOpenXml;

namespace Migration.Shared.Extensions
{
    public static class StartupExtensions
    {
        public static void ConfigureThirdPartyLicenses()
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }
    }
}
