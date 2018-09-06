using System;

namespace ReferencingLibrary
{
    public class Class1
    {
        private readonly ClassLibrary1.Class1 obj;
        private readonly ClassLibrary2.Class1 obj2;

        public Class1(ClassLibrary1.Class1 obj, ClassLibrary2.Class1 obj2, NonEmbeddedLibrary.Class1 obj3)
        {
            this.obj = obj;
            this.obj2 = obj2;
        }
    }
}
