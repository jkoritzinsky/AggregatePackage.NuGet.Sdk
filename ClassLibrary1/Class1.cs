using Newtonsoft.Json.Linq;
using System;

namespace ClassLibrary1
{
    public class Class1
    {
        private readonly JObject obj;

        public Class1(JObject obj)
        {
            this.obj = obj;
        }
    }
}
