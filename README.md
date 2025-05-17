# TidyMediator
A messaging system supporting commands, queries, and notifications. Message handlers are registered in a dependency injection system with handlers registered as transient. 

Command and query request messages allow only one handler implementation per request type. Notifications allow zero or more notification handlers. Additionally, notification delegate handlers can be registered by singleton objects or other instanciated objects. Such registered delegates can optionally be scheduled on synchronization contexts, enabling those handlers to be scheduled on to UI threads.

