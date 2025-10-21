-- Add created_at column to Tag table
ALTER TABLE [Tag] 
ADD [created_at] datetime2(0) NOT NULL DEFAULT (sysdatetime());

-- Update existing tags with current timestamp
UPDATE [Tag] 
SET [created_at] = GETDATE() 
WHERE [created_at] IS NULL;

-- Optional: Add index for better performance
CREATE INDEX IX_Tag_CreatedAt ON [Tag] ([created_at]);
