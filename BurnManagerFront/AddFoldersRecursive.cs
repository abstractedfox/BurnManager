using BurnManagerFront;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace BurnManager
{
    public class AddFoldersRecursive : ILongRunningProcedure
    {
        private Task? _addFoldersTask;
        public CompletionCallback? callOnCompletionDelegate { get; set; }
        private bool keepRunning = false; //Should the loop continue to run
        private bool running = false; //Is the procedure running
        private bool halt = false; //Has the procedure been instructed to halt
        public bool StopWhenComplete { get; set; } = true;
        private object _lockObj = new object();
        private int _batchQueueSize = 100;
        private BurnManagerAPI _api;

        public bool IsComplete
        {
            get
            {
                return running;
            }
        }
        

        private LinkedList<StorageFolder> _nextFolders = new LinkedList<StorageFolder>();
        //private List<FileProps> _erroredFiles;

        private ChecksumFactory checksumFactory = new ChecksumFactory();

        public AddFoldersRecursive(int batchQueueSize, BurnManagerAPI api)
        {
            _batchQueueSize = batchQueueSize;
            _api = api;
        }

        //Should not return until checksumFactory.StartOperation() returns
        public async Task StartOperation()
        {
            keepRunning = true;
            _addFoldersTask = _mainTask();
            await checksumFactory.StartOperation();

            _end();
        }

        public void AddFolderToQueue(StorageFolder folder)
        {
            lock (_lockObj)
            {
                _nextFolders.AddLast(folder);
            }
        }

        private async Task _mainTask()
        {
            await Task.Run(async () => {
                running = true;
                List<FileProps> filesToChecksum = new List<FileProps>();
                while (keepRunning || _nextFolders.Count > 0)
                {
                    StorageFolder currentFolder;
                    LinkedListNode<StorageFolder> currentNode;
                    lock (_lockObj)
                    {
                        if (_nextFolders.Count == 0 && StopWhenComplete)
                        {
                            keepRunning = false;
                            break;
                        }

                        currentFolder = _nextFolders.First();
                        if (_nextFolders.First != null)
                        {
                            currentNode = _nextFolders.First;
                        }
                        else
                        {
                            continue;
                        }
                    }

                    foreach (var file in await currentNode.Value.GetFilesAsync())
                    {
                        if (halt)
                        {
                            break;
                        }

                        FileProps thisFile = await FrontendFunctions.StorageFileToFileProps(file);
                        _api.AddFile(thisFile);
                        filesToChecksum.Add(thisFile);
                        

                        if (filesToChecksum.Count == _batchQueueSize)
                        {
                            checksumFactory.AddBatch(filesToChecksum);
                            filesToChecksum.Clear();
                        }
                    }
                    foreach (var folder in await currentNode.Value.GetFoldersAsync())
                    {
                        if (halt)
                        {
                            break;
                        }

                        lock (_lockObj)
                        {
                            _nextFolders.AddLast(folder);
                        }
                    }

                    _nextFolders.Remove(currentNode);
                }


                if (halt)
                {
                    checksumFactory.EndImmediately();
                    return;
                }

                if (filesToChecksum.Count > 0)
                {
                    checksumFactory.AddBatch(filesToChecksum);
                    filesToChecksum.Clear();
                }
            });
        }

        

        public void EndWhenComplete()
        {
            checksumFactory.EndWhenComplete();
            keepRunning = false;
        }

        public void EndImmediately()
        {
            halt = true;
            keepRunning = false;
            checksumFactory.EndImmediately();
        }

        //Should only be called when all tasks have completed
        private void _end()
        {
            running = false;
            if (callOnCompletionDelegate != null)
            {
                callOnCompletionDelegate();
            }
        }
    }
}
