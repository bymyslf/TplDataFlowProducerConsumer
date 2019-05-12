using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace TplDataFlowProducerConsumer
{
    internal class DataflowProducerConsumer
    {
        private static async Task Produce(BufferBlock<int> queue, IEnumerable<int> values)
        {
            foreach (var value in values)
            {
                await queue.SendAsync(value);
            }
        }

        private static async Task ProduceAll(BufferBlock<int> queue)
        {
            var producer1 = Produce(queue, Enumerable.Range(0, 10));
            var producer2 = Produce(queue, Enumerable.Range(8, 12));
            var producer3 = Produce(queue, Enumerable.Range(15, 15));

            await Task.WhenAll(producer1, producer2, producer3);

            queue.Complete();
        }

        private static async Task<IEnumerable<int>> Consume(BufferBlock<int> queue)
        {
            var ret = new List<int>();
            while (await queue.OutputAvailableAsync())
            {
                ret.Add(await queue.ReceiveAsync());
            }

            return ret;
        }

        static async Task Main(string[] args)
        {
            var results = new List<int>();
            var duplicated = new List<int>();

            var queue = new BufferBlock<int>(new DataflowBlockOptions { BoundedCapacity = 5, });
            var consumer = new ActionBlock<int>(x =>
            {
                if (results.Contains(x))
                    duplicated.Add(x);
                else
                    results.Add(x);
            }
            , new ExecutionDataflowBlockOptions { BoundedCapacity = 1, });

            queue.LinkTo(consumer, new DataflowLinkOptions { PropagateCompletion = true, });

            var producers = ProduceAll(queue);

            await Task.WhenAll(producers, consumer.Completion);

            Console.WriteLine("::::::: FINAL RESULTS ::::::");

            foreach (var item in results.OrderBy(x => x))
                Console.WriteLine(item);

            Console.WriteLine("::::::: IGNORED ITEMS ::::::");

            foreach (var item in duplicated.OrderBy(x => x))
                Console.WriteLine(item);

            Console.Read();
        }
    }
}
