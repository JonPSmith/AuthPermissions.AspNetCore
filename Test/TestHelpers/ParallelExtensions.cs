// Copyright (c) 2023 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Threading.Tasks.Dataflow;

namespace Test.TestHelpers
{
    //NOTE Parallel.ForEach doesn't handle async
    //I found a very useful article https://medium.com/@alex.puiu/parallel-foreach-async-in-c-36756f8ebe62 
    //From this I decided the AsyncParallelForEach approach, which can run async methods in paralle
    public static class ParallelExtensions
    {
        public static async IAsyncEnumerable<int> NumTimesAsyncEnumerable(this int numTimes)
        {
            for (int i = 1; i <= numTimes; i++)
            {
                yield return i;
            }
        }


        public static async Task AsyncParallelForEach<T>(this IAsyncEnumerable<T> source, Func<T, Task> body, int maxDegreeOfParallelism = DataflowBlockOptions.Unbounded, TaskScheduler scheduler = null)
        {
            var options = new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = maxDegreeOfParallelism
            };
            if (scheduler != null)
                options.TaskScheduler = scheduler;

            var block = new ActionBlock<T>(body, options);

            await foreach (var item in source)
                block.Post(item);

            block.Complete();
            await block.Completion;
        }
    }
}