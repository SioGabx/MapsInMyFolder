using Jint.Native;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapsInMyFolder.Core.Generic.Extensions
{
    public static class JArrayExtensions
    {
        public static JToken TryGet(this JArray jarray, int Index)
        {
            try
            {
                if (jarray.HasValues)
                {
                    return jarray[Index];
                }
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                return null;
            }
        }
        public static JToken TryGet(this JToken jtoken, int Index)
        {
            try
            {
                if (jtoken is JArray jarray && jarray.HasValues)
                {
                    return jarray[Index];
                }
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                return null;
            }
        }
    }
}
