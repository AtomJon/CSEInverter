using CSEInverter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Reflection;

namespace ApplicationTests
{
    [TestClass]
    public class IProductTaskTests
    {
        [TestMethod]
        public void DoesEveryDescendantHaveParameterlessConstructor()
        {
            Type taskInterface = typeof(IProductTask);

            foreach (var type in Assembly.GetAssembly(taskInterface).GetTypes())
            {
                if (type.IsClass && !type.IsAbstract && type.GetInterfaces().Contains(taskInterface))
                {
                    var constructor = type.GetConstructor(Type.EmptyTypes);

                    Assert.IsNotNull(constructor, $"Task: {type} does not have a parameterless constructor");
                }
            }
        }
    }
}
