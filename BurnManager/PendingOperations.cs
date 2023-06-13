using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BurnManager
{
    //This class gives callback functionality to a list of 'PendingOperation's for UI convenience
    public class PendingOperations
    {
        protected object _lockObj = new object();

        protected List<PendingOperation> _pendingOperations = new List<PendingOperation>();

        //A callback to be called once after the list is emptied
        private Action? _onEmptyListCallback = null;
        public Action? OnEmptyListCallback
        {
            get
            {
                lock (_lockObj)
                {
                    return _onEmptyListCallback;
                }
            }
            set
            {
                lock (_lockObj)
                {
                    _onEmptyListCallback = value;
                    if (!(_onEmptyListCallback is null))
                    {
                        _onEmptyListCallback();
                    }
                }
            }
        }

        public bool Add(PendingOperation item)
        {
            lock (_lockObj)
            {
                if (_pendingOperations.Count > 0 && _pendingOperations.Where(operation => operation.Blocking == true).Any())
                {
                    return false;
                }

                _pendingOperations.Add(item);
                if (!(item.OnAddOperationCallback is null))
                {
                    item.OnAddOperationCallback();
                }
                return true;
            }
        }

        public void Clear()
        {
            lock (_lockObj)
            {
                foreach (var operation in _pendingOperations)
                {
                    if (!(operation.OnRemoveOperationCallback is null))
                    {
                        operation.OnRemoveOperationCallback();
                    }
                }
                _pendingOperations.Clear();
            }
        }

        public bool Contains(PendingOperation item)
        {
            lock (_lockObj)
            {
                return _pendingOperations.Contains(item);
            }
        }

        public bool Remove(PendingOperation item)
        {
            lock (_lockObj)
            {
                if (!(item.OnRemoveOperationCallback is null))
                {
                    item.OnRemoveOperationCallback();
                }

                bool result = _pendingOperations.Remove(item);

                if (_pendingOperations.Count == 0 && !(OnEmptyListCallback is null))
                {
                    OnEmptyListCallback();
                }

                return result;
            }
        }
    }
}
