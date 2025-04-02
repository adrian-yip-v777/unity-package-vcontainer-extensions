using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace vz777.VContainers.Editor
{
    public class AutoInjectionGameObjectMissingException : InvalidRegistrationException
    {
        public AutoInjectionGameObjectMissingException(IReadOnlyCollection<GameObject> missingGameObjects, TimeSpan elapsed) : base(elapsed)
        {
            var listToDisplay = string.Join("\n\n", missingGameObjects.Select((go, index) => $"[{index + 1}] <color=cyan>{go.name}</color>\nPath: <color=yellow>{GetScenePath(go.transform)}</color>"));
            message = $"({ElapsedTime:N} seconds)\nThe following gameObjects have [Inject] attributes but not registered on any auto-injected list of any lifetime scope:\n{listToDisplay}";
            MissingGameObjects = missingGameObjects;
        }

        /// <summary>
        /// The list of gameObjects that are missing from any auto-injected gameObjects list of any lifetime scope.
        /// </summary>
        private IReadOnlyCollection<GameObject> MissingGameObjects { get; }
        
        private string GetScenePath(Transform transform)
        {
            var current = transform;
            var inScenePath = new List<string> { current.name };
            while (current != transform.root)
            {
                current = current.parent;
                inScenePath.Add(current.name);
            }
            
            var builder = new StringBuilder();
            foreach (var item in Enumerable.Reverse(inScenePath)) 
                builder.Append($"\\{item}");
            
            return builder.ToString().TrimStart('\\');
        }
    }
}