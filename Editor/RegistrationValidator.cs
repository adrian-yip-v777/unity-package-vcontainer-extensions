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
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("Validating the scene only works when the editor is not running.");
                return;
            }
            
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            
            // Step 1: Find all active lifetime scopes in the scene
            var monoBehaviours = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var scopes = monoBehaviours
                .Where (mb => mb is LifetimeScope { isActiveAndEnabled: true })
                .Cast<LifetimeScope>()
                .ToArray();

            if (scopes.Length == 0)
                throw new InvalidRegistrationException("No LifetimeScope found in the scene.", stopWatch.Elapsed);
            
            // Step 2: Build containers for all LifetimeScopes
            // Build the scope will handle the registration as well as the parent-child relationship.
            // It will validate all registrations and the gameObjects on the auto-injected list.
            var lifetimeScopeType = typeof(LifetimeScope);
            var waitingListFieldInfo = lifetimeScopeType.GetField("WaitingList", BindingFlags.Static | BindingFlags.NonPublic);
            var enqueueAwakeMethod = lifetimeScopeType.GetMethod("EnqueueAwake", BindingFlags.Static | BindingFlags.NonPublic);
            var parentPropertyInfo = lifetimeScopeType.GetProperty("Parent", BindingFlags.Instance | BindingFlags.Public);
            
            if (waitingListFieldInfo == null || enqueueAwakeMethod == null || parentPropertyInfo == null)
                throw new InvalidRegistrationException("The WaitingList field, EnqueueAwake method or Parent property was not found. VContainer might have changed their code.", stopWatch.Elapsed);
            
            // Initialize the waiting list to make sure it's not null.
            waitingListFieldInfo.SetValue(null, new List<LifetimeScope>());
            
            // Get the waiting list instance for later use.
            var waitingList = waitingListFieldInfo.GetValue(null) as List<LifetimeScope>;

            foreach (var scope in scopes)
            {
                // Mocking the awake function of a lifetime scope.
                try
                {
                    scope.Build();
                }
                catch (VContainerParentTypeReferenceNotFound) when (!scope.IsRoot)
                {
                    if (waitingList.Contains(scope))
                        throw;

                    enqueueAwakeMethod.Invoke(null, new object[] { scope });
                }
            }

            try
            {
                // Step 3: Finding any gameObjects that injects dependencies but not registered in the auto-injected list.
                var autoInjectGameObjectsFieldInfo = lifetimeScopeType.GetField("autoInjectGameObjects",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                if (autoInjectGameObjectsFieldInfo == null)
                    throw new InvalidRegistrationException(
                        "The autoInjectGameObjects field was not found. VContainer might have changed their code.",
                        stopWatch.Elapsed);

                var candidates = monoBehaviours
                    .Where(mb => mb
                        .GetType()
                        .GetMembers(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                        .Any(fieldInfo => fieldInfo.GetCustomAttributes(typeof(InjectAttribute), true).Length > 0)
                    )
                    .Select(mb => mb.gameObject)
                    .Distinct();

                // The list of auto-injected GameObjects on all lifetime scope.
                var registry = scopes
                    .SelectMany(scope => autoInjectGameObjectsFieldInfo.GetValue(scope) as List<GameObject>).Distinct();

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
            finally
            {
                // Step 4: Reset the runtime reference of parent to null.
                foreach (var scope in scopes)
                    parentPropertyInfo.SetValue(scope, null);
            
                waitingListFieldInfo.SetValue(null, null);
            }
        }
        
        /// <summary>
        /// Press alt(option) + shift + R to validate the scene and play the editor.
        /// </summary>
        [MenuItem("Tools/VContainer/Validate and Play &#C")]
        public static void ValidateAndPlay()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("Validating the scene only works when the editor is not running.");
                return;
            }
            
            ValidateScene();
            EditorApplication.isPlaying = true;
        }
    }
}