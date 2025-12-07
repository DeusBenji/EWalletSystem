IF NOT EXISTS(SELECT * FROM sys.databases WHERE name = 'BachMitID')
BEGIN
    CREATE DATABASE BachMitID;
END
GO

USE BachMitID;
GO

-- ============== Account (lokal projection af AccountService) ==============
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Account')
BEGIN
    CREATE TABLE Account (
        ID          UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        Email       NVARCHAR(255)    NOT NULL,
        CreatedAt   DATETIME2        NOT NULL,
        IsActive    BIT              NULL
    );

    IF NOT EXISTS (
        SELECT * FROM sys.indexes 
        WHERE name = 'IX_Account_Email' 
          AND object_id = OBJECT_ID('Account')
    )
    BEGIN
        CREATE INDEX IX_Account_Email ON Account (Email);
    END
END
GO

-- ============== MitID_Account (din eksisterende model) ==============
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'MitID_Account')
BEGIN
    CREATE TABLE MitID_Account (
        ID        UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        AccountID UNIQUEIDENTIFIER NOT NULL,
        SubID     NVARCHAR(255)    NOT NULL,
        IsAdult   BIT              NOT NULL
    );

    -- Index på AccountID
    IF NOT EXISTS (
        SELECT * FROM sys.indexes 
        WHERE name = 'IX_MitID_Account_AccountID' 
          AND object_id = OBJECT_ID('MitID_Account')
    )
    BEGIN
        CREATE INDEX IX_MitID_Account_AccountID ON MitID_Account (AccountID);
    END

    -- Index på SubID (hashed sub)
    IF NOT EXISTS (
        SELECT * FROM sys.indexes 
        WHERE name = 'IX_MitID_Account_SubID' 
          AND object_id = OBJECT_ID('MitID_Account')
    )
    BEGIN
        CREATE INDEX IX_MitID_Account_SubID ON MitID_Account (SubID);
    END
END
GO
