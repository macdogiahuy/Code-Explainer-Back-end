DROP TABLE IF EXISTS ChatMessages;
DROP TABLE IF EXISTS ChatSessions;
DROP TABLE IF EXISTS Notifications;
DROP TABLE IF EXISTS Users;
DROP TABLE IF EXISTS __EFMigrationsHistory;
GO

-- Bảng migrations
CREATE TABLE __EFMigrationsHistory (
    MigrationId NVARCHAR(150) NOT NULL,
    ProductVersion NVARCHAR(32) NOT NULL,
    CONSTRAINT PK___EFMigrationsHistory PRIMARY KEY (MigrationId)
);
GO

-- Bảng Users
CREATE TABLE Users (
    UserId UNIQUEIDENTIFIER NOT NULL,
    UserName NVARCHAR(100) NOT NULL,
    Email NVARCHAR(256) NOT NULL,
    PasswordHash NVARCHAR(MAX) NOT NULL,
    EmailConfirmed BIT NOT NULL,
    UserRole INT NOT NULL,
    ProfilePictureUrl NVARCHAR(MAX) NOT NULL,
    RefreshToken NVARCHAR(MAX),
    RefreshTokenExpiryTime DATETIMEOFFSET NULL,
    CreatedAt DATETIME NOT NULL,
    UpdatedAt DATETIME NOT NULL,
    CONSTRAINT PK_Users PRIMARY KEY (UserId)
);
GO

-- Bảng ChatSessions
CREATE TABLE ChatSessions (
    ChatSessionId UNIQUEIDENTIFIER NOT NULL,
    UserId UNIQUEIDENTIFIER NOT NULL,
    Title NVARCHAR(MAX) NOT NULL,
    CreatedAt DATETIME NOT NULL,
    UpdatedAt DATETIME NOT NULL,
    CONSTRAINT PK_ChatSessions PRIMARY KEY (ChatSessionId),
    CONSTRAINT FK_ChatSessions_Users_UserId FOREIGN KEY (UserId) REFERENCES Users(UserId) ON DELETE CASCADE
);
GO

-- Bảng Notifications
CREATE TABLE Notifications (
    NotificationId UNIQUEIDENTIFIER NOT NULL,
    UserId UNIQUEIDENTIFIER NOT NULL,
    Title NVARCHAR(200) NOT NULL,
    Message NVARCHAR(1000) NOT NULL,
    IsRead BIT NOT NULL,
    CreatedAt DATETIME NOT NULL,
    CONSTRAINT PK_Notifications PRIMARY KEY (NotificationId),
    CONSTRAINT FK_Notifications_Users_UserId FOREIGN KEY (UserId) REFERENCES Users(UserId) ON DELETE CASCADE
);
GO

-- Bảng ChatMessages
CREATE TABLE ChatMessages (
    ChatMessageId UNIQUEIDENTIFIER NOT NULL,
    ChatSessionId UNIQUEIDENTIFIER NOT NULL,
    Role NVARCHAR(MAX) NOT NULL,
    Content NVARCHAR(MAX) NOT NULL,
    CreatedAt DATETIME NOT NULL,
    CONSTRAINT PK_ChatMessages PRIMARY KEY (ChatMessageId),
    CONSTRAINT FK_ChatMessages_ChatSessions_ChatSessionId FOREIGN KEY (ChatSessionId) REFERENCES ChatSessions(ChatSessionId) ON DELETE CASCADE
);
GO

-- Indexes
CREATE INDEX IX_ChatMessages_ChatSessionId ON ChatMessages (ChatSessionId);
CREATE INDEX IX_ChatSessions_UserId ON ChatSessions (UserId);
CREATE INDEX IX_Notifications_UserId ON Notifications (UserId);

CREATE UNIQUE INDEX IX_Users_Email ON Users (Email);
CREATE UNIQUE INDEX IX_Users_UserName ON Users (UserName);
GO

-- Migrations Table Seed
INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion)
VALUES ('20251025015755_Initial', '9.0.10');
GO