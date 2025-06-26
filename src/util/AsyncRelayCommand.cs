using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Logavise
{
    public class AsyncRelayCommand(Func<Task> execute) : ICommand
    {
        private bool _isExecuting;

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return !_isExecuting;
        }

        public async void Execute(object parameter)
        {
            _isExecuting = true;
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);

            try
            {
                await execute();
            }
            finally
            {
                _isExecuting = false;
                CanExecuteChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}
