#if UNITASK || UNITY_6000_0_OR_NEWER

#if !(UNITY_EDITOR || DEBUG) || DISABLE_DEBUG
#define __MODULE_CORE_PUBSUB_NO_VALIDATION__
#else
#define __MODULE_CORE_PUBSUB_VALIDATION__
#endif

using System.Diagnostics.CodeAnalysis;
using Module.Core.Logging;
using Module.Core.PubSub.Internals;

namespace Module.Core.PubSub
{
    partial class MessagePublisher
    {
        public readonly partial struct Publisher<TScope>
        {
            internal readonly MessagePublisher _publisher;

            public bool IsValid => _publisher != null;

            public TScope Scope { get; }

            internal Publisher([NotNull] MessagePublisher publisher, [NotNull] TScope scope)
            {
                _publisher = publisher;
                Scope = scope;
            }

            public CachedPublisher<TMessage> Cache<TMessage>(ILogger logger = null)
#if MODULE_CORE_PUBSUB_RELAX_MODE
                where TMessage : new()
#else
                where TMessage : IMessage, new()
#endif
            {
#if __MODULE_CORE_PUBSUB_VALIDATION__
                if (Validate(logger) == false)
                {
                    return default;
                }
#endif

                var brokers = _publisher._brokers;

                lock (brokers)
                {
                    if (brokers.TryGet<MessageBroker<TScope, TMessage>>(out var scopedBroker) == false)
                    {
                        scopedBroker = new MessageBroker<TScope, TMessage>();

                        if (brokers.TryAdd(scopedBroker) == false)
                        {
#if __MODULE_CORE_PUBSUB_VALIDATION__
                            LogUnexpectedErrorWhenCache<TMessage>(logger);
#endif

                            scopedBroker?.Dispose();
                            return default;
                        }
                    }

                    var broker = scopedBroker.Cache(Scope, _publisher._taskArrayPool);
                    return new CachedPublisher<TMessage>(broker);
                }
            }

#if __MODULE_CORE_PUBSUB_VALIDATION__
            private bool Validate(ILogger logger)
            {
                if (_publisher == null)
                {
                    (logger ?? DevLogger.Default).LogError(
                        $"{GetType()} must be retrieved via `{nameof(MessagePublisher)}.{nameof(MessagePublisher.Scope)}` API"
                    );

                    return false;
                }

                if (Scope == null)
                {
                    (logger ?? DevLogger.Default).LogException(new System.NullReferenceException(nameof(Scope)));
                    return false;
                }

                return true;
            }

            private bool Validate<TMessage>(TMessage message, ILogger logger)
            {
                if (_publisher == null)
                {
                    (logger ?? DevLogger.Default).LogError(
                        $"{GetType()} must be retrieved via `{nameof(MessagePublisher)}.{nameof(MessagePublisher.Scope)}` API"
                    );

                    return false;
                }

                if (Scope == null)
                {
                    (logger ?? DevLogger.Default).LogException(new System.NullReferenceException(nameof(Scope)));
                    return false;
                }

                if (message == null)
                {
                    (logger ?? DevLogger.Default).LogException(new System.ArgumentNullException(nameof(message)));
                    return false;
                }

                return true;
            }

            private static void LogWarning<TMessage>(TScope scope, ILogger logger)
            {
                (logger ?? DevLogger.Default).LogWarning(
                    $"Found no subscription for `{typeof(TMessage)}` in scope `{scope}`"
                );
            }

            private static void LogUnexpectedErrorWhenCache<TMessage>(ILogger logger)
            {
                (logger ?? DevLogger.Default).LogError(
                    $"Something went wrong when registering a new instance of {typeof(MessageBroker<TScope, TMessage>)}!"
                );
            }
#endif
        }
    }
}

#endif
