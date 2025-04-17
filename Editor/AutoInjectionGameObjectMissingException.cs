using System;
using System.Collections.Generic;
using System.Linq;
using vz777.Foundations;
using UnityEngine;

namespace vz777.VContainers.Editor
{
    public class AutoInjectionGameObjectMissingException : InvalidRegistrationException
    {
        public AutoInjectionGameObjectMissingException(IReadOnlyCollection<GameObject> missingGameObjects, TimeSpan elapsed) : base(elapsed)
        {
            var listToDisplay = string.Join("\n\n", missingGameObjects.Select((go, index) => $"[{index + 1}] <color=yellow>{go.name}</color>\nPath: {go.transform.GetScenePath()}"));
            message = $"({ElapsedTime:N} seconds)\nThe following gameObjects have [Inject] attributes but not registered on any auto-injected list of any lifetime scope:\n{listToDisplay}";
            MissingGameObjects = missingGameObjects;
        }

        /// <summary>
        /// The list of gameObjects that are missing from any auto-injected gameObjects list of any lifetime scope.
        /// </summary>
        private IReadOnlyCollection<GameObject> MissingGameObjects { get; }
    }
}