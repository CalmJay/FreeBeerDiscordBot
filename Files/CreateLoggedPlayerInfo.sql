USE [FreeBeerdb]
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].LoggedPlayerInfo(
	[TransactionID] INT NOT NULL IDENTITY(1,1) PRIMARY KEY,
	[PlayerID] [varchar] (50)NOT NULL,
	[PlayerName] [varchar](50) NOT NULL,
	[GuildID] [varchar](50) NULL,
	[DeathFame] bigint NULL,
	[KillFame] bigint NULL,
	[FameRatio] float NULL,
	[PVEFame] bigint NULL,
	[GatheringFame] bigint NULL,
	[CraftingFame] bigint NULL,
	[RecordedDate] [datetime] NULL,

	);
SET IDENTITY_INSERT LoggedPlayerInfo ON
