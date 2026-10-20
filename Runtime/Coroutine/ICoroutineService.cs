using System;
using System.Collections;

namespace Commons.Coroutine
{
    public interface ICoroutineService : IService
    {
        public Guid StartCoroutine(IEnumerator enumerator);
        public UnityEngine.Coroutine GetCoroutine(Guid guid);
        public void StopCoroutine(ref Guid guid);
    }
}