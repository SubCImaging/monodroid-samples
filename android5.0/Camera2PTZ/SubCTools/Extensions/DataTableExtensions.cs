// <copyright file="DataTableExtensions.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>

namespace SubCTools.Extensions
{
    using System.Data;
    using System.Threading.Tasks;

    public static class DataTableExtensions
    {
        public static bool TryCompute(this DataTable table, string expression, string filter)
        {
            try
            {
                var result = table.Compute(expression, filter);
                if (bool.TryParse(result.ToString(), out var r))
                {
                    return r;
                }
                else
                {
                    return false;
                }
            }
            catch (SyntaxErrorException)
            {
                throw;
            }
            catch (EvaluateException)
            {
                throw;
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="table"></param>
        /// <param name="expression"></param>
        /// <param name="filter"></param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public static Task<bool> TryComputeAsync(this DataTable table, string expression, string filter)
        {
            return Task.Run(() => TryCompute(table, expression, filter));
        }
    }
}