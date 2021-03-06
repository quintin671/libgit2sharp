﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LibGit2Sharp.Core;
using LibGit2Sharp.Core.Handles;
using LibGit2Sharp.Handlers;

namespace LibGit2Sharp
{
    /// <summary>
    ///   Provides access to network functionality for a repository.
    /// </summary>
    public class Network
    {
        private readonly Repository repository;

        /// <summary>
        ///   Needed for mocking purposes.
        /// </summary>
        protected Network()
        { }

        internal Network(Repository repository)
        {
            this.repository = repository;
        }

        /// <summary>
        ///   The heads that have been updated during the last fetch.
        /// </summary>
        public virtual IEnumerable<FetchHead> FetchHeads
        {
            get
            {
                int i = 0;

                return Proxy.git_repository_fetchhead_foreach(
                    repository.Handle,
                    (name, url, oid, isMerge) => new FetchHead(repository, name, url, new ObjectId(oid), isMerge, i++));
            }
        }

        /// <summary>
        ///   Push the objectish to the destination reference on the <see cref = "Remote" />.
        /// </summary>
        /// <param name="remote">The <see cref = "Remote" /> to push to.</param>
        /// <param name="objectish">The source objectish to push.</param>
        /// <param name="destinationSpec">The reference to update on the remote.</param>
        /// <param name="onPushStatusError">Handler for reporting failed push updates.</param>
        public virtual void Push(
            Remote remote,
            string objectish,
            string destinationSpec,
            PushStatusErrorHandler onPushStatusError)
        {
            Ensure.ArgumentNotNull(remote, "remote");
            Ensure.ArgumentNotNull(objectish, "objectish");
            Ensure.ArgumentNotNullOrEmptyString(destinationSpec, destinationSpec);

            Push(remote, string.Format("{0}:{1}", objectish, destinationSpec), onPushStatusError);
        }

        /// <summary>
        ///   Push specified reference to the <see cref="Remote"/>.
        /// </summary>
        /// <param name="remote">The <see cref = "Remote" /> to push to.</param>
        /// <param name="pushRefSpec">The pushRefSpec to push.</param>
        /// <param name="onPushStatusError">Handler for reporting failed push updates.</param>
        public virtual void Push(
            Remote remote,
            string pushRefSpec,
            PushStatusErrorHandler onPushStatusError)
        {
            Ensure.ArgumentNotNull(remote, "remote");
            Ensure.ArgumentNotNullOrEmptyString(pushRefSpec, "pushRefSpec");

            Push(remote, new string[] { pushRefSpec }, onPushStatusError);
        }

        /// <summary>
        ///   Push specified references to the <see cref="Remote"/>.
        /// </summary>
        /// <param name="remote">The <see cref = "Remote" /> to push to.</param>
        /// <param name="pushRefSpecs">The pushRefSpecs to push.</param>
        /// <param name="onPushStatusError">Handler for reporting failed push updates.</param>
        public virtual void Push(
            Remote remote,
            IEnumerable<string> pushRefSpecs,
            PushStatusErrorHandler onPushStatusError)
        {
            Ensure.ArgumentNotNull(remote, "remote");
            Ensure.ArgumentNotNull(pushRefSpecs, "pushRefSpecs");

            // Return early if there is nothing to push.
            if (!pushRefSpecs.Any())
            {
                return;
            }

            PushCallbacks pushStatusUpdates = new PushCallbacks(onPushStatusError);

            // Load the remote.
            using (RemoteSafeHandle remoteHandle = Proxy.git_remote_load(repository.Handle, remote.Name, true))
            {
                try
                {
                    Proxy.git_remote_connect(remoteHandle, GitDirection.Push);

                    // Perform the actual push.
                    using (PushSafeHandle pushHandle = Proxy.git_push_new(remoteHandle))
                    {
                        // Add refspecs.
                        foreach (string pushRefSpec in pushRefSpecs)
                        {
                            Proxy.git_push_add_refspec(pushHandle, pushRefSpec);
                        }

                        Proxy.git_push_finish(pushHandle);

                        if (!Proxy.git_push_unpack_ok(pushHandle))
                        {
                            throw new LibGit2SharpException("Push failed - remote did not successfully unpack.");
                        }

                        Proxy.git_push_status_foreach(pushHandle, pushStatusUpdates.Callback);

                        Proxy.git_push_update_tips(pushHandle);
                    }
                }
                finally
                {
                    Proxy.git_remote_disconnect(remoteHandle);
                }
            }
        }

        /// <summary>
        ///   Helper class to handle callbacks during push.
        /// </summary>
        private class PushCallbacks
        {
            PushStatusErrorHandler OnError;

            public PushCallbacks(PushStatusErrorHandler onError)
            {
                OnError = onError;
            }

            public int Callback(IntPtr referenceNamePtr, IntPtr msgPtr, IntPtr payload)
            {
                // Exit early if there is no callback.
                if (OnError == null)
                {
                    return 0;
                }

                // The reference name pointer should never be null - if it is,
                // this indicates a bug somewhere (libgit2, server, etc).
                if (referenceNamePtr == IntPtr.Zero)
                {
                    Proxy.giterr_set_str(GitErrorCategory.Invalid, "Not expecting null for reference name in push status.");
                    return -1;
                }

                // Only report updates where there is a message - indicating
                // that there was an error.
                if (msgPtr != IntPtr.Zero)
                {
                    string referenceName = Utf8Marshaler.FromNative(referenceNamePtr);
                    string msg = Utf8Marshaler.FromNative(msgPtr);
                    OnError(new PushStatusError(referenceName, msg));
                }

                return 0;
            }
        }
    }
}
