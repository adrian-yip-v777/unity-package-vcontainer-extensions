using UnityEngine;
using VContainer;

namespace vz777.VContainer
{
    public abstract class ScriptableObjectInstaller : ScriptableObject, IInstaller
    {
        public abstract void Installer(IContainerBuilder builder);
    }
}