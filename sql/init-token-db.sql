IF NOT EXISTS(SELECT * FROM sys.databases WHERE name = 'TokenServiceDB')
BEGIN
    CREATE DATABASE TokenServiceDB;
END
GO

USE TokenServiceDB;
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AgeAttestations')
BEGIN
    CREATE TABLE AgeAttestations (
        Id UNIQUEIDENTIFIER PRIMARY KEY,
        AccountId UNIQUEIDENTIFIER NOT NULL,
        SubjectId NVARCHAR(255) NOT NULL,
        IsAdult BIT NOT NULL,
        IssuedAt DATETIME2 NOT NULL,
        ExpiresAt DATETIME2 NOT NULL,
        Token NVARCHAR(MAX) NOT NULL,
        Hash NVARCHAR(MAX) NOT NULL
    );
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_AgeAttestations_AccountId' AND object_id = OBJECT_ID('AgeAttestations'))
BEGIN
    CREATE INDEX IX_AgeAttestations_AccountId ON AgeAttestations (AccountId);
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_AgeAttestations_SubjectId' AND object_id = OBJECT_ID('AgeAttestations'))
BEGIN
    CREATE INDEX IX_AgeAttestations_SubjectId ON AgeAttestations (SubjectId);
END
GO
