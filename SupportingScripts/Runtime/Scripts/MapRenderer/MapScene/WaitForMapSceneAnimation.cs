namespace Microsoft.Maps.Unity
{
    using System.Reflection;
    using UnityEngine;

    /// <summary>
    /// Used to suspend coroutine execution once the associated MapScene animation has been completed or cancelled.
    /// </summary>
    public class WaitForMapSceneAnimation : CustomYieldInstruction
    {
        private bool _keepWaiting = true;

        /// <summary>
        /// Returns false once the animation has been completed or cancelled.
        /// </summary>
        public override bool keepWaiting => _keepWaiting;

        /// <summary>
        /// Constructs the yieldable instance.
        /// </summary>
        public WaitForMapSceneAnimation(bool isCompleted = false)
        {
            _keepWaiting = !isCompleted;
        }

        /// <summary>
        /// Completes the yield instruction.
        /// </summary>
        public void SetComplete()
        {
            _keepWaiting = false;
        }
    }
}
