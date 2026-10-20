using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Commons.Coroutine
{
    /// <summary>
    /// This service allows usage of coroutines outside MonoBehaviours
    /// </summary>
    public class CoroutineService : MonoBehaviour, ICoroutineService
    {
        #region FIELDS
        
        private readonly Dictionary<Guid, CoroutineData> _coroutines = new();

        #endregion

        #region PUBLIC METHODS
        
        /// <summary>
        /// This method starts non-MonoBehaviour coroutine.
        /// </summary>
        /// <param name="enumerator"><see cref="IEnumerator"/> to perform as <see cref="Coroutine"/></param>
        /// <returns><see cref="Guid">GUID</see> to use when trying to <see cref="GetCoroutine"/></returns>
        public new Guid StartCoroutine(IEnumerator enumerator)
        {
            var guid = Guid.NewGuid();
            
            var newCoroutine = ((MonoBehaviour)this).StartCoroutine(enumerator);
            var newCoroutineData = new CoroutineData(enumerator, newCoroutine);
            
            _coroutines.Add(guid, newCoroutineData);
            
            return guid;
        }

        /// <summary>
        /// Get reference to non-MonoBehaviour coroutine.
        /// </summary>
        /// <param name="guid"><see cref="Guid">GUID</see> of requested <see cref="Coroutine"/>.</param>
        /// <returns><see cref="Coroutine"/> with given <see cref="Guid">GUID</see>.</returns>
        public UnityEngine.Coroutine GetCoroutine(Guid guid)
        {
            return _coroutines.TryGetValue(guid, out var coroutine) 
                ? coroutine.Coroutine 
                : null;
        }
        
        /// <summary>
        /// Stop non-MonoBehaviour coroutine.
        /// </summary>
        /// <param name="guid"><see cref="Guid">GUID</see> of requested <see cref="Coroutine"/> Guid is set to Guid.Empty.</param>
        public void StopCoroutine(ref Guid guid)
        {
            _coroutines.TryGetValue(guid, out var data);
            guid = Guid.Empty;

            if (data is not { Coroutine: not null }) 
                return;
            
            StopCoroutine(data.Coroutine);
        }

        #endregion
    }
}


