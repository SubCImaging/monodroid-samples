// <copyright file="Encryption.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>

namespace SubCTools.Security
{
    using System;
    using System.IO;
    using System.Security.Cryptography;
    using System.Text;

    public static class Encryption
    {
        private static readonly string Salt = "SubCImaging_92c4eadf-1ac6-455d-abf3-14a2ec7d6d88";

        /// <summary>
        /// Computes a SHA256 hash string from a string input.
        /// </summary>
        /// <param name="rawData">The string to hash.</param>
        /// <returns>The hash.</returns>
        public static string ComputeSha256Hash(string rawData)
        {
            // Create a SHA256
            using (var sha256Hash = SHA256.Create())
            {
                // ComputeHash - returns byte array
                var bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));

                // Convert byte array to a string
                var builder = new StringBuilder();
                for (var i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }

                return builder.ToString();
            }
        }

        public static string GenerateKey(DirectoryInfo directory)
        {
            var totalHash = string.Empty;

            totalHash += Salt;

            foreach (var file in directory.GetFiles())
            {
                if (file.Name == "key")
                {
                    continue;
                }

                totalHash += CalculateSHA512(file);
            }

            return CalculateSHA512(totalHash);
        }

        private static string CalculateSHA512(FileInfo file)
        {
            using (var sha5 = SHA512.Create())
            {
                using (var stream = File.OpenRead(file.FullName))
                {
                    var hash = sha5.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", string.Empty).ToLowerInvariant();
                }
            }
        }

        private static string CalculateSHA512(string str)
        {
            using (var sha5 = SHA512.Create())
            {
                var hash = sha5.ComputeHash(Encoding.ASCII.GetBytes(str));
                return BitConverter.ToString(hash).Replace("-", string.Empty).ToLowerInvariant();

            }
        }
    }
}
