-- Add Status and RejectionReason columns to Post table
ALTER TABLE Post 
ADD Status NVARCHAR(20) DEFAULT 'pending',
    RejectionReason NVARCHAR(500) NULL;

-- Update existing posts to have approved status
UPDATE Post 
SET Status = 'approved' 
WHERE Status IS NULL OR Status = '';

-- Create index for better performance
CREATE INDEX IX_Post_Status ON Post(Status);
CREATE INDEX IX_Post_user_id_Status ON Post(user_id, Status);
