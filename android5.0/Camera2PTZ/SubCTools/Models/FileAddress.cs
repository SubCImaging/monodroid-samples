// <copyright file="FileAddress.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>

namespace SubCTools.Models
{
    public class FileAddress : CommunicatorAddress
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FileAddress"/> class.
        /// </summary>
        /// <param name="path"></param>
        public FileAddress(string path)
        {
            FilePath = path;
            Add(nameof(FilePath), path);
        }

        public string FilePath { get; } = string.Empty;

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"File Location:{FilePath}";
        }
    }
}
