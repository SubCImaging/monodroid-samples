namespace SubCTools.Helpers.TestHelpers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    /// <summary>
    /// A utility class for testing the public settable Properties of an object.
    /// </summary>
    public class PropertyTester
    {
        private readonly object testee;
        private readonly IEnumerable<Delegate> factories;

        /// <summary>
        /// Initializes a new instance of the <see cref="PropertyTester"/> class.
        /// </summary>
        /// <param name="testee">The object to test.</param>
        /// <param name="factories">A collection of types and factory funcs. Any property of a matching will be set to the value of said func during testing.</param>
        public PropertyTester(object testee, IEnumerable<Delegate> factories)
        {
            this.testee = testee;
            this.factories = factories;
        }

        /// <summary>
        /// Tests all public settable Properties of the testee object against the factories provided.
        /// </summary>
        /// <returns>True if all properties were tested successfully, false otherwise.</returns>
        public bool TestAllPublicSetProperties()
        {
            if (testee == null
                || factories == null
                || factories.Count() < 1)
            {
                return false;
            }

            var type = testee.GetType();
            var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);

            foreach (var property in properties)
            {
                // If not writable then cannot null it; if not readable then cannot check it's value
                if (!property.CanWrite || !property.CanRead)
                {
                    continue;
                }

                var get = property.GetGetMethod(false);
                var set = property.GetSetMethod(false);

                // Get and set methods have to be public
                if (get == null
                || set == null)
                {
                    continue;
                }

                foreach (var factory in factories)
                {
                    if (factory.Method.ReturnType == property.PropertyType)
                    {
                        var value = factory.DynamicInvoke();
                        property.SetValue(testee, value);
                        var testValue = property.GetValue(testee);
                        Console.WriteLine($"Expected {value}, Actual {testValue}");
                        if (!value.Equals(testValue))
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }
    }
}
