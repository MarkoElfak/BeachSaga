using System;
using System.Collections.Generic;
using UnityEngine;

namespace BeachTycoon.Core
{
    /// <summary>
    /// Small static, strongly typed event bus. Every event type gets its own static channel,
    /// so publishing a struct event is a plain delegate call with no boxing or allocation.
    /// </summary>
    public static class EventBus
    {
        private static readonly List<Action> ChannelClearers = new List<Action>();

        public static void Subscribe<T>(Action<T> handler) where T : struct
        {
            Channel<T>.Subscribe(handler);
        }

        public static void Unsubscribe<T>(Action<T> handler) where T : struct
        {
            Channel<T>.Unsubscribe(handler);
        }

        public static void Publish<T>(T evt) where T : struct
        {
            Channel<T>.Publish(evt);
        }

        /// <summary>Removes every subscriber from every channel (new session, tests).</summary>
        public static void ClearAll()
        {
            for (int i = 0; i < ChannelClearers.Count; i++)
            {
                ChannelClearers[i]();
            }
        }

        // Static state survives play mode when domain reload is disabled, so reset it explicitly.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlayModeEnter()
        {
            ClearAll();
        }

        private static class Channel<T> where T : struct
        {
            private static Action<T> _handlers;
            private static bool _registered;

            public static void Subscribe(Action<T> handler)
            {
                if (!_registered)
                {
                    _registered = true;
                    ChannelClearers.Add(Clear);
                }

                _handlers -= handler; // guard against double subscription
                _handlers += handler;
            }

            public static void Unsubscribe(Action<T> handler)
            {
                _handlers -= handler;
            }

            public static void Publish(T evt)
            {
                _handlers?.Invoke(evt);
            }

            private static void Clear()
            {
                _handlers = null;
            }
        }
    }
}
