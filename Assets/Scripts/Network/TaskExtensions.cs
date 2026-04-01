using System.Threading.Tasks;
using UnityEngine;

namespace VoidRogues.Network
{
    /// <summary>
    /// Extension helpers for <see cref="Task"/> in a Unity/Fusion context.
    /// </summary>
    public static class TaskExtensions
    {
        /// <summary>
        /// Fire-and-forget wrapper that logs unobserved exceptions to the Unity console
        /// instead of silently swallowing them.
        /// </summary>
        public static async void Forget(this Task task)
        {
            try
            {
                await task;
            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
            }
        }
    }
}
