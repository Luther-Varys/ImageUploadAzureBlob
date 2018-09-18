using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageUploadAzureBlob.Helpers
{
    internal class ProjBasicHelper
    {

        internal static string GetTimestamp(DateTime value)
        {
            return value.ToString("yyyyMMddHHmmssffff");
        }


        internal static string GenrateGuidString()
        {
            string guidString = Guid.NewGuid().ToString();
            return guidString;
        }


        internal static Guid GenrateGuid()
        {
            Guid guid = Guid.NewGuid();
            return guid;
        }

        internal static bool IsStringNullOrEmptyOrWhitespace(string str)
        {
            if (string.IsNullOrEmpty(str) || string.IsNullOrWhiteSpace(str))
                return true;


            return false;
        }


    }


}
