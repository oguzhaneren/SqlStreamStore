# SQL Stream Store with Transaction Support

A stream store library for .NET that specifically target SQL based implementations. Typically
used in Event Sourced based applications.

Forked from https://github.com/SQLStreamStore/SQLStreamStore to add transaction support.

# Design considerations:

 - Designed to only ever support RDMBS\SQL implementations.
 - Subscriptions are eventually consistent.
 - API is influenced by (but not compatible with) [EventStore](https://geteventstore.com/)
 - Async only.
 - JSON only event and metadata payloads (usually just a `string` / `varchar` / etc).
 - Transaction support 
 
    *(if you want 'enforcing the concept of the stream as the consistency and transaction boundary' you should look at: https://github.com/SQLStreamStore/SQLStreamStore)*


# Licences

Licenced under [MIT](LICENSE).
