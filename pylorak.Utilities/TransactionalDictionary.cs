using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;

namespace pylorak.Utilities
{
    public sealed class TransactionalDictionary<K, V> where K : notnull
    {
        public class Transaction : Disposable
        {
            private readonly TransactionalDictionary<K, V> _Parent;
            private readonly Dictionary<K, V> _Dictionary;
            private bool _IsCommitted = false;

            private void ThrowIfClosed()
            {
                if (IsDisposed || _IsCommitted)
                    throw new InvalidOperationException("The transaction is already closed.");
            }

            // Note of unenforced code contract:
            // A Dictionary reference retrieved before Commit() must not be modified after Commit().
            public Dictionary<K, V> Dictionary
            {
                get
                {
                    ThrowIfClosed();
                    return _Dictionary;
                }
            }

            public Transaction(TransactionalDictionary<K, V> parent, bool startWithEmptyDict)
            {
                if (Interlocked.CompareExchange(ref parent.EditorsInProgress, 1, 0) != 0)
                    throw new InvalidOperationException("A transaction for this dictionary is already in progress.");

                try
                {
                    _Parent = parent;
                    _Dictionary = startWithEmptyDict ? new Dictionary<K, V>() : new Dictionary<K, V>(parent.CommittedDictionary);
                }
                catch
                {
                    Interlocked.Decrement(ref parent.EditorsInProgress);
                    throw;
                }
            }

            public void Commit()
            {
                ThrowIfClosed();
                _Parent.CommittedDictionary = new ReadOnlyDictionary<K, V>(Dictionary);
                _IsCommitted = true;
            }

            protected override void Dispose(bool disposing)
            {
                if (IsDisposed)
                    return;

                if (disposing)
                {
                    Interlocked.Decrement(ref _Parent.EditorsInProgress);
                }

                base.Dispose(disposing);
            }
        }

        private volatile ReadOnlyDictionary<K, V> CommittedDictionary = new(new Dictionary<K, V>());
        private int EditorsInProgress = 0;

        public Transaction CreateTransaction(bool startWithEmptyDict)
        {
            return new Transaction(this, startWithEmptyDict);
        }

        public ReadOnlyDictionary<K, V> Snapshot
        {
            get => CommittedDictionary;
        }
    }
}
