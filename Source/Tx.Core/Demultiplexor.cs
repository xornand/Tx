﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Subjects;

namespace System.Reactive
{
    /// <summary>
    ///     Efficiently demultiplexes input sequence of objects into output sequences of fixed types
    ///     The callbacks on the output sequences are called in the order of occurence of input events
    ///     OnNext of the Demultiplexor should not be called from multiple threads
    /// </summary>
    public class Demultiplexor : IObserver<object>
    {
        private readonly Dictionary<Type, IObserver<object>> _outputs = new Dictionary<Type, IObserver<object>>();

        public void OnCompleted()
        {
            foreach (var output in _outputs.Values.ToArray())
            {
                output.OnCompleted();
            }
        }

        public void OnError(Exception error)
        {
            foreach (var output in _outputs.Values)
            {
                output.OnError(error);
            }
        }

        public void OnNext(object value)
        {
            IObserver<object> output;
            if (_outputs.TryGetValue(value.GetType(), out output))
            {
                output.OnNext(value);
            }

            if (_outputs.TryGetValue(value.GetType().BaseType, out output))
            {
                output.OnNext(value);
            }
        }

        /// <summary>
        ///     Returns an output sequence of given type
        /// </summary>
        /// <typeparam name="TOutput">The desired type</typeparam>
        /// <returns>Sequence in which all events are of type TOutput</returns>
        public IObservable<TOutput> GetObservable<TOutput>()
        {
            IObserver<object> o;
            if (!_outputs.TryGetValue(typeof (TOutput), out o))
            {
                o = new OutputSubject<TOutput>();
                _outputs.Add(typeof (TOutput), o);
            }

            var output = (IObservable<TOutput>) o;
            return output;
        }

        private class OutputSubject<T> : ISubject<object, T>, IDisposable
        {
            private readonly Subject<T> _subject;
            private int _refcount;

            public OutputSubject()
            {
                _subject = new Subject<T>();
            }

            public void Dispose()
            {
                _refcount--;
                //if (_refcount == 0)
                //{
                //    _parent._outputs.Remove(typeof(T));
                //}
            }

            public void OnCompleted()
            {
                _subject.OnCompleted();
            }

            public void OnError(Exception error)
            {
                _subject.OnError(error);
            }

            public void OnNext(object value)
            {
                _subject.OnNext((T) value);
            }

            public IDisposable Subscribe(IObserver<T> observer)
            {
                IDisposable subscription = _subject.Subscribe(observer);
                _refcount++;

                return new CompositeDisposable(subscription, this);
            }
        }
    }
}