using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using VContainer;

namespace vz777.VContainer
{
	public static class VContainerExtensions
	{
		/// <summary>
		/// A extension function to mark the type as non lazy.
		/// </summary>
		public static RegistrationBuilder NonLazy(this RegistrationBuilder builder, IContainerBuilder containerBuilder, bool resolveInterfaces = false)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));
            if (containerBuilder == null)
                throw new ArgumentNullException(nameof(containerBuilder));

            // Access ImplementationType field via reflection
            var implementationTypeField = typeof(RegistrationBuilder)
                .GetField("ImplementationType", BindingFlags.NonPublic | BindingFlags.Instance);
            if (implementationTypeField == null)
                throw new VContainerException(null, "Cannot access ImplementationType field via reflection.");

            if (implementationTypeField.GetValue(builder) is not Type implementationType)
                throw new VContainerException(null, "No ImplementationType found for the registration.");

            // Access InterfaceTypes field via reflection if requested
            var interfaceTypes = new List<Type>();
            if (resolveInterfaces)
            {
                var interfaceTypesField = typeof(RegistrationBuilder)
                    .GetField("InterfaceTypes", BindingFlags.NonPublic | BindingFlags.Instance);
                if (interfaceTypesField != null)
                {
                    interfaceTypes = interfaceTypesField.GetValue(builder) as List<Type> ?? new List<Type>();
                }
            }

            // Register build callback to resolve types
            containerBuilder.RegisterBuildCallback(container =>
            {
                try
                {
                    // Resolve implementation type
                    container.Resolve(implementationType);

                    // Resolve interface types if requested
                    foreach (var interfaceType in interfaceTypes)
                    {
                        container.Resolve(interfaceType);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to resolve types: {ex.Message}");
                }
            });

            return builder;
        }
	}
}