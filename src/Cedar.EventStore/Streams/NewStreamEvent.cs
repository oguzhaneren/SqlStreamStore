﻿namespace Cedar.EventStore.Streams
{
    using System;
    using EnsureThat;

    public struct NewStreamEvent
    {
        public readonly string JsonData;
        public readonly Guid EventId;
        public readonly string Type;
        public readonly string JsonMetadata;

        public NewStreamEvent(Guid eventId, string type, string jsonData, string metadata = null)
        {
            Ensure.That(eventId, "eventId").IsNotEmpty();
            Ensure.That(type, "type").IsNotNullOrEmpty();
            Ensure.That(jsonData, "data").IsNotNullOrEmpty();

            EventId = eventId;
            Type = type;
            JsonData = jsonData;
            JsonMetadata = metadata ?? string.Empty;
        }
    }
}