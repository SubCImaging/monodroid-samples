//-----------------------------------------------------------------------
// <copyright file="RemoteState.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>
// <author>Adam Rowe</author>
//-----------------------------------------------------------------------
namespace SubCTools.Attributes
{
    using System;

    /// <summary>
    /// Defines a remote state, this class extends attribute.
    /// </summary>
    public class RemoteState : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RemoteState" /> class.
        /// </summary>
        public RemoteState()
            : this(string.Empty)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RemoteState" /> class.
        /// </summary>
        /// <param name="prefix"> A prefix of the attribute. </param>
        public RemoteState(string prefix)
        {
            Prefix = prefix;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RemoteState" /> class.
        /// </summary>
        /// <param name="isPrivateSet"> Can the command interpreter access the set of the property. </param>
        public RemoteState(bool isPrivateSet)
            : this(isPrivateSet, false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RemoteState" /> class.
        /// </summary>
        /// <param name="isPrivateSet"> Can the command interpreter access the set of the property. </param>
        /// <param name="isSyncable"> Is the property used to sync multiple cameras states. </param>
        public RemoteState(bool isPrivateSet, bool isSyncable)
        {
            IsPrivateSet = isPrivateSet;
            IsSyncable = isSyncable;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RemoteState" /> class.
        /// </summary>
        /// <param name="isPrivateSet"> Can the command interpreter access the set of the property. </param>
        /// <param name="isSyncable"> Is the property used to sync multiple cameras states. </param>
        public RemoteState(bool isPrivateSet, bool isSyncable, bool hidden = false)
        {
            IsPrivateSet = isPrivateSet;
            IsSyncable = isSyncable;
            Hidden = hidden;
        }

        /// <summary>
        /// Gets a value indicating whether the property is used to sync multiple cameras.
        /// </summary>
        public bool Hidden { get; } = false;

        /// <summary>
        /// Gets a value indicating whether the property setter "should" be private, and inaccessible.
        /// </summary>
        public bool IsPrivateSet { get; }

        // An update to .NET prevents object from executing private set from outside the class, need
        // a way to prevent the command interpreter from trying to access it
        /// <summary>
        /// Gets a value indicating whether the property is used to sync multiple cameras.
        /// </summary>
        public bool IsSyncable { get; } = false;

        /// <summary>
        /// Gets the prefix defined in this remote state.
        /// </summary>
        public string Prefix { get; }
    }
}