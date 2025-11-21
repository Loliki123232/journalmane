CREATE TABLE [dbo].[Attendance] (
    [Id]         INT           IDENTITY (1, 1) NOT NULL,
    [StudentId]  INT           NOT NULL,
    [ScheduleId] INT           NOT NULL,
    [Date]       DATE          NOT NULL,
    [IsPresent]  BIT           DEFAULT ((0)) NOT NULL,
    [RecordedBy] INT           NOT NULL,
    [RecordedAt] DATETIME2 (7) DEFAULT (getdate()) NOT NULL,
    PRIMARY KEY CLUSTERED ([Id] ASC),
    FOREIGN KEY ([StudentId]) REFERENCES [dbo].[Students] ([Id]),
    FOREIGN KEY ([ScheduleId]) REFERENCES [dbo].[Schedule] ([Id]),
    FOREIGN KEY ([RecordedBy]) REFERENCES [dbo].[Teachers] ([Id])
);