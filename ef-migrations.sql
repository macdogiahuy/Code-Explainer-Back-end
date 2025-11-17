CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;
CREATE TABLE "Users" (
    "UserId" uuid NOT NULL,
    "UserName" text NOT NULL,
    "Email" text NOT NULL,
    "PasswordHash" text NOT NULL,
    "EmailConfirmed" boolean NOT NULL,
    "UserRole" integer NOT NULL,
    "ProfilePictureUrl" text NOT NULL,
    "RefreshToken" text,
    "RefreshTokenExpiryTime" timestamp with time zone,
    "CreatedAt" timestamp without time zone NOT NULL,
    "UpdatedAt" timestamp without time zone NOT NULL,
    CONSTRAINT "PK_Users" PRIMARY KEY ("UserId")
);

CREATE TABLE "ChatSessions" (
    "ChatSessionId" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "Title" text NOT NULL,
    "CreatedAt" timestamp without time zone NOT NULL,
    "UpdatedAt" timestamp without time zone NOT NULL,
    CONSTRAINT "PK_ChatSessions" PRIMARY KEY ("ChatSessionId"),
    CONSTRAINT "FK_ChatSessions_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("UserId") ON DELETE CASCADE
);

CREATE TABLE "Notifications" (
    "NotificationId" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "Title" character varying(200) NOT NULL,
    "Message" character varying(1000) NOT NULL,
    "IsRead" boolean NOT NULL,
    "CreatedAt" timestamp without time zone NOT NULL,
    CONSTRAINT "PK_Notifications" PRIMARY KEY ("NotificationId"),
    CONSTRAINT "FK_Notifications_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("UserId") ON DELETE CASCADE
);

CREATE TABLE "ChatMessages" (
    "ChatMessageId" uuid NOT NULL,
    "ChatSessionId" uuid NOT NULL,
    "Role" text NOT NULL,
    "Content" text NOT NULL,
    "CreatedAt" timestamp without time zone NOT NULL,
    CONSTRAINT "PK_ChatMessages" PRIMARY KEY ("ChatMessageId"),
    CONSTRAINT "FK_ChatMessages_ChatSessions_ChatSessionId" FOREIGN KEY ("ChatSessionId") REFERENCES "ChatSessions" ("ChatSessionId") ON DELETE CASCADE
);

CREATE INDEX "IX_ChatMessages_ChatSessionId" ON "ChatMessages" ("ChatSessionId");

CREATE INDEX "IX_ChatSessions_UserId" ON "ChatSessions" ("UserId");

CREATE INDEX "IX_Notifications_UserId" ON "Notifications" ("UserId");

CREATE UNIQUE INDEX "IX_Users_Email" ON "Users" ("Email");

CREATE UNIQUE INDEX "IX_Users_UserName" ON "Users" ("UserName");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20251025015755_Initial', '9.0.10');

COMMIT;

