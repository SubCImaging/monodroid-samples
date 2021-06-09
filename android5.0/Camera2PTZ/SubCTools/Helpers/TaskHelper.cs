// <copyright file="TaskHelper.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>

namespace SubCTools.Helpers
{
    using System;
    using System.Threading.Tasks;

    public static class TaskHelper
    {
        // NOT WORKING
        //        public static TResult TimeoutTask<TResult>(Task<TResult> task, TimeSpan timeout) 
        //        {
        //            using (var timeoutCancellationTokenSource = new CancellationTokenSource()) 
        //            {
        //                try
        //                {
        //                    timeoutCancellationTokenSource.CancelAfter(timeout);
        //                    Task.Run()
        //                    
        //                    return task(timeoutCancellationTokenSource.Token).Result;
        //                }
        //                catch (Exception e)
        //                {
        //                    throw new TimeoutException();
        //                }
        //            }
        //        }

        /// <summary>
        /// Times out a given <see cref="Task"/> after a period of time.
        /// </summary>
        /// <param name="task">The task to timeout.</param>
        /// <param name="timeSpan">The period of time before timing out the task.</param>
        /// <exception cref="TimeoutException">The exception that is thrown as a result of a timeout.</exception>
        public static void TimeoutTask(Task task, TimeSpan timeSpan)
        {
            if (!task.Wait(timeSpan))
            {
                throw new TimeoutException();
            }
        }

        /// <summary>
        /// Times out a given <see cref="Action"/> after a period of time.
        /// </summary>
        /// <param name="action">The <see cref="Action"/> to timeout.</param>
        /// <param name="timeSpan">The period of time before timing out the task.</param>
        /// <exception cref="TimeoutException">The exception that is thrown as a result of a timeout.</exception>
        public static void TimeoutTask(Action action, TimeSpan timeSpan)
        {
            TimeoutTask(Task.Run(action), timeSpan);
        }
    }
}