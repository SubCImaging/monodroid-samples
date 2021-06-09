//-----------------------------------------------------------------------
// <copyright file="IHandlerThread.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>
// <author>Adam Rowe</author>
//-----------------------------------------------------------------------
namespace SubCTools.Droid.Interfaces
{
    public interface IHandlerThread
    {
        ILooper Looper { get; }

        void Start();
    }
}