using System;
using System.Collections.Generic;
using System.Linq;
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

            var implementationType = implementationTypeField.GetValue(builder) as Type;
            if (implementationType == null)
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

            // Register build callback to resolve or instantiate types
            containerBuilder.RegisterBuildCallback(container =>
            {
                try
                {
                    object implementationInstance;

                    // Try resolving the implementation type
                    try
                    {
                        implementationInstance = container.Resolve(implementationType);
                    }
                    catch (VContainerException)
                    {
                        try
                        {
                            // Get all public constructors
                            var constructors = implementationType.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
                            if (constructors.Length == 0)
                                throw new VContainerException(implementationType, $"No public constructors found for {implementationType.Name}.");

                            // Find a constructor with the most resolvable parameters
                            ConstructorInfo selectedConstructor = null;
                            object[] parameterInstances = null;
                            var maxResolvableParameters = -1;

                            foreach (var constructor in constructors.OrderByDescending(c => c.GetParameters().Length))
                            {
                                var parameters = constructor.GetParameters();
                                var tempParameterInstances = new object[parameters.Length];
                                var allParametersResolved = true;

                                for (var i = 0; i < parameters.Length; i++)
                                {
                                    try
                                    {
                                        tempParameterInstances[i] = container.Resolve(parameters[i].ParameterType);
                                    }
                                    catch (VContainerException)
                                    {
                                        allParametersResolved = false;
                                        break;
                                    }
                                }

                                if (allParametersResolved && parameters.Length > maxResolvableParameters)
                                {
                                    selectedConstructor = constructor;
                                    parameterInstances = tempParameterInstances;
                                    maxResolvableParameters = parameters.Length;
                                }
                            }

                            if (selectedConstructor == null)
                                throw new VContainerException(implementationType, $"No constructor with resolvable parameters found for {implementationType.Name}.");

                            // Instantiate with resolved parameters
                            implementationInstance = selectedConstructor.Invoke(parameterInstances);
                            container.Inject(implementationInstance);
                        }
                        catch (Exception instEx)
                        {
                            Debug.LogError($"Failed to instantiate or inject {implementationType.Name}: {instEx.Message}");
                        }
                    }

                    // Try resolving interface types if requested
                    if (resolveInterfaces)
                    {
                        foreach (var interfaceType in interfaceTypes)
                        {
                            try
                            {
                                container.Resolve(interfaceType);
                            }
                            catch (VContainerException ex)
                            {
                                Debug.LogWarning($"Failed to resolve interface {interfaceType.Name}: {ex.Message}. Interface should be satisfied by implementation instance.");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Unexpected error in NonLazy resolution: {ex.Message}");
                }
            });

            return builder;
        }
	}
}