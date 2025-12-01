IF NOT EXISTS(SELECT * FROM sys.databases WHERE name = 'ValidationServiceDB')
BEGIN
    CREATE DATABASE ValidationServiceDB;
END
GO

USE ValidationServiceDB;
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'VerificationLogs')
BEGIN
    CREATE TABLE VerificationLogs (
        Id UNIQUEIDENTIFIER PRIMARY KEY,
        VcJwtHash NVARCHAR(MAX) NOT NULL,
        IsValid BIT NOT NULL,
        FailureReason NVARCHAR(MAX) NULL,
        VerifiedAt DATETIME2 NOT NULL
    );
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_VerificationLogs_VcJwtHash' AND object_id = OBJECT_ID('VerificationLogs'))
BEGIN
    CREATE INDEX IX_VerificationLogs_VcJwtHash ON VerificationLogs (VcJwtHash);
END
GO
