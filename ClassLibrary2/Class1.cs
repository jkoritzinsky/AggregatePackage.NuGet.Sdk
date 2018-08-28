using Newtonsoft.Json.Linq;
using System;
using System.Reactive.Subjects;

namespace ClassLibrary2
{
    public class Class1
    {
        private readonly Subject<JObject> subject;

        public Class1(Subject<JObject> subject)
        {
            this.subject = subject;
        }
    }
}
