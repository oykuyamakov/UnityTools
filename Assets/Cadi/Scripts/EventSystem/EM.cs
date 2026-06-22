using System;
using System.Collections.Generic;

namespace Cadi.Scripts.EventSystem
{
    public static class EM
    {
        public static readonly object GlobalContext = new();

        public static object CurrentContext { get; private set; } = GlobalContext;

        public const int C_DefaultChannel = -1;

        private static readonly Dictionary<object, Dictionary<int, EventHub>> s_ChannelsByContext = new();

        private static bool TryGetEventListenerCollection(
            object context,
            int channel,
            out EventHub listeners,
            bool createIfNotExists = true)
        {
            context ??= GlobalContext;

            if (!s_ChannelsByContext.TryGetValue(context, out var listenersByChannel))
            {
                if (!createIfNotExists)
                {
                    listeners = null;
                    return false;
                }

                listenersByChannel = new Dictionary<int, EventHub>();
                s_ChannelsByContext[context] = listenersByChannel;
            }

            if (!listenersByChannel.TryGetValue(channel, out listeners))
            {
                if (!createIfNotExists)
                {
                    listeners = null;
                    return false;
                }

                listeners = new EventHub();
                listenersByChannel[channel] = listeners;
            }

            return true;
        }

        public static bool AddListener<T>(
            EventListener<T> listener,
            Priority priority = Priority.Normal,
            object context = null,
            int channel = C_DefaultChannel)
            where T : Event, new()
        {
            if (listener == null)
                throw new ArgumentNullException(nameof(listener));

            TryGetEventListenerCollection(context, channel, out var listeners);
            return listeners.AddListener(listener, priority);
        }

        public static bool RemoveListener<T>(
            EventListener<T> listener,
            object context = null,
            int channel = C_DefaultChannel)
            where T : Event, new()
        {
            if (listener == null)
                throw new ArgumentNullException(nameof(listener));

            if (!TryGetEventListenerCollection(context, channel, out var listeners, false))
                return false;

            return listeners.RemoveListener(listener);
        }

        public static T SendEvent<T>(
            T evt,
            object context = null,
            int channel = C_DefaultChannel)
            where T : Event, new()
        {
            if (evt == null)
                throw new ArgumentNullException(nameof(evt));

            context = context ?? GlobalContext;

            if (!TryGetEventListenerCollection(context, channel, out var listeners, false))
                return evt;

            var oldContext = CurrentContext;
            CurrentContext = context;

            try
            {
                listeners.SendEvent(evt);
            }
            finally
            {
                CurrentContext = oldContext;
            }

            return evt;
        }

        public static bool ClearChannel(
            object context = null,
            int channel = C_DefaultChannel)
        {
            context = context ?? GlobalContext;

            if (!s_ChannelsByContext.TryGetValue(context, out var listenersByChannel))
                return false;

            bool removed = listenersByChannel.Remove(channel);

            if (listenersByChannel.Count == 0)
                s_ChannelsByContext.Remove(context);

            return removed;
        }

        public static bool ClearContext(object context = null)
        {
            context = context ?? GlobalContext;
            return s_ChannelsByContext.Remove(context);
        }

        public static void ClearAll()
        {
            s_ChannelsByContext.Clear();
            CurrentContext = GlobalContext;
        }
    }

    public static class EventExtensions
    {
        public static T SendGlobal<T>(this T evt, int channel = EM.C_DefaultChannel)
            where T : Event, new()
        {
            return EM.SendEvent(evt, EM.GlobalContext, channel);
        }

        public static T SendEvent<T>(
            this object context,
            T evt,
            int channel = EM.C_DefaultChannel)
            where T : Event, new()
        {
            return EM.SendEvent(evt, context, channel);
        }
    }
}