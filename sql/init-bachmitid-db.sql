IF NOT EXISTS(SELECT * FROM sys.databases WHERE name = 'BachMitID')
BEGIN
    CREATE DATABASE BachMitID;
END
GO

USE BachMitID;
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Accounts')
BEGIN
    CREATE TABLE Accounts (
        ID UNIQUEIDENTIFIER PRIMARY KEY,
        Email NVARCHAR(255) NOT NULL,
        Password NVARCHAR(MAX) NOT NULL
    );
END
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'MitID_Accounts')
BEGIN
    CREATE TABLE MitID_Accounts (
        ID UNIQUEIDENTIFIER PRIMARY KEY,
        AccountID UNIQUEIDENTIFIER NOT NULL,
        SubID NVARCHAR(255) NOT NULL,
        IsAdult BIT NOT NULL
    );
END
GO

IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_MitID_Accounts_Accounts' AND parent_object_id = OBJECT_ID('MitID_Accounts'))
BEGIN
    ALTER TABLE MitID_Accounts
    ADD CONSTRAINT FK_MitID_Accounts_Accounts FOREIGN KEY (AccountID) REFERENCES Accounts (ID);
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_MitID_Accounts_AccountID' AND object_id = OBJECT_ID('MitID_Accounts'))
BEGIN
    CREATE INDEX IX_MitID_Accounts_AccountID ON MitID_Accounts (AccountID);
END
GO
