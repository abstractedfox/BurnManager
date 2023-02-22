using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace BurnManager
{
    public class ChecksumFactory
    {
        private object _lockObj = new object();
        private bool running = false;
        public bool IsRunning
        {
            get
            {
                return running;
            }
        }
        private bool halt = false;
        private List<List<FileProps>> batches = new List<List<FileProps>>();
        private List<List<FileProps>> erroredFiles = new List<List<FileProps>>();

        public ChecksumFactory()
        {
        }

        //Add a new batch to the queue
        public void AddBatch(IReadOnlyList<FileProps> batch)
        {
            lock (_lockObj)
            {
                batches.Add(new List<FileProps>(batch));
            }
        }

        //Start processing whatever is in the queue. The batch loop will run regardless of contents until FinishQueue() is called.
        public async void StartQueue()
        {
            lock (_lockObj)
            {
                if (running) return;
                running = true;
            }

            await Task.Run(() => { _runBatch(); });
        }

        //Call when finished passing new batches. All queued batches will complete, and the batch task will return.
        public void FinishQueue()
        {
            lock (_lockObj)
            {
                running = false;
            }
        }
        
        //The batch process will end after the current job finishes regardless of queued work
        public void FinishImmediately()
        {
            halt = true;
        }

        private async void _runBatch()
        {
            while (running || batches.Count > 0)
            {
                List<FileProps> thisBatch = new List<FileProps>();
                lock (_lockObj)
                {
                    if (batches.Count > 0)
                    {
                        thisBatch = new List<FileProps>(batches.Last());
                    }
                    else continue;
                }

                //Do not block AddBatch by remaining locked while a batch is processing
                List<FileProps> errorOut = new List<FileProps>();
                await BurnManagerAPI.GenerateChecksums(thisBatch, true, errorOut);
                if (errorOut.Count > 0) erroredFiles.Add(errorOut);
                if (!batches.Remove(thisBatch))
                {
                    throw new Exception("Batch collection was modified in an unexpected way");
                }
                if (halt)
                {
                    running = false;
                    break;
                }
            }
            Console.WriteLine("breakpoint!");
        }


    }
}
