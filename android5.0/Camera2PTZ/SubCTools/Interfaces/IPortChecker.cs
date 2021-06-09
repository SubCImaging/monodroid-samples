namespace SubCTools.Interfaces
{
    using System.Net;

    /// <summary>
    /// An interface to check ports and their availability.
    /// </summary>
    public interface IPortChecker
    {
        /// <summary>
        /// Check to see if a port is open at a given address.
        /// </summary>
        /// <param name="address">Address to connect.</param>
        /// <param name="port">Port to connect.</param>
        /// <returns>True if port is open, false otherwise.</returns>
        public bool IsPortOpen(IPAddress address, int port);
    }
}
