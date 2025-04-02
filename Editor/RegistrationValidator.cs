using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using Debug = UnityEngine.Debug;

namespace vz777.VContainers.Editor
{
    public class RegistrationValidator : EditorWindow
    {
        /// <summary>
        /// Press alt(option) + shift + V to validate the scene
        /// </summary>
        /// <exception cref="InvalidRegistrationException">General invalid exception</exception>
        /// <exception cref="AutoInjectionGameObjectMissingException">Missing elements in auto-injected GameObject list</exception>
        [MenuItem("Tools/VContainer/Validate Scene &#V")]
        public static void ValidateScene()
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            var monoBehaviours = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            
            // Step 1: Find all active lifetime scopes in the scene
            var lifetimeScopes = monoBehaviours
                .Where (mb => mb is LifetimeScope { isActiveAndEnabled: true })
                .Cast<LifetimeScope>()
                .ToArray();

            if (lifetimeScopes.Length == 0)
                throw new InvalidRegistrationException("No LifetimeScope found in the scene.", stopWatch.Elapsed);

            // Step 2: Build containers for all LifetimeScopes
            // Build the scope will handle the registration as well as the parent-child relationship.
            // It will validate all registrations and the gameObjects on the auto-injected list.
            foreach (var scope in lifetimeScopes)
                scope.Build();

            // Step 3: Finding any gameObjects that injects dependencies but not registered in the auto-injected list.
            var autoInjectGameObjectsFieldInfo = typeof(LifetimeScope).GetField("autoInjectGameObjects", BindingFlags.Instance | BindingFlags.NonPublic);
            if (autoInjectGameObjectsFieldInfo == null)
                throw new InvalidRegistrationException("The autoInjectGameObjects field was not found. VContainer might have changed their code.", stopWatch.Elapsed);

            var candidates = monoBehaviours
                .Where(mb => mb
                    .GetType()
                    .GetMembers(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .Any(fieldInfo => fieldInfo.GetCustomAttributes(typeof(InjectAttribute), true).Length > 0)
                )
                .Select(mb => mb.gameObject)
                .Distinct();
            
            // The list of auto-injected GameObjects on all lifetime scope.
            var registry = lifetimeScopes.SelectMany(scope => autoInjectGameObjectsFieldInfo.GetValue(scope) as List<GameObject>).Distinct();
            
            var missingGameObjects = candidates
                .Where(candidate => !registry
                    .FirstOrDefault(registeredGameObject =>
                        // Check if the candidate is on the list
                        candidate.Equals(registeredGameObject) ||
                        
                        // Or it's not on the list, but it's a child of a registered gameObject
                        candidate.transform.IsChildOf(registeredGameObject.transform)
                    )).ToArray();
            
            if (missingGameObjects.Length > 0)
                throw new AutoInjectionGameObjectMissingException(missingGameObjects, stopWatch.Elapsed);   

            stopWatch.Stop();
            Debug.Log($"VContainer validation completed successfully. ({stopWatch.Elapsed.TotalSeconds} seconds)");
        }
        
        /// <summary>
        /// Press alt(option) + shift + R to validate the scene and play the editor.
        /// </summary>
        [MenuItem("Tools/VContainer/Validate and Play &#R")]
        public static void ValidateAndPlay()
        {
            ValidateScene();
            EditorApplication.isPlaying = true;
        }
    }
}