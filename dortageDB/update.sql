BEGIN TRANSACTION;
DROP TABLE [Referrals];

DROP INDEX [IX_VekarerProfiles_ReferralCode] ON [VekarerProfiles];

DECLARE @var sysname;
SELECT @var = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[VekarerProfiles]') AND [c].[name] = N'ReferralCode');
IF @var IS NOT NULL EXEC(N'ALTER TABLE [VekarerProfiles] DROP CONSTRAINT [' + @var + '];');
ALTER TABLE [VekarerProfiles] DROP COLUMN [ReferralCode];

DECLARE @var1 sysname;
SELECT @var1 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[VekarerProfiles]') AND [c].[name] = N'UsedReferralCode');
IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [VekarerProfiles] DROP CONSTRAINT [' + @var1 + '];');
ALTER TABLE [VekarerProfiles] DROP COLUMN [UsedReferralCode];

ALTER TABLE [Projeler] ADD [PanellumKlasorYolu] nvarchar(max) NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260224102808_AddPanellumKlasorYolu', N'9.0.10');

COMMIT;
GO

