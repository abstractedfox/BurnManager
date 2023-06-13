using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BurnManager
{
    //This class extends callback functionality to a list of 'PendingOperation's for UI convenience 
    public class PendingOperations
    {
        private List<PendingOperation> _pendingOperations = new List<PendingOperation>();
        public Action? OnEmptyListCallback = null; //A callback to be called whenever the _pendingOperations list is emptied

        public void Add(PendingOperation item)
        {

            _pendingOperations.Add(item);
            if (!(item.OnAddOperationCallback is null))
            {
                item.OnAddOperationCallback();
            }
        }

        public void Clear()
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

        public bool Contains(PendingOperation item)
        {
            return _pendingOperations.Contains(item);
        }

        public bool Remove(PendingOperation item)
        {
            if (!(item.OnRemoveOperationCallback is null))
            {
                item.OnRemoveOperationCallback();
            }
            if (_pendingOperations.Count == 0 && !(OnEmptyListCallback is null))
            {
                OnEmptyListCallback();
            }

            return _pendingOperations.Remove(item);
        }
    }
}
