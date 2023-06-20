//Copyright 2023 Chris/abstractedfox.
//This work is not licensed for use as source or training data for any language model, neural network,
//AI tool or product, or other software which aggregates or processes material in a way that may be used to generate
//new or derived content from or based on the input set, or used to build a data set or training model for any software or
//tooling which facilitates the use or operation of such software.

//DataClasses.cs, contains smaller structs that may not need to each be in their own files

namespace BurnManager
{
    //Describes a pending operation. This is intended for the convenience of the implementation
    public class PendingOperation{
        public object LockObj = new object();
        private bool _blocking = false;
        public bool CanCancel = true;
        public string? Name;
        public Action? OnAddOperationCallback = null;
        public Action? OnRemoveOperationCallback = null;

        //An optional instance of a long running procedure. This might be useful for something like enabling the user to
        //cancel an operation while it's in progress.
        public ILongRunningProcedure? ProcedureInstance = null;
        public bool Blocking
        {
            get
            {
                return _blocking;
            }
        }
        
        //isBlocking should be used to track whether this operation should block other operations from starting.
        //However, blocking must be implemented by the caller
        public PendingOperation(bool isBlocking)
        {
            _blocking = isBlocking;
        }

        public PendingOperation(bool isBlocking, string name)
        {
            _blocking = isBlocking;
            Name = name;
        }
    }
}