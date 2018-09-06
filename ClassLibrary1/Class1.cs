using Newtonsoft.Json.Linq;
using System;

namespace ClassLibrary1
{
    public class Class1
    {
        private readonly JObject obj;

        public Class1(JObject obj, NonEmbeddedLibrary.Class1 cls)
        {
            this.obj = obj;
        }
    }
}
