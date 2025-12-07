IF NOT EXISTS(SELECT * FROM sys.databases WHERE name = 'TokenServiceDB')
BEGIN
    CREATE DATABASE TokenServiceDB;
END
GO
USE TokenServiceDB;
GO

-- ============== AccountAgeStatus ==============
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AccountAgeStatus')
BEGIN
    CREATE TABLE AccountAgeStatus (
        AccountId  UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        IsAdult    BIT              NOT NULL,
        VerifiedAt DATETIME2        NOT NULL
    );

    -- (Primærnøgle på AccountId er nok; ekstra index er ikke nødvendigt)
END
GO

-- ============== Attestations (AgeAttestation) ==============
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Attestations')
BEGIN
    CREATE TABLE Attestations (
        Id         UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        AccountId  UNIQUEIDENTIFIER NOT NULL,
        SubjectId  NVARCHAR(255)    NOT NULL,
        IsAdult    BIT              NOT NULL,
        IssuedAt   DATETIME2        NOT NULL,
        ExpiresAt  DATETIME2        NOT NULL,
        Token      NVARCHAR(MAX)    NOT NULL,
        Hash       NVARCHAR(MAX)    NOT NULL
    );

    -- Index på AccountId
    IF NOT EXISTS (
        SELECT * FROM sys.indexes 
        WHERE name = 'IX_Attestations_AccountId' 
          AND object_id = OBJECT_ID('Attestations')
    )
    BEGIN
        CREATE INDEX IX_Attestations_AccountId ON Attestations (AccountId);
    END

    -- Index på SubjectId (hvis du vil slå op på subjekt)
    IF NOT EXISTS (
        SELECT * FROM sys.indexes 
        WHERE name = 'IX_Attestations_SubjectId' 
          AND object_id = OBJECT_ID('Attestations')
    )
    BEGIN
        CREATE INDEX IX_Attestations_SubjectId ON Attestations (SubjectId);
    END

    -- Index på Hash (validations/audit)
    IF NOT EXISTS (
        SELECT * FROM sys.indexes 
        WHERE name = 'IX_Attestations_Hash' 
          AND object_id = OBJECT_ID('Attestations')
    )
    BEGIN
        CREATE INDEX IX_Attestations_Hash ON Attestations (Hash);
    END
END
GO
