
namespace Nox.CCK.Development
{
    /// <summary>
    /// Interface for components that can draw custom gizmos in the scene view.
    /// </summary>
    public interface IGizmos
    {
        /// <summary>
        /// Called when gizmos should be drawn for this component.
        /// </summary>
        void OnDrawGizmos();

        /// <summary>
        /// Gets the gizmos attribute associated with this component's type.
        /// </summary>
        /// <returns>The gizmos attribute, or null if not found.</returns>
        public GizmosAttribute GetGizmosAttribute()
        {
            var type = GetType();
            var attributes = type.GetCustomAttributes(typeof(GizmosAttribute), true);
            if (attributes.Length == 0)
                return null;
            return (GizmosAttribute)attributes[0];
        }
    }
}