using UnityEngine;
using VContainer;

namespace vz777.VContainer
{
    public abstract class MonoInstaller : MonoBehaviour, IInstaller
    {
        public abstract void Install(IContainerBuilder builder);
    }
}