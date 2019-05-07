using System;
using System.Runtime.CompilerServices;
using UnityEngine.ResourceManagement.AsyncOperations;

/// <summary>
/// Script cleaned up and taken from https://forum.unity.com/threads/async-await-support-for-loading-assets.538898/
/// </summary>
public static class IAsyncOperationExtensions
{
    public static AsyncOperationAwaiter GetAwaiter(this AsyncOperationHandle operation)
    {
        return new AsyncOperationAwaiter(operation);
    }

    public static AsyncOperationAwaiter<T> GetAwaiter<T>(this AsyncOperationHandle<T> operation) where T : class
    {
        return new AsyncOperationAwaiter<T>(operation);
    }

    public readonly struct AsyncOperationAwaiter : INotifyCompletion
    {
        readonly AsyncOperationHandle _operation;

        public AsyncOperationAwaiter(AsyncOperationHandle operation)
        {
            _operation = operation;
        }

        public bool IsCompleted => _operation.Status != AsyncOperationStatus.None;

        public void OnCompleted(Action continuation) => _operation.Completed += (op) => continuation?.Invoke();

        public object GetResult() => _operation.Result;

    }

    public readonly struct AsyncOperationAwaiter<T> : INotifyCompletion where T : class
    {
        readonly AsyncOperationHandle<T> _operation;

        public AsyncOperationAwaiter(AsyncOperationHandle<T> operation)
        {
            _operation = operation;
        }

        public bool IsCompleted => _operation.Status != AsyncOperationStatus.None;

        public void OnCompleted(Action continuation) => _operation.Completed += (op) => continuation?.Invoke();

        public T GetResult() => _operation.Result;
    }
}