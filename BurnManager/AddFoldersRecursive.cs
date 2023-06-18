
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BurnManager
{
    public class AddFoldersRecursive : ILongRunningProcedure
    {
        private Task? _addFoldersTask;

        public Action? callOnCompletionDelegate { get; set; }

        protected bool _shouldAlwaysRun = false; //Should the loop continue to run if there is no data in queue
        protected bool _isCurrentlyRunning = false; //Is the procedure running
        protected bool _halt = false; //Has the procedure been instructed to halt

        private object _lockObj = new object();
        protected BurnManagerAPI _api;

        public bool IsComplete
        {
            get
            {
                return _isCurrentlyRunning;
            }
        }

        //Convenient buffer for the checksum queue
        private List<FileProps> _checksumBufferOutput = new List<FileProps>();

        //Get the contents of the checksum buffer and clear it. If sizeLimit > 0, it will return null unless there are 'sizeLimit' elements in the buffer.
        //If the procedure is not currently running, it will return whatever is in the buffer regardless of size limit.
        public IReadOnlyList<FileProps>? GetBufferContentsAndClear(int sizeLimit)
        {
            lock (_lockObj)
            {
                if (_isCurrentlyRunning && sizeLimit > 0 && _checksumBufferOutput.Count < sizeLimit) return null;

                List<FileProps> output = new List<FileProps>(_checksumBufferOutput);
                _checksumBufferOutput.Clear();
                return output.AsReadOnly();
            }
        }

        private LinkedList<DirectoryInfo> _nextFolders = new LinkedList<DirectoryInfo>();

        public AddFoldersRecursive(BurnManagerAPI api)
        {
            _api = api;
        }

        public virtual void StartOperation()
        {
            lock (_lockObj)
            {
                if (_isCurrentlyRunning) return;
                _shouldAlwaysRun = true;
                _addFoldersTask = _mainTask();
            }
        }

        public virtual void AddFolderToQueue(DirectoryInfo folder)
        {
            lock (_lockObj)
            {
                _nextFolders.AddLast(folder);
            }
        }

        private async Task _mainTask()
        {
            Func<bool> loopCondition = () =>
            {
                lock (_lockObj)
                {
                    return (_shouldAlwaysRun || _nextFolders.Count > 0) && !_halt;
                }
            };

            await Task.Run(async () => {
                _isCurrentlyRunning = true;
                while (loopCondition())
                {

                    LinkedListNode<DirectoryInfo> currentNode;
                    lock (_lockObj)
                    {
                        if (_nextFolders.First != null)
                        {
                            currentNode = _nextFolders.First;
                        }
                        else
                        {
                            continue;
                        }
                    }

                    foreach (var file in currentNode.Value.GetFiles())
                    {
                        if (_halt)
                        {
                            break;
                        }

                        FileProps thisFile = new FileProps(file);
                        _api.AddFile(thisFile);
                        lock (_lockObj)
                        {
                            _checksumBufferOutput.Add(thisFile);
                        }
                    }

                    foreach (var folder in currentNode.Value.GetDirectories())
                    {
                        if (_halt)
                        {
                            break;
                        }

                        lock (_lockObj)
                        {
                            _nextFolders.AddLast(folder);
                        }
                    }

                    lock (_lockObj)
                    {
                        _nextFolders.Remove(currentNode);
                    }
                }

                _end();
            });
        }

        public virtual void EndWhenComplete()
        {
            _shouldAlwaysRun = false;
        }

        public virtual void EndImmediately()
        {
            _halt = true;
            _shouldAlwaysRun = false;
        }

        private void _end()
        {
            _isCurrentlyRunning = false;
            if (callOnCompletionDelegate != null)
            {
                callOnCompletionDelegate();
            }
        }
    }
}
