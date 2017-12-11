
    DECLARE @streamIdInternal AS INT;
    DECLARE @latestStreamVersion AS INT;
	DECLARE @latestStreamPosition AS BIGINT;

     SELECT @streamIdInternal = dbo.Streams.IdInternal,
            @latestStreamVersion = dbo.Streams.[Version]
       FROM dbo.Streams
      WHERE dbo.Streams.Id = @streamId;

         IF @streamIdInternal IS NULL
        BEGIN
            
            RAISERROR('WrongExpectedVersion', 16, 1);
            RETURN;
        END

        IF @latestStreamVersion != @expectedStreamVersion
        BEGIN
            
            RAISERROR('WrongExpectedVersion', 16, 2);
            RETURN;
        END

INSERT INTO dbo.Messages (StreamIdInternal, StreamVersion, Id, Created, [Type], JsonData, JsonMetadata)
     SELECT @streamIdInternal,
            StreamVersion + @latestStreamVersion + 1,
            Id,
            Created,
            [Type],
            JsonData,
            JsonMetadata
       FROM @newMessages
   ORDER BY StreamVersion;

  SELECT TOP(1)
            @latestStreamVersion = dbo.Messages.StreamVersion,
            @latestStreamPosition = dbo.Messages.Position
       FROM dbo.Messages
      WHERE dbo.Messages.StreamIDInternal = @streamIdInternal
   ORDER BY dbo.Messages.Position DESC

     UPDATE dbo.Streams
        SET dbo.Streams.[Version] = @latestStreamVersion,
            dbo.Streams.[Position] = @latestStreamPosition
      WHERE dbo.Streams.IdInternal = @streamIdInternal



/* Select CurrentVersion, CurrentPosition */

     SELECT currentVersion = @latestStreamVersion, currentPosition = @latestStreamPosition

/* Select Metadata */
    DECLARE @metadataStreamId as NVARCHAR(42)
    DECLARE @metadataStreamIdInternal as INT
        SET @metadataStreamId = '$$' + @streamId

     SELECT @metadataStreamIdInternal = dbo.Streams.IdInternal
       FROM dbo.Streams
      WHERE dbo.Streams.Id = @metadataStreamId;

     SELECT TOP(1)
            dbo.Messages.JsonData
       FROM dbo.Messages
      WHERE dbo.Messages.StreamIdInternal = @metadataStreamIdInternal
   ORDER BY dbo.Messages.Position DESC;
