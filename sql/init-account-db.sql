IF NOT EXISTS(SELECT * FROM sys.databases WHERE name = 'AccountServiceDB')
BEGIN
    CREATE DATABASE AccountServiceDB;
END
GO

USE AccountServiceDB;
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Accounts')
BEGIN
    CREATE TABLE Accounts (
        Id UNIQUEIDENTIFIER PRIMARY KEY,
        Email NVARCHAR(255) NOT NULL,
        PasswordHash NVARCHAR(MAX) NULL,
        MitIdSubId NVARCHAR(255) NULL,
        IsAdult BIT NOT NULL DEFAULT 0,
        IsMitIdLinked BIT NOT NULL DEFAULT 0,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        IsActive BIT NOT NULL DEFAULT 1
    );
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Accounts_Email' AND object_id = OBJECT_ID('Accounts'))
BEGIN
    CREATE UNIQUE INDEX IX_Accounts_Email ON Accounts (Email);
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Accounts_MitIdSubId' AND object_id = OBJECT_ID('Accounts'))
BEGIN
    CREATE INDEX IX_Accounts_MitIdSubId ON Accounts (MitIdSubId) WHERE MitIdSubId IS NOT NULL;
END
GO
