using System.Collections.Generic;
using System.Threading;

namespace ArchiverApp
{
    public class TaskPool
    {
        #region Fields

        private Queue<KeyValuePair<int, byte[]>> _taskPool;

        private readonly int _capacity;

        private bool _isTerminate;

        #endregion

        #region .ctor

        public TaskPool(int capacity)
        {
            _isTerminate = false;
            _taskPool = new Queue<KeyValuePair<int, byte[]>>(capacity);
            _capacity = capacity;
        }

        #endregion

        public bool TrySet(int position, byte[] value)
        {
            lock (_taskPool)
            {
                while (_taskPool.Count >= _capacity)
                {
                    if (_isTerminate)
                    {
                        return false;
                    }

                    Monitor.Wait(_taskPool);
                }

                if (_isTerminate)
                {
                    return false;
                }

                _taskPool.Enqueue(new KeyValuePair<int, byte[]>(position, value));

                Monitor.Pulse(_taskPool);
                return true;
            }
        }

        public bool TryGet(out int position, out byte[] value)
        {
            lock (_taskPool)
            {
                while (_taskPool.Count == 0)
                {
                    if (_isTerminate)
                    {
                        position = -1;
                        value = null;
                        return false;
                    }

                    Monitor.Wait(_taskPool);
                }

                if (_isTerminate)
                {
                    position = -1;
                    value = null;
                    return false;
                }

                KeyValuePair<int, byte[]> block = _taskPool.Dequeue();
                position = block.Key;
                value = block.Value;

                Monitor.Pulse(_taskPool);
                return true;
            }
        }

        public void Terminate()
        {
            lock (_taskPool)
            {
                _isTerminate = true;
                Monitor.PulseAll(_taskPool);
            }
        }
    }
}
