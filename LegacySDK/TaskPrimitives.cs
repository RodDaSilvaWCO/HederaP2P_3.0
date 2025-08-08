using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Unosys.SDK
{
	public class AsyncManualResetEvent
	{
		// https://blogs.msdn.microsoft.com/pfxteam/2012/02/11/building-async-coordination-primitives-part-1-asyncmanualresetevent/
		private TaskCompletionSource<bool> m_tcs = new TaskCompletionSource<bool>();

		public Task WaitAsync() { return m_tcs.Task; }

		public void Set() { m_tcs.TrySetResult( true ); }

		public void Reset()
		{
			while (true)
			{
				var tcs = m_tcs;
				if (!tcs.Task.IsCompleted ||
					Interlocked.CompareExchange( ref m_tcs, new TaskCompletionSource<bool>(), tcs ) == tcs)
					return;
			}
		}
	}

	public class AsyncAutoResetEvent
	{
		// https://blogs.msdn.microsoft.com/pfxteam/2012/02/11/building-async-coordination-primitives-part-2-asyncautoresetevent/
		private readonly static Task s_completed = Task.FromResult( true );
		private readonly Queue<TaskCompletionSource<bool>> m_waits = new Queue<TaskCompletionSource<bool>>();
		private bool m_signaled;


		public Task WaitAsync()
		{
			lock (m_waits)
			{
				if (m_signaled)
				{
					m_signaled = false;
					return s_completed;
				}
				else
				{
					var tcs = new TaskCompletionSource<bool>();
					m_waits.Enqueue( tcs );
					return tcs.Task;
				}
			}
		}

		public void Set()
		{
			TaskCompletionSource<bool> toRelease = null;
			lock (m_waits)
			{
				if (m_waits.Count > 0)
					toRelease = m_waits.Dequeue();
				else if (!m_signaled)
					m_signaled = true;
			}
			if (toRelease != null)
				toRelease.SetResult( true );
		}
	}

	public class AsyncCountdownEvent
	{
		// https://blogs.msdn.microsoft.com/pfxteam/2012/02/11/building-async-coordination-primitives-part-3-asynccountdownevent/
		private readonly AsyncManualResetEvent m_amre = new AsyncManualResetEvent();
		private int m_count;

		public AsyncCountdownEvent( int initialCount )
		{
			if (initialCount <= 0) throw new ArgumentOutOfRangeException("initialCount");
			m_count = initialCount;
		}


		public Task WaitAsync() { return m_amre.WaitAsync(); }


		public void Signal()
		{
			if (m_count <= 0)
				throw new InvalidOperationException();

			int newCount = Interlocked.Decrement( ref m_count );
			if (newCount == 0)
				m_amre.Set();
			else if (newCount < 0)
				throw new InvalidOperationException();
		}

		public Task SignalAndWait()
		{
			Signal();
			return WaitAsync();
		}
	}

	public class AsyncBarrier
	{
		// https://blogs.msdn.microsoft.com/pfxteam/2012/02/11/building-async-coordination-primitives-part-4-asyncbarrier/
		private readonly int m_participantCount;
		private TaskCompletionSource<bool> m_tcs = new TaskCompletionSource<bool>();
		private int m_remainingParticipants;

		public AsyncBarrier( int participantCount )
		{
			if (participantCount <= 0) throw new ArgumentOutOfRangeException("participantCount");
			m_remainingParticipants = m_participantCount = participantCount;
		}

		public Task SignalAndWait()
		{
			var tcs = m_tcs;
			if (Interlocked.Decrement( ref m_remainingParticipants ) == 0)
			{
				m_remainingParticipants = m_participantCount;
				m_tcs = new TaskCompletionSource<bool>();
				tcs.SetResult( true );
			}
			return tcs.Task;
		}


	}

	public class AsyncSemaphore
	{
		// https://blogs.msdn.microsoft.com/pfxteam/2012/02/12/building-async-coordination-primitives-part-5-asyncsemaphore/
		private readonly static Task s_completed = Task.FromResult( true );
		private readonly Queue<TaskCompletionSource<bool>> m_waiters = new Queue<TaskCompletionSource<bool>>();
		private int m_currentCount;

		public AsyncSemaphore( int initialCount )
		{
			if (initialCount < 0) throw new ArgumentOutOfRangeException("initialCount");
			m_currentCount = initialCount;
		}


		public Task WaitAsync()
		{
			lock (m_waiters)
			{
				if (m_currentCount > 0)
				{
					--m_currentCount;
					return s_completed;
				}
				else
				{
					var waiter = new TaskCompletionSource<bool>();
					m_waiters.Enqueue(waiter);
					return waiter.Task;
				}
			}
		}


		public void Release()
		{
			TaskCompletionSource<bool> toRelease = null;
			lock (m_waiters)
			{
				if (m_waiters.Count > 0)
					toRelease = m_waiters.Dequeue();
				else
					++m_currentCount;
			}
			if (toRelease != null)
				toRelease.SetResult( true );
		}
	}

	public class AsyncLock
	{
		// https://blogs.msdn.microsoft.com/pfxteam/2012/02/12/building-async-coordination-primitives-part-6-asynclock/

		// Usage:
		//private readonly AsyncLock m_lock = new AsyncLock();
		//  …
		//using(var releaser = await m_lock.LockAsync())
		//{
		//	… // protected code here
		//}
		private readonly AsyncSemaphore m_semaphore;
		private readonly Task<Releaser> m_releaser;

		public AsyncLock()
		{
			m_semaphore = new AsyncSemaphore( 1 );
			m_releaser = Task.FromResult( new Releaser( this ) );
		}

		public Task<Releaser> LockAsync()
		{
			var wait = m_semaphore.WaitAsync();
			return wait.IsCompleted ?
				m_releaser :
				wait.ContinueWith( ( _, state ) => new Releaser( (AsyncLock) state ),
					this, CancellationToken.None,
					TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default );
		}

		public struct Releaser : IDisposable
		{
			private readonly AsyncLock m_toRelease;

			public Releaser( AsyncLock toRelease ) { m_toRelease = toRelease; }

			public void Dispose()
			{
				if (m_toRelease != null)
					m_toRelease.m_semaphore.Release();
			}
		}
	}

	public class AsyncReaderWriterLock
	{
		// https://blogs.msdn.microsoft.com/pfxteam/2012/02/12/building-async-coordination-primitives-part-7-asyncreaderwriterlock/
		private readonly Task<Releaser> m_readerReleaser;
		private readonly Task<Releaser> m_writerReleaser;


		private readonly Queue<TaskCompletionSource<Releaser>> m_waitingWriters =
			new Queue<TaskCompletionSource<Releaser>>();
		private TaskCompletionSource<Releaser> m_waitingReader =
			new TaskCompletionSource<Releaser>();
		private int m_readersWaiting;
		private int m_status;


		public AsyncReaderWriterLock()
		{
			m_readerReleaser = Task.FromResult( new Releaser( this, false ) );
			m_writerReleaser = Task.FromResult( new Releaser( this, true ) );
		}

		public Task<Releaser> ReaderLockAsync()
		{
			lock (m_waitingWriters)
			{
				if (m_status >= 0 && m_waitingWriters.Count == 0)
				{
					++m_status;
					return m_readerReleaser;
				}
				else
				{
					++m_readersWaiting;
					return m_waitingReader.Task.ContinueWith( t => t.Result );
				}
			}
		}


		public Task<Releaser> WriterLockAsync()
		{
			lock (m_waitingWriters)
			{
				if (m_status == 0)
				{
					m_status = -1;
					return m_writerReleaser;
				}
				else
				{
					var waiter = new TaskCompletionSource<Releaser>();
					m_waitingWriters.Enqueue( waiter );
					return waiter.Task;
				}
			}
		}

		

		private void ReaderRelease()
		{
			TaskCompletionSource<Releaser> toWake = null;

			lock (m_waitingWriters)
			{
				--m_status;
				if (m_status == 0 && m_waitingWriters.Count > 0)
				{
					m_status = -1;
					toWake = m_waitingWriters.Dequeue();
				}
			}

			if (toWake != null)
				toWake.SetResult(new Releaser(this, true));
		}




		private void WriterRelease()
		{
			TaskCompletionSource<Releaser> toWake = null;
			bool toWakeIsWriter = false;

			lock (m_waitingWriters)
			{
				if (m_waitingWriters.Count > 0)
				{
					toWake = m_waitingWriters.Dequeue();
					toWakeIsWriter = true;
				}
				else if (m_readersWaiting > 0)
				{
					toWake = m_waitingReader;
					m_status = m_readersWaiting;
					m_readersWaiting = 0;
					m_waitingReader = new TaskCompletionSource<Releaser>();
				}
				else m_status = 0;
			}

			if (toWake != null)
				toWake.SetResult( new Releaser( this, toWakeIsWriter ) );
		}


		public struct Releaser : IDisposable
		{
			private readonly AsyncReaderWriterLock m_toRelease;
			private readonly bool m_writer;

			public Releaser( AsyncReaderWriterLock toRelease, bool writer )
			{
				m_toRelease = toRelease;
				m_writer = writer;
			}

			public void Dispose()
			{
				if (m_toRelease != null)
				{
					if (m_writer) m_toRelease.WriterRelease();
					else m_toRelease.ReaderRelease();
				}
			}
		}

	}
}
