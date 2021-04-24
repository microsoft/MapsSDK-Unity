namespace Microsoft.Maps.Unity
{
    /// <summary>
    /// Specifies the animation to use when setting a MapScene.
    /// </summary>
    public enum  MapSceneAnimationKind
    {
        /// <summary>
        /// No animation.
        /// </summary>
        None,

        /// <summary>
        /// A linear animation.
        /// </summary>
        Linear,

        /// <summary>
        /// A linear animation with smoothing applied to the beginning and end of the animation.
        /// </summary>
        SmoothLinear,

        /// <summary>
        /// A parabolic animation.
        /// </summary>
        Bow
    }
}
