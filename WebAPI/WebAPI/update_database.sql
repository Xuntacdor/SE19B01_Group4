CREATE TABLE CommentLike (
    comment_like_id INT IDENTITY(1,1) PRIMARY KEY,
    comment_id INT NOT NULL,
    user_id INT NOT NULL,
    created_at DATETIME2(0) NOT NULL DEFAULT SYSDATETIME(),
    
    CONSTRAINT FK__CommentLike__comment_id 
        FOREIGN KEY (comment_id) REFERENCES Comment(comment_id) 
        ON DELETE NO ACTION,
    CONSTRAINT FK__CommentLike__user_id 
        FOREIGN KEY (user_id) REFERENCES Users(user_id) 
        ON DELETE NO ACTION,
    
    CONSTRAINT UQ_CommentLike_user_comment 
        UNIQUE (user_id, comment_id)
);

-- Tạo bảng UserPostHide để track việc ẩn bài viết theo từng user
CREATE TABLE UserPostHide (
    user_post_hide_id INT IDENTITY(1,1) PRIMARY KEY,
    user_id INT NOT NULL,
    post_id INT NOT NULL,
    hidden_at DATETIME2(0) NOT NULL DEFAULT SYSDATETIME(),
    
    CONSTRAINT FK__UserPostHide__user_id 
        FOREIGN KEY (user_id) REFERENCES Users(user_id) 
        ON DELETE CASCADE,
    CONSTRAINT FK__UserPostHide__post_id 
        FOREIGN KEY (post_id) REFERENCES Post(post_id) 
        ON DELETE CASCADE,
    
    CONSTRAINT UQ_UserPostHide_user_post 
        UNIQUE (user_id, post_id)
);