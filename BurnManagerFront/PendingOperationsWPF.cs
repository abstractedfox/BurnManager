﻿
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace BurnManagerFront
{
    public class PendingOperationsWPF : BurnManager.PendingOperations
    {
        private MainWindow _mainWindowInstance;
        public PendingOperationsWPF(MainWindow window)
        {
            _mainWindowInstance = window;

            this.OnEmptyListCallback = () =>
            {
                _mainWindowInstance.UpdateStatusIndicator("Ready");

                _mainWindowInstance.Dispatcher.Invoke(() => { 
                    _mainWindowInstance.CancelOperationButton.IsEnabled = false;
                });
            };
        }

        public new bool Add(BurnManager.PendingOperation operation)
        {
            bool result = base.Add(operation);
            if (!result)
            {
                FrontendFunctions.OperationsInProgressDialog(_pendingOperations);
                return result;
            }

            if (!(operation.Name is null))
            {
                _mainWindowInstance.UpdateStatusIndicator(operation.Name);
            }
            else
            {
                _mainWindowInstance.UpdateStatusIndicator("Unnamed Operation");
            }

            if (operation.CanCancel)
            {
                _mainWindowInstance.Dispatcher.Invoke(() => { 
                    _mainWindowInstance.CancelOperationButton.IsEnabled = true;
                });
            }

            return result;
        }

        public new bool Remove(BurnManager.PendingOperation operation)
        {
            bool result = base.Remove(operation);
            if (!result)
            {
                throw new Exception("PendingOperation was not registered");
            }
            return result;
        }
    }
}
