﻿using LibGit2Sharp.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LibGit2Sharp
{
    /// <summary>
    ///   Provides helper overloads to a <see cref = "Network" />.
    /// </summary>
    public static class NetworkExtensions
    {
        /// <summary>
        ///   Push the objectish to the destination reference on the <see cref = "Remote" />.
        /// </summary>
        /// <param name="network">The <see cref="Network"/> being worked with.</param>
        /// <param name="remote">The <see cref = "Remote" /> to push to.</param>
        /// <param name="objectish">The source objectish to push.</param>
        /// <param name="destinationSpec">The reference to update on the remote.</param>
        /// <returns>Results of the push operation.</returns>
        public static PushResult Push(
            this Network network,
            Remote remote,
            string objectish,
            string destinationSpec)
        {
            Ensure.ArgumentNotNull(remote, "remote");
            Ensure.ArgumentNotNull(objectish, "objectish");
            Ensure.ArgumentNotNullOrEmptyString(destinationSpec, "destinationSpec");

            return network.Push(remote, string.Format("{0}:{1}", objectish, destinationSpec));
        }

        /// <summary>
        ///   Push specified reference to the <see cref="Remote"/>.
        /// </summary>
        /// <param name="network">The <see cref="Network"/> being worked with.</param>
        /// <param name="remote">The <see cref = "Remote" /> to push to.</param>
        /// <param name="pushRefSpec">The pushRefSpec to push.</param>
        /// <returns>Results of the push operation.</returns>
        public static PushResult Push(this Network network, Remote remote, string pushRefSpec)
        {
            Ensure.ArgumentNotNull(remote, "remote");
            Ensure.ArgumentNotNullOrEmptyString(pushRefSpec, "pushRefSpec");

            return network.Push(remote, new string[] { pushRefSpec });
        }

        /// <summary>
        ///   Push specified references to the <see cref="Remote"/>.
        /// </summary>
        /// <param name="network">The <see cref="Network"/> being worked with.</param>
        /// <param name="remote">The <see cref="Remote"/> to push to.</param>
        /// <param name="pushRefSpecs">The pushRefSpecs to push.</param>
        /// <returns>Results of the push operation.</returns>
        public static PushResult Push(this Network network, Remote remote, IEnumerable<string> pushRefSpecs)
        {
            Ensure.ArgumentNotNull(remote, "remote");
            Ensure.ArgumentNotNull(pushRefSpecs, "pushRefSpecs");

            List<PushStatusError> failedRemoteUpdates = new List<PushStatusError>();

            network.Push(
                remote,
                pushRefSpecs,
                failedRemoteUpdates.Add);

            return new PushResult(failedRemoteUpdates);
        }
    }
}
