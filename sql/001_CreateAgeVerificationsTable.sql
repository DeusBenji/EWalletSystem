-- Migration: Create AgeVerifications table
-- Privacy-first design: NO personal data columns (CPR, DOB, Name, etc.)

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[AgeVerifications]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[AgeVerifications] (
        [Id] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
        [AccountId] UNIQUEIDENTIFIER NULL,
        [ProviderId] NVARCHAR(50) NOT NULL,
        [SubjectId] NVARCHAR(256) NOT NULL,
        [IsAdult] BIT NOT NULL,
        [VerifiedAt] DATETIME2(7) NOT NULL,
        [AssuranceLevel] NVARCHAR(50) NOT NULL DEFAULT 'substantial',
        [ExpiresAt] DATETIME2(7) NULL,
        [PolicyVersion] NVARCHAR(50) NULL,
        [CreatedAt] DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedAt] DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
        
        CONSTRAINT [PK_AgeVerifications] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [UK_AgeVerifications_Provider_Subject] UNIQUE NONCLUSTERED ([ProviderId] ASC, [SubjectId] ASC)
    );
    
    -- Index for looking up by internal AccountId
    CREATE NONCLUSTERED INDEX [IX_AgeVerifications_AccountId] ON [dbo].[AgeVerifications] ([AccountId] ASC) WHERE [AccountId] IS NOT NULL;
    
    PRINT 'AgeVerifications table created successfully.';
END
ELSE
BEGIN
    PRINT 'AgeVerifications table already exists.';
END
GO
