using Dapper;

namespace ChatApp.Api.Data;

public class DatabaseInitializer
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ILogger<DatabaseInitializer> _logger;

    public DatabaseInitializer(IDbConnectionFactory connectionFactory, ILogger<DatabaseInitializer> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task InitializeAsync()
    {
        try
        {
            using var connection = _connectionFactory.CreateConnection();

            const string sql = """
IF DB_ID('ChatAppDb') IS NULL
BEGIN
    CREATE DATABASE ChatAppDb;
END;

IF DB_NAME() <> 'ChatAppDb'
BEGIN
    USE ChatAppDb;
END;

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Users')
BEGIN
    CREATE TABLE [dbo].[Users] (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [Username] NVARCHAR(50) NOT NULL UNIQUE,
        [PasswordHash] NVARCHAR(200) NOT NULL,
        [Email] NVARCHAR(100) NOT NULL,
        [DisplayName] NVARCHAR(100) NOT NULL,
        [CreatedAt] DATETIME2 NOT NULL CONSTRAINT DF_Users_CreatedAt DEFAULT (SYSUTCDATETIME()),
        [IsActive] BIT NOT NULL CONSTRAINT DF_Users_IsActive DEFAULT (1)
    );
END;

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Conversations')
BEGIN
    CREATE TABLE [dbo].[Conversations] (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [Title] NVARCHAR(100) NOT NULL,
        [IsGroup] BIT NOT NULL,
        [CreatedAt] DATETIME2 NOT NULL CONSTRAINT DF_Conversations_CreatedAt DEFAULT (SYSUTCDATETIME()),
        [CreatedByUserId] INT NOT NULL,
        [IsArchived] BIT NOT NULL CONSTRAINT DF_Conversations_IsArchived DEFAULT (0)
    );
END;

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ConversationParticipants')
BEGIN
    CREATE TABLE [dbo].[ConversationParticipants] (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [ConversationId] INT NOT NULL,
        [UserId] INT NOT NULL,
        [JoinedAt] DATETIME2 NOT NULL CONSTRAINT DF_ConversationParticipants_JoinedAt DEFAULT (SYSUTCDATETIME()),
        [Role] NVARCHAR(20) NOT NULL,
        [IsMuted] BIT NOT NULL CONSTRAINT DF_ConversationParticipants_IsMuted DEFAULT (0)
    );
END;

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Messages')
BEGIN
    CREATE TABLE [dbo].[Messages] (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [ConversationId] INT NOT NULL,
        [SenderId] INT NOT NULL,
        [Content] NVARCHAR(1000) NOT NULL,
        [SentAt] DATETIME2 NOT NULL CONSTRAINT DF_Messages_SentAt DEFAULT (SYSUTCDATETIME()),
        [IsEdited] BIT NOT NULL CONSTRAINT DF_Messages_IsEdited DEFAULT (0),
        [IsDeleted] BIT NOT NULL CONSTRAINT DF_Messages_IsDeleted DEFAULT (0)
    );
END;

IF NOT EXISTS (
    SELECT * FROM sys.foreign_keys WHERE name = 'FK_Conversations_Users_CreatedByUserId'
)
BEGIN
    ALTER TABLE [dbo].[Conversations]
    ADD CONSTRAINT [FK_Conversations_Users_CreatedByUserId]
        FOREIGN KEY ([CreatedByUserId]) REFERENCES [dbo].[Users] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM sys.foreign_keys WHERE name = 'FK_ConversationParticipants_Conversations_ConversationId'
)
BEGIN
    ALTER TABLE [dbo].[ConversationParticipants]
    ADD CONSTRAINT [FK_ConversationParticipants_Conversations_ConversationId]
        FOREIGN KEY ([ConversationId]) REFERENCES [dbo].[Conversations] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM sys.foreign_keys WHERE name = 'FK_ConversationParticipants_Users_UserId'
)
BEGIN
    ALTER TABLE [dbo].[ConversationParticipants]
    ADD CONSTRAINT [FK_ConversationParticipants_Users_UserId]
        FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM sys.foreign_keys WHERE name = 'FK_Messages_Conversations_ConversationId'
)
BEGIN
    ALTER TABLE [dbo].[Messages]
    ADD CONSTRAINT [FK_Messages_Conversations_ConversationId]
        FOREIGN KEY ([ConversationId]) REFERENCES [dbo].[Conversations] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM sys.foreign_keys WHERE name = 'FK_Messages_Users_SenderId'
)
BEGIN
    ALTER TABLE [dbo].[Messages]
    ADD CONSTRAINT [FK_Messages_Users_SenderId]
        FOREIGN KEY ([SenderId]) REFERENCES [dbo].[Users] ([Id]);
END;
""";

            await connection.ExecuteAsync(sql);

            _logger.LogInformation("Database schema ensured successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ensuring database schema.");
            throw;
        }
    }
}

