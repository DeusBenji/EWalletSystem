IF NOT EXISTS(SELECT * FROM sys.databases WHERE name = 'AccountServiceDB')
BEGIN
    CREATE DATABASE AccountServiceDB;
END
GO
USE AccountServiceDB;
GO

DROP TABLE IF EXISTS dbo.Account;
GO

CREATE TABLE dbo.Account
(
    ID               UNIQUEIDENTIFIER NOT NULL,
    Email            NVARCHAR(255)    NOT NULL,
    PasswordHash     NVARCHAR(255)    NULL,
    CreatedAt        DATETIME2        NOT NULL,
    IsActive         BIT              NOT NULL,
    IsMitIdVerified  BIT              NOT NULL DEFAULT 0,
    IsAdult          BIT              NOT NULL DEFAULT 0,

    CONSTRAINT PK_Account PRIMARY KEY (ID),
    CONSTRAINT UQ_Account_Email UNIQUE (Email)
);
GO
